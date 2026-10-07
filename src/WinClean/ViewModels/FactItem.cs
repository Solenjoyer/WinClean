using CommunityToolkit.Mvvm.ComponentModel;

namespace WinClean.ViewModels;

/// <summary>One label and value line in a details pane or a hardware card.</summary>
public sealed partial class FactItem : ObservableObject
{
    public FactItem(string label)
    {
        Label = label;
        Value = string.Empty;
    }

    public string Label { get; }

    [ObservableProperty]
    public partial string Value { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;
}
