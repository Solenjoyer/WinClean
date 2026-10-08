using CommunityToolkit.Mvvm.ComponentModel;

namespace WinClean.ViewModels;

/// <summary>One entry in the navigation rail. The key is stable and used by --page and the tray menu.</summary>
public sealed class NavigationItem
{
    public NavigationItem(string key, string title, string glyph, ObservableObject content)
    {
        Key = key;
        Title = title;
        Glyph = glyph;
        Content = content;
    }

    public string Key { get; }

    public string Title { get; }

    public string Glyph { get; }

    public ObservableObject Content { get; }
}
