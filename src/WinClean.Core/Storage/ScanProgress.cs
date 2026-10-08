namespace WinClean.Core.Storage;

public sealed record ScanProgress(int Folders, long Files, long Bytes, string CurrentPath);
