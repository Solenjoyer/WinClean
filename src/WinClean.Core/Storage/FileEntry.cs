namespace WinClean.Core.Storage;

public sealed record FileEntry(string Path, long Size, DateTime LastWriteUtc);
