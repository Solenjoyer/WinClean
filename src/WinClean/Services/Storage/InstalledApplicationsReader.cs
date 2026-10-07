using System.Globalization;
using Microsoft.Win32;

namespace WinClean.Services.Storage;

/// <summary>What "Installed apps" lists, read from the Uninstall keys of both registry views and both hives.</summary>
internal static class InstalledApplicationsReader
{
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    public static IReadOnlyList<InstalledApplication> Read()
    {
        var applications = new Dictionary<string, InstalledApplication>(StringComparer.OrdinalIgnoreCase);

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var uninstall = root.OpenSubKey(UninstallKey);

                if (uninstall is null)
                {
                    continue;
                }

                foreach (var name in uninstall.GetSubKeyNames())
                {
                    using var key = uninstall.OpenSubKey(name);

                    if (key is not null && Read(key) is { } application)
                    {
                        applications.TryAdd(application.Name + "|" + application.Version, application);
                    }
                }
            }
        }

        return applications.Values.OrderBy(application => application.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static InstalledApplication? Read(RegistryKey key)
    {
        if (key.GetValue("DisplayName") is not string name || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        // Components of other products and hidden entries do not appear in Installed apps either.
        if (key.GetValue("SystemComponent") is int component && component == 1 || key.GetValue("ParentKeyName") is string)
        {
            return null;
        }

        var size = key.GetValue("EstimatedSize") is int kilobytes && kilobytes > 0 ? kilobytes * 1024L : (long?)null;
        var location = key.GetValue("InstallLocation") as string;
        DateTime? installDate = null;

        if (key.GetValue("InstallDate") is string date
            && DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            installDate = parsed;
        }

        return new InstalledApplication(
            name.Trim(),
            key.GetValue("DisplayVersion") as string,
            key.GetValue("Publisher") as string,
            size,
            string.IsNullOrWhiteSpace(location) ? null : location.Trim().TrimEnd('\\'),
            installDate);
    }
}
