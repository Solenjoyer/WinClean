namespace WinClean.Core.Cleanup;

/// <summary>One thing the preview shows and the executor acts on, exactly as previewed.</summary>
public sealed record CleanupItem(string CategoryId, string Path, long Size, DateTimeOffset LastWrite, CleanupItemKind Kind, string? Root = null, string? Detail = null);
