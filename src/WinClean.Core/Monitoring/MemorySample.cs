namespace WinClean.Core.Monitoring;

public sealed record MemorySample(
    long Total,
    long Available,
    long Installed,
    long CommitTotal,
    long CommitLimit,
    long Cached,
    long KernelPaged,
    long KernelNonpaged,
    int Processes,
    int Threads,
    int Handles)
{
    public long Used => Total - Available;

    public double UsedPercent => Total <= 0 ? 0 : Used * 100.0 / Total;

    /// <summary>Memory the firmware or devices keep from Windows: installed minus what the kernel manages.</summary>
    public long HardwareReserved => Installed > Total ? Installed - Total : 0;
}
