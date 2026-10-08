namespace WinClean.Core.Monitoring.Parsers;

/// <summary>Cumulative counters from IOCTL_DISK_PERFORMANCE; times are in 100-nanosecond units.</summary>
public readonly record struct DiskPerformance(long BytesRead, long BytesWritten, long ReadTime, long WriteTime, long IdleTime, uint ReadCount, uint WriteCount, uint QueueDepth, long QueryTime);
