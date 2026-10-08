using WinClean.Core.Applications;
using WinClean.Core.Monitoring;
using WinClean.Core.Monitoring.Parsers;
using WinClean.Native;

namespace WinClean.Services.Processes;

/// <summary>
/// One kernel call per tick for every process on the machine, parsed in place. Processes are tracked by
/// id and start time, so a reused id never inherits another process's history, and the rates come
/// from the difference to the previous tick.
/// </summary>
public sealed class ProcessSnapshotReader
{
    private const int InitialBufferSize = 1024 * 1024;

    private readonly ProcessDetailsCache _details;

    private readonly Dictionary<ProcessIdentity, ProcessState> _states = [];

    private readonly List<ProcessRecord> _records = [];

    private readonly List<ProcessIdentity> _gone = [];

    private byte[] _buffer = GC.AllocateUninitializedArray<byte>(InitialBufferSize, pinned: true);

    private long _previousTicks;

    private int _sequence;

    public ProcessSnapshotReader(ProcessDetailsCache details)
    {
        _details = details;
    }

    public string? Reason { get; private set; }

    public ProcessSnapshot? Read(
        long nowTicks,
        int processorCount,
        IReadOnlyList<(GpuCounterInstance Instance, double Value)> gpuEngines,
        IReadOnlyList<(GpuCounterInstance Instance, double Value)> gpuMemory,
        Dictionary<int, WindowFacts> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        if (!Query())
        {
            return null;
        }

        var elapsed = _previousTicks == 0 ? 0 : nowTicks - _previousTicks;
        _previousTicks = nowTicks;
        _sequence++;

        var gpuPercent = GpuPercentByPid(gpuEngines);
        var gpuBytes = GpuMemoryByPid(gpuMemory);
        var samples = new List<ProcessSample>(_records.Count);
        var facts = new List<ProcessFacts>(_records.Count);

        foreach (var record in _records)
        {
            if (record.Pid == 0)
            {
                continue;
            }

            var identity = new ProcessIdentity(record.Pid, record.CreateTime);

            if (!_states.TryGetValue(identity, out var state))
            {
                state = new ProcessState(ProcessInformationParser.ReadName(_buffer, record));
                _states[identity] = state;
            }

            state.Seen = _sequence;
            var details = _details.TryGet(identity);

            if (details is null)
            {
                _details.Request(identity);
            }

            var cpu = state.HasHistory ? CpuMath.ProcessUsage(record.CpuTime - state.CpuTime, elapsed, processorCount) : 0;
            var io = state.HasHistory && elapsed > 0 ? Math.Max(0, record.IoTransfer - state.IoTransfer) * (double)TimeSpan.TicksPerSecond / elapsed : 0;
            state.CpuTime = record.CpuTime;
            state.IoTransfer = record.IoTransfer;
            state.HasHistory = true;

            windows.TryGetValue(record.Pid, out var window);

            var processFacts = new ProcessFacts(
                record.Pid,
                record.CreateTime,
                record.ParentPid,
                state.Name,
                details?.Path,
                details?.CommandLine,
                details?.Description,
                record.SessionId,
                details?.IsSystemAccount ?? record.SessionId == 0,
                details?.IsCritical ?? false);

            facts.Add(processFacts);
            samples.Add(new ProcessSample(
                processFacts,
                cpu,
                record.CpuTime,
                record.PrivateWorkingSet,
                record.WorkingSet,
                record.Commit,
                record.Threads,
                record.Handles,
                io,
                gpuPercent.GetValueOrDefault(record.Pid),
                gpuBytes.GetValueOrDefault(record.Pid),
                record.IsSuspended,
                window is not null,
                window?.IsHung ?? false));
        }

        ForgetExited();

        var titles = new Dictionary<int, IReadOnlyList<string>>(windows.Count);

        foreach (var (pid, window) in windows)
        {
            titles[pid] = window.Titles;
        }

        return new ProcessSnapshot(samples, ApplicationGrouper.Group(facts), titles);
    }

    /// <summary>Drops the rate history, for instance after the machine slept.</summary>
    public void Reset()
    {
        _previousTicks = 0;

        foreach (var state in _states.Values)
        {
            state.HasHistory = false;
        }
    }

    private bool Query()
    {
        while (true)
        {
            var status = NtDll.NtQuerySystemInformation(NtDll.SystemProcessInformation, _buffer, (uint)_buffer.Length, out var needed);

            if (status is NtDll.STATUS_INFO_LENGTH_MISMATCH or NtDll.STATUS_BUFFER_TOO_SMALL)
            {
                // Leave room for processes that start while the bigger buffer is being filled.
                var size = Math.Max(_buffer.Length * 2, (int)needed + 256 * 1024);
                _buffer = GC.AllocateUninitializedArray<byte>(size, pinned: true);
                continue;
            }

            if (status != NtDll.STATUS_SUCCESS)
            {
                Reason = $"NtQuerySystemInformation failed with status 0x{status:X8}.";
                return false;
            }

            ulong address;

            unsafe
            {
                fixed (byte* start = _buffer)
                {
                    address = (ulong)start;
                }
            }

            // The buffer lives on the pinned heap, so the addresses the kernel wrote are still valid here.
            ProcessInformationParser.Parse(_buffer.AsSpan(0, (int)needed), address, _records);
            Reason = null;
            return true;
        }
    }

    private void ForgetExited()
    {
        _gone.Clear();

        foreach (var (identity, state) in _states)
        {
            if (state.Seen != _sequence)
            {
                _gone.Add(identity);
            }
        }

        foreach (var identity in _gone)
        {
            _states.Remove(identity);
            _details.Forget(identity);
        }
    }

    /// <summary>Task Manager's number: the busiest engine type of each process, summed over that type's engines.</summary>
    private static Dictionary<int, double> GpuPercentByPid(IReadOnlyList<(GpuCounterInstance Instance, double Value)> engines)
    {
        var byType = new Dictionary<(int Pid, string Type), double>();

        foreach (var (instance, value) in engines)
        {
            if (instance.ProcessId is { } pid && value > 0)
            {
                var key = (pid, instance.EngineType ?? string.Empty);
                byType[key] = byType.GetValueOrDefault(key) + value;
            }
        }

        var result = new Dictionary<int, double>();

        foreach (var ((pid, _), value) in byType)
        {
            result[pid] = Math.Min(100, Math.Max(result.GetValueOrDefault(pid), value));
        }

        return result;
    }

    private static Dictionary<int, long> GpuMemoryByPid(IReadOnlyList<(GpuCounterInstance Instance, double Value)> memory)
    {
        var result = new Dictionary<int, long>();

        foreach (var (instance, value) in memory)
        {
            if (instance.ProcessId is { } pid)
            {
                result[pid] = result.GetValueOrDefault(pid) + (long)value;
            }
        }

        return result;
    }

    private sealed class ProcessState(string name)
    {
        public string Name { get; } = name;

        public long CpuTime { get; set; }

        public long IoTransfer { get; set; }

        public bool HasHistory { get; set; }

        public int Seen { get; set; }
    }
}
