namespace WinClean.Core.Storage;

/// <summary>Everything one scan produced. A cancelled scan still carries what it saw, marked as partial.</summary>
public sealed record ScanResult(
    string RootPath,
    ScanTree Tree,
    IReadOnlyList<FileEntry> LargestFiles,
    IReadOnlyList<CategoryTotal> Categories,
    IReadOnlyList<DeveloperArtifact> Artifacts,
    TimeSpan Duration,
    bool Cancelled)
{
    public const int Root = 0;

    public long TotalBytes => Tree.TotalBytes(Root);

    public int TotalFiles => Tree.TotalFiles(Root);

    public int DeniedFolders => Tree.DeniedFolders(Root);
}
