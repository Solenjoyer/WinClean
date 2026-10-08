using CommunityToolkit.Mvvm.ComponentModel;

namespace WinClean.ViewModels;

/// <summary>One line of the Overview's memory list, updated in place so the list never rebuilds.</summary>
public sealed partial class TopApplicationItem : ObservableObject
{
    public TopApplicationItem(string key)
    {
        Key = key;
        Name = string.Empty;
        Category = string.Empty;
        MemoryText = string.Empty;
    }

    public string Key { get; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Category { get; set; }

    [ObservableProperty]
    public partial string MemoryText { get; set; }

    [ObservableProperty]
    public partial double Percent { get; set; }
}
