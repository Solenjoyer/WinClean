namespace WinClean.Core.Monitoring.Parsers;

/// <summary>Idle, kernel and user time of one logical processor, in 100-nanosecond units.</summary>
public readonly record struct ProcessorTimes(long Idle, long Kernel, long User)
{
    /// <summary>Kernel time includes idle time, as the kernel reports it.</summary>
    public long Total => Kernel + User;
}
