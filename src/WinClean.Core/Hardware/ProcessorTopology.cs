namespace WinClean.Core.Hardware;

/// <summary>Cores, threads, packages and caches from GetLogicalProcessorInformationEx.</summary>
public sealed record ProcessorTopology(
    int Cores,
    int LogicalProcessors,
    int Packages,
    int PerformanceCores,
    int EfficiencyCores,
    IReadOnlyList<CacheInfo> Caches)
{
    public static ProcessorTopology Empty { get; } = new(0, 0, 0, 0, 0, []);

    public bool IsHybrid => EfficiencyCores > 0 && PerformanceCores > 0;
}
