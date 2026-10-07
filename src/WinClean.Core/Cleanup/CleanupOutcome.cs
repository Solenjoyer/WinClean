namespace WinClean.Core.Cleanup;

public enum CleanupOutcome
{
    Deleted,
    Recycled,
    InUse,
    AccessDenied,
    Gone,
    Skipped,
    Failed,
}
