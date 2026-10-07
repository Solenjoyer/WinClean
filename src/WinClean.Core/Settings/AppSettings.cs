namespace WinClean.Core.Settings;

/// <summary>
/// Everything the user can configure. Persisted as settings.json; every property has a sensible default.
/// Properties have plain setters on purpose: the JSON source generator fills init-only members like
/// constructor parameters, which would turn every key missing from the file into the type's default
/// instead of the declared one.
/// </summary>
public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public const int MinimumRefreshIntervalSeconds = 1;

    public const int MaximumRefreshIntervalSeconds = 10;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public ThemePreference Theme { get; set; } = ThemePreference.System;

    public int RefreshIntervalSeconds { get; set; } = 1;

    public bool ShowSystemProcesses { get; set; }

    public bool GroupByApplication { get; set; } = true;

    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

    /// <summary>Hardware sensors are opt-in: CPU readings need the PawnIO driver and administrator rights.</summary>
    public bool SensorsEnabled { get; set; }

    public bool StartWithWindows { get; set; }

    public bool StartMinimized { get; set; }

    public bool VerboseLogging { get; set; }

    public bool CompactNavigation { get; set; }

    public TraySettings Tray { get; set; } = new();

    public CleanupSettings Cleanup { get; set; } = new();

    public WindowPlacement? Window { get; set; }
}
