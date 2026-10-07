using WinClean.Core.Applications;

namespace WinClean.Core.Monitoring;

/// <summary>Every process on the machine at one moment, already grouped by application.</summary>
public sealed record ProcessSnapshot(
    IReadOnlyList<ProcessSample> Processes,
    GroupingResult Grouping,
    IReadOnlyDictionary<int, IReadOnlyList<string>> WindowTitles)
{
    public static ProcessSnapshot Empty { get; } = new([], new GroupingResult([], new Dictionary<int, string>()), new Dictionary<int, IReadOnlyList<string>>());
}
