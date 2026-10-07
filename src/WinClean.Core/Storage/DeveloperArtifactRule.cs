namespace WinClean.Core.Storage;

/// <summary>
/// How to recognise a developer folder: by its name plus, when the name alone is too common ("target",
/// "build", "Library"), a marker next to it or inside it.
/// </summary>
public sealed record DeveloperArtifactRule(
    ArtifactKind Kind,
    string DisplayName,
    string Description,
    IReadOnlyList<string> FolderNames,
    IReadOnlyList<string>? SiblingMarkers = null,
    IReadOnlyList<string>? ContentMarkers = null,
    bool AllSiblingMarkers = false,
    bool HomeOnly = false,
    bool Restorable = false)
{
    public bool IsCandidateForCleanup => Restorable;
}
