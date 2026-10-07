using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Resources;

namespace WinClean.ViewModels;

public sealed partial class VolumeItem : ObservableObject
{
    private const double CriticalFreePercent = 10;

    public VolumeItem(VolumeSample sample)
    {
        Root = sample.Root;
        Title = string.Empty;
        FreeText = string.Empty;
        Update(sample);
    }

    public string Root { get; }

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    public partial double UsedPercent { get; private set; }

    [ObservableProperty]
    public partial string FreeText { get; private set; }

    [ObservableProperty]
    public partial bool IsCritical { get; private set; }

    public void Update(VolumeSample sample)
    {
        Title = sample.Label.Length == 0
            ? sample.Letter
            : string.Format(CultureInfo.CurrentCulture, Strings.Overview_VolumeLabel, sample.Label, sample.Letter);
        UsedPercent = sample.UsedPercent;
        FreeText = string.Format(CultureInfo.CurrentCulture, Strings.Overview_FreeOfTotal, ByteSize.Format(sample.Free), ByteSize.Format(sample.Total));
        IsCritical = sample.Total > 0 && sample.FreePercent < CriticalFreePercent;
    }
}
