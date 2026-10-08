namespace WinClean.Core.Settings;

/// <summary>Where the desktop widget sits in the window order.</summary>
public enum WidgetPlacement
{
    /// <summary>Above every other window.</summary>
    AlwaysOnTop,

    /// <summary>Under every other window and above the wallpaper. "Show desktop" hides it until the desktop is toggled back.</summary>
    BehindWindows,

    /// <summary>An ordinary window that others can cover.</summary>
    Normal,
}
