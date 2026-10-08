using System.Collections.ObjectModel;

namespace WinClean.ViewModels;

/// <summary>One card on the Hardware page: a title and its facts.</summary>
public sealed class HardwareCard
{
    public HardwareCard(string title, IEnumerable<FactItem> facts)
    {
        Title = title;
        Facts = new ObservableCollection<FactItem>(facts);
    }

    public string Title { get; }

    public ObservableCollection<FactItem> Facts { get; }
}
