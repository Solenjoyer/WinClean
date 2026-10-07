namespace WinClean.Core.Storage;

[Flags]
public enum FolderState : byte
{
    None = 0,

    /// <summary>The folder could not be listed; its size is unknown rather than zero.</summary>
    AccessDenied = 1,

    /// <summary>A junction or symbolic link that was not followed.</summary>
    Link = 2,

    /// <summary>A cloud folder whose files may live only online.</summary>
    CloudFolder = 4,

    /// <summary>Recognised build output, package cache or similar developer artifact.</summary>
    DeveloperArtifact = 8,
}
