namespace WinClean.Core.Health;

/// <summary>What the machine knows about updates without going online.</summary>
public sealed record UpdateFacts(
    DateTimeOffset? LastDetect,
    DateTimeOffset? LastDownload,
    DateTimeOffset? LastInstall,
    IReadOnlyList<UpdateHistoryEntry> History,
    bool? RebootRequired)
{
    public static UpdateFacts Empty { get; } = new(null, null, null, [], null);
}
