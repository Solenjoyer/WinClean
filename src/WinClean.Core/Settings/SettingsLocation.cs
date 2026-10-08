namespace WinClean.Core.Settings;

/// <summary>
/// Where settings and logs live. Next to the executable when a file named "portable" sits beside it,
/// otherwise under the user's local application data.
/// </summary>
public sealed record SettingsLocation(string DataDirectory, bool IsPortable)
{
    public const string PortableMarkerFileName = "portable";

    public const string SettingsFileName = "settings.json";

    public string SettingsFile => Path.Combine(DataDirectory, SettingsFileName);

    public string LogDirectory => Path.Combine(DataDirectory, "logs");

    public static SettingsLocation Resolve(string applicationDirectory, string localAppDataDirectory, bool portableMarkerExists)
    {
        ArgumentException.ThrowIfNullOrEmpty(applicationDirectory);
        ArgumentException.ThrowIfNullOrEmpty(localAppDataDirectory);

        return portableMarkerExists
            ? new SettingsLocation(Path.Combine(applicationDirectory, "data"), IsPortable: true)
            : new SettingsLocation(Path.Combine(localAppDataDirectory, "WinClean"), IsPortable: false);
    }
}
