namespace WinClean.Core.Cleanup;

/// <summary>What the Recycle Bin remembers about a deleted item: where it came from, how big it was and when it went.</summary>
public sealed record RecycleBinEntry(string OriginalPath, long Size, DateTimeOffset DeletedAt, int FormatVersion);
