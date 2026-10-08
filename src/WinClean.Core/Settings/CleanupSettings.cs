namespace WinClean.Core.Settings;

public sealed record CleanupSettings
{
    /// <summary>Files in temporary folders younger than this are left alone so running installers are not disturbed.</summary>
    public int TemporaryFileMinimumAgeHours { get; set; } = 24;

    /// <summary>Developer artifacts whose project has not changed for this long are offered as stale.</summary>
    public int StaleArtifactDays { get; set; } = 90;

    /// <summary>Installers in Downloads older than this are offered for review.</summary>
    public int OldDownloadDays { get; set; } = 90;
}
