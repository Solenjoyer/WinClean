using WinClean.Core.Monitoring;
using WinClean.Core.Monitoring.Parsers;
using WinClean.Native;

namespace WinClean.Services.Monitoring;

/// <summary>Total usage from GetSystemTimes, per-core usage from the processor performance information class.</summary>
internal sealed class CpuSampler
{
    private readonly byte[] _coreBuffer = new byte[ProcessorTimesParser.EntrySize * (Environment.ProcessorCount + 8)];

    private ProcessorTimes _previous;

    private ProcessorTimes[]? _previousCores;

    private bool _primed;

    public int ProcessorCount { get; } = Environment.ProcessorCount;

    public string? Reason { get; private set; }

    public string? CoresReason { get; private set; }

    public (double? Total, float[]? Cores) Sample()
    {
        if (!Kernel32.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            Reason = Win32Reason.LastError();
            return (null, null);
        }

        Reason = null;
        var current = new ProcessorTimes(idle, kernel, user);
        double? total = _primed ? CpuMath.Usage(_previous, current) : null;
        _previous = current;
        _primed = true;

        return (total, SampleCores());
    }

    /// <summary>Forget the previous reading, for instance after the machine resumed from sleep.</summary>
    public void Reset()
    {
        _primed = false;
        _previousCores = null;
    }

    private float[]? SampleCores()
    {
        var status = NtDll.NtQuerySystemInformation(NtDll.SystemProcessorPerformanceInformation, _coreBuffer, (uint)_coreBuffer.Length, out var returned);

        if (status != NtDll.STATUS_SUCCESS)
        {
            CoresReason = $"NtQuerySystemInformation returned 0x{status:X8}";
            return null;
        }

        CoresReason = null;
        var current = ProcessorTimesParser.Parse(_coreBuffer.AsSpan(0, (int)returned));
        float[]? usage = null;

        if (_previousCores is not null && _previousCores.Length == current.Length)
        {
            usage = new float[current.Length];

            for (var index = 0; index < current.Length; index++)
            {
                usage[index] = (float)CpuMath.Usage(_previousCores[index], current[index]);
            }
        }

        _previousCores = current;
        return usage;
    }
}
