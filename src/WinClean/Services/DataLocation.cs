using System.IO;
using WinClean.Core.Settings;

namespace WinClean.Services;

internal static class DataLocation
{
    /// <summary>Portable when a file named "portable" sits next to the executable, otherwise per-user.</summary>
    public static SettingsLocation Detect()
    {
        var applicationDirectory = AppContext.BaseDirectory;
        var marker = Path.Combine(applicationDirectory, SettingsLocation.PortableMarkerFileName);

        return SettingsLocation.Resolve(
            applicationDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            File.Exists(marker));
    }
}
