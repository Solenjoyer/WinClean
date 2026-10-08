namespace WinClean.Core.Cleanup;

public enum CleanupItemKind
{
    /// <summary>A single file, deleted on its own or moved to the Recycle Bin.</summary>
    File,

    /// <summary>An entry of the Recycle Bin; the bin of its drive is emptied as a whole.</summary>
    RecycleBinEntry,

    /// <summary>A detected developer folder, the one case of recursive deletion.</summary>
    DeveloperFolder,

    /// <summary>A command run by a tool that owns the data, such as a Docker prune.</summary>
    Command,
}
