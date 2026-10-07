namespace WinClean.Core.Monitoring;

/// <summary>One tick of the monitor. Anything that could not be read on this machine is null.</summary>
public sealed record SystemSample(
    DateTimeOffset Timestamp,
    long Sequence,
    double? CpuPercent,
    IReadOnlyList<float>? CpuCores,
    double? CpuFrequencyMHz,
    int ProcessorCount,
    MemorySample? Memory,
    IReadOnlyList<VolumeSample> Volumes,
    DiskActivitySample? Disk,
    NetworkSample? Network,
    BatterySample? Battery,
    IReadOnlyList<GpuSample> Gpus,
    TimeSpan Uptime,
    ProcessSnapshot? Processes = null)
{
    /// <summary>The busiest adapter, which is what the Overview card shows.</summary>
    public GpuSample? PrimaryGpu => Gpus.Count == 0 ? null : Gpus.MaxBy(gpu => gpu.DedicatedTotal ?? gpu.DedicatedUsed);
}
