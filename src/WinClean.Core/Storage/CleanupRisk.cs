namespace WinClean.Core.Storage;

public enum CleanupRisk
{
    /// <summary>Regenerated or re-downloaded on demand with no visible side effect.</summary>
    Safe,

    /// <summary>Removable, but something is lost or has to be rebuilt: diagnostics, indexes, shader caches.</summary>
    Caution,

    /// <summary>Only for people who know what the folder is for.</summary>
    Advanced,
}
