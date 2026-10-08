using Microsoft.Win32.SafeHandles;
using WinClean.Core.Monitoring;
using WinClean.Core.Monitoring.Parsers;
using WinClean.Native;

namespace WinClean.Services.Monitoring;

/// <summary>
/// Read and write throughput from IOCTL_DISK_PERFORMANCE on each physical disk. This is the source the
/// performance counters are built from, so it works without administrator rights and without perflib.
/// </summary>
internal sealed class DiskActivitySampler : IDisposable
{
    private const int MaximumDiskNumber = 32;

    private static readonly TimeSpan EnumerationInterval = TimeSpan.FromSeconds(60);

    private readonly List<Disk> _disks = [];

    private readonly byte[] _buffer = new byte[DiskPerformanceParser.Size];

    private string? _openError;

    private string? _ioctlError;

    private long? _lastEnumerationTicks;

    public string? Reason { get; private set; }

    public DiskActivitySample? Sample(long nowTicks)
    {
        if (_lastEnumerationTicks is null || nowTicks - _lastEnumerationTicks.Value > EnumerationInterval.Ticks)
        {
            Enumerate();
            _lastEnumerationTicks = nowTicks;
        }

        if (_disks.Count == 0)
        {
            Reason ??= _openError is null
                ? "No physical disk could be opened."
                : $"Physical disks could not be opened: {_openError}.";
            return null;
        }

        double read = 0;
        double write = 0;
        double busiest = 0;
        var measured = 0;

        for (var index = _disks.Count - 1; index >= 0; index--)
        {
            var disk = _disks[index];

            if (!Kernel32.DeviceIoControl(disk.Handle, Kernel32.IOCTL_DISK_PERFORMANCE, [], 0, _buffer, (uint)_buffer.Length, out _, 0)
                || !DiskPerformanceParser.TryParse(_buffer, out var performance))
            {
                _ioctlError = Win32Reason.LastError();
                disk.Handle.Dispose();
                _disks.RemoveAt(index);
                continue;
            }

            var readRate = disk.Read.Update((ulong)performance.BytesRead, nowTicks);
            var writeRate = disk.Write.Update((ulong)performance.BytesWritten, nowTicks);

            if (readRate is null || writeRate is null)
            {
                disk.PreviousIdle = performance.IdleTime;
                disk.PreviousTicks = nowTicks;
                continue;
            }

            var elapsed = nowTicks - disk.PreviousTicks;

            if (elapsed > 0)
            {
                var idle = Math.Clamp((performance.IdleTime - disk.PreviousIdle) / (double)elapsed, 0, 1);
                busiest = Math.Max(busiest, 100 * (1 - idle));
            }

            disk.PreviousIdle = performance.IdleTime;
            disk.PreviousTicks = nowTicks;
            read += readRate.Value;
            write += writeRate.Value;
            measured++;
        }

        if (_disks.Count == 0)
        {
            Reason = $"IOCTL_DISK_PERFORMANCE failed: {_ioctlError}; disk counters may be disabled (diskperf -Y).";
            return null;
        }

        Reason = null;
        return measured == 0 ? null : new DiskActivitySample(read, write, busiest, measured);
    }

    public void Reset()
    {
        foreach (var disk in _disks)
        {
            disk.Read.Reset();
            disk.Write.Reset();
        }
    }

    public void Dispose()
    {
        foreach (var disk in _disks)
        {
            disk.Handle.Dispose();
        }

        _disks.Clear();
    }

    private void Enumerate()
    {
        var known = _disks.Select(disk => disk.Number).ToHashSet();

        for (var number = 0; number < MaximumDiskNumber; number++)
        {
            if (known.Contains(number))
            {
                continue;
            }

            var handle = Kernel32.CreateFileW(
                $@"\\.\PhysicalDrive{number}",
                0,
                Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE,
                0,
                Kernel32.OPEN_EXISTING,
                0,
                0);

            if (handle.IsInvalid)
            {
                // Numbers are not contiguous, so a missing disk is normal; the error is only kept for the report.
                var error = Win32Reason.LastError();

                if (!error.StartsWith("The system cannot find the file specified", StringComparison.Ordinal))
                {
                    _openError = error;
                }

                handle.Dispose();
                continue;
            }

            _disks.Add(new Disk(number, handle));
        }
    }

    private sealed class Disk(int number, SafeFileHandle handle)
    {
        public int Number { get; } = number;

        public SafeFileHandle Handle { get; } = handle;

        public RateCalculator Read { get; } = new();

        public RateCalculator Write { get; } = new();

        public long PreviousIdle { get; set; }

        public long PreviousTicks { get; set; }
    }
}
