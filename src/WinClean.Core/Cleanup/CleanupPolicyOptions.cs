namespace WinClean.Core.Cleanup;

/// <summary>
/// The places cleanup must never touch, as resolved by the application. Protected roots (program
/// folders, the user profile, the Users folder) may not be a cleanup root themselves, although their
/// subfolders may; protected trees (Desktop, Documents, Pictures, ...) are off limits entirely.
/// </summary>
public sealed record CleanupPolicyOptions(
    string WindowsDirectory,
    IReadOnlyList<string> AllowedWindowsSubfolders,
    IReadOnlyList<string> ProtectedRoots,
    IReadOnlyList<string> ProtectedTrees,
    int MaximumDepth = 32);
