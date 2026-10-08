namespace WinClean.Core.Storage;

/// <summary>
/// A well-known folder worth measuring, and possibly cleaning. Path templates use %VARIABLE% names and
/// a * segment for per-profile or per-version folders.
/// </summary>
public sealed record KnownLocation(
    string Id,
    LocationGroup Group,
    string DisplayName,
    string Description,
    IReadOnlyList<string> PathTemplates,
    LocationKind Kind,
    CleanupRisk Risk,
    bool RequiresElevation = false,
    bool DefaultSelected = false,
    IReadOnlyList<string>? FilePatterns = null,
    bool UsesTemporaryFileAge = false,
    IReadOnlyList<string>? ConflictingProcesses = null,
    bool RecycleInsteadOfDelete = false,
    string? Note = null,
    string? Command = null)
{
    public bool IsCleanable => Kind == LocationKind.Cleanable;
}
