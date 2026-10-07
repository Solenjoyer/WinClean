using System.Text.Json;

namespace WinClean.Core.Settings;

public static class SettingsSerializer
{
    public static string Serialize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings);
    }

    /// <summary>Never throws: a missing, empty or damaged file yields defaults plus a message worth logging.</summary>
    public static SettingsLoadResult Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SettingsLoadResult(new AppSettings(), "The settings file is empty.");
        }

        try
        {
            var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings);
            return settings is null
                ? new SettingsLoadResult(new AppSettings(), "The settings file does not contain an object.")
                : new SettingsLoadResult(Normalize(settings), null);
        }
        catch (JsonException exception)
        {
            return new SettingsLoadResult(new AppSettings(), $"The settings file could not be read: {exception.Message}");
        }
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        return settings with
        {
            SchemaVersion = AppSettings.CurrentSchemaVersion,
            RefreshIntervalSeconds = Math.Clamp(
                settings.RefreshIntervalSeconds,
                AppSettings.MinimumRefreshIntervalSeconds,
                AppSettings.MaximumRefreshIntervalSeconds),
            Tray = settings.Tray ?? new TraySettings(),
            Cleanup = settings.Cleanup ?? new CleanupSettings(),
        };
    }
}
