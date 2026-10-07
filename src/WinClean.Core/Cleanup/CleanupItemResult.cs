namespace WinClean.Core.Cleanup;

public sealed record CleanupItemResult(CleanupItem Item, CleanupOutcome Outcome, string? Detail = null)
{
    public bool Removed => Outcome is CleanupOutcome.Deleted or CleanupOutcome.Recycled;
}
