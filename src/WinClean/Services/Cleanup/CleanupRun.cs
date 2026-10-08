using WinClean.Core.Cleanup;

namespace WinClean.Services.Cleanup;

public sealed record CleanupRun(IReadOnlyList<CleanupItemResult> Results, string? LogPath, TimeSpan Duration)
{
    public long FreedBytes => Results.Where(result => result.Removed).Sum(result => result.Item.Size);

    public int Removed => Results.Count(result => result.Removed);

    public int InUse => Results.Count(result => result.Outcome == CleanupOutcome.InUse);

    public int AccessDenied => Results.Count(result => result.Outcome == CleanupOutcome.AccessDenied);
}
