using CommunityToolkit.Mvvm.ComponentModel;

namespace WinClean.ViewModels;

/// <summary>One application group on the widget's "Running now" list, updated in place between ticks.</summary>
public sealed partial class WidgetToolRow : ObservableObject
{
    public WidgetToolRow(string key)
    {
        Key = key;
        Name = string.Empty;
        CpuText = string.Empty;
        MemoryText = string.Empty;
    }

    public string Key { get; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string CpuText { get; set; }

    [ObservableProperty]
    public partial string MemoryText { get; set; }
}
