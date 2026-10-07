using System.Windows;
using WinClean.Core.Settings;

namespace WinClean.Services.Shell;

/// <summary>
/// Keeps the application's own colour dictionary in step with the Fluent theme. The Fluent theme swaps
/// its brushes itself; the series colours defined by WinClean are swapped here.
/// </summary>
internal static class ThemeResources
{
    private static readonly Uri LightColors = new("Resources/Colors.Light.xaml", UriKind.Relative);

    private static readonly Uri DarkColors = new("Resources/Colors.Dark.xaml", UriKind.Relative);

    public static void Apply(Application application, ThemePreference preference)
    {
        ArgumentNullException.ThrowIfNull(application);

        var dark = preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => WindowsTheme.AppsUseDarkTheme(),
        };

        application.ThemeMode = preference switch
        {
            ThemePreference.Dark => ThemeMode.Dark,
            ThemePreference.Light => ThemeMode.Light,
            _ => ThemeMode.System,
        };

        var dictionaries = application.Resources.MergedDictionaries;
        var colors = new ResourceDictionary { Source = dark ? DarkColors : LightColors };

        if (dictionaries.Count > 0 && dictionaries[0].Source is { } source && (source == LightColors || source == DarkColors))
        {
            dictionaries[0] = colors;
        }
        else
        {
            dictionaries.Insert(0, colors);
        }
    }
}
