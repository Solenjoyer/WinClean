using WinClean.Core.Cleanup;
using WinClean.Core.Storage;

namespace WinClean.Services.Cleanup;

/// <summary>What one cleanup category holds right now, down to the file.</summary>
public sealed record CategoryDiscovery(
    string Id,
    LocationGroup Group,
    string DisplayName,
    string Description,
    CleanupRisk Risk,
    bool RequiresElevation,
    bool DefaultSelected,
    bool AllOrNothing,
    IReadOnlyList<CleanupItem> Items,
    long Bytes,
    IReadOnlyList<string> RunningConflicts,
    string? Note,
    bool Unavailable = false)
{
    public int Count => Items.Count;
}
