namespace WinClean.Services.Monitoring;

/// <summary>What the UI currently needs, so the sampler does no work nobody is looking at.</summary>
public sealed record MonitoringDemand(bool Metrics, bool Detailed, bool Processes, int IntervalSeconds)
{
    public static MonitoringDemand None { get; } = new(false, false, false, 5);

    public bool Any => Metrics || Detailed || Processes;
}
