namespace WinClean.Services.Cleanup;

public sealed record CleanupProgress(int Done, int Total, string CurrentPath);
