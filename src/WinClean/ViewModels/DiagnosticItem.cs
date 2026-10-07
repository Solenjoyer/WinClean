using CommunityToolkit.Mvvm.ComponentModel;

namespace WinClean.ViewModels;

public sealed partial class DiagnosticItem : ObservableObject
{
    public DiagnosticItem(string name, string status)
    {
        Name = name;
        Status = status;
    }

    public string Name { get; }

    [ObservableProperty]
    public partial string Status { get; set; }
}
