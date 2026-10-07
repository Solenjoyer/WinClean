using Microsoft.Win32;

namespace WinClean.Services.Shell;

/// <summary>Reads the Windows personalization setting the Fluent theme follows in System mode.</summary>
internal static class WindowsTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool AppsUseDarkTheme() => ReadFlag("AppsUseLightTheme") is false;

    /// <summary>The taskbar follows the system colour, which can differ from the app colour.</summary>
    public static bool SystemUsesDarkTheme() => ReadFlag("SystemUsesLightTheme") is false;

    private static bool? ReadFlag(string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue(valueName) is int value ? value != 0 : null;
    }
}
