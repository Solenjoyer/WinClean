using Microsoft.Win32;

namespace WinClean.Services.Shell;

/// <summary>The Run value of the current account: the only way WinClean starts by itself, and only when asked.</summary>
internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string ValueName = "WinClean";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is string;
    }

    public static void Apply(bool enabled, bool minimized)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("The executable path is unknown.");
        key.SetValue(ValueName, minimized ? $"\"{executable}\" --minimized" : $"\"{executable}\"");
    }
}
