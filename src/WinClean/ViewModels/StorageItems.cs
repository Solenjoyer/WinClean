using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Resources;

namespace WinClean.ViewModels;

/// <summary>A drive card on the Storage page, updated in place from the monitor's volume list.</summary>
public sealed partial class DriveItem : ObservableObject
{
    private const double CriticalFreePercent = 10;

    public DriveItem(VolumeSample sample)
    {
        Root = sample.Root;
        Title = string.Empty;
        KindText = string.Empty;
        FreeText = string.Empty;
        Update(sample);
    }

    public string Root { get; }

    public bool CanScan { get; private set; }

    public bool IsFast { get; private set; }

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    public partial string KindText { get; private set; }

    [ObservableProperty]
    public partial double UsedPercent { get; private set; }

    [ObservableProperty]
    public partial string FreeText { get; private set; }

    [ObservableProperty]
    public partial bool IsCritical { get; private set; }

    public void Update(VolumeSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        Title = sample.Label.Length == 0 ? sample.Letter : string.Format(CultureInfo.CurrentCulture, Strings.Overview_VolumeLabel, sample.Label, sample.Letter);
        var kind = sample.Kind switch
        {
            VolumeKind.Fixed => Strings.Storage_DriveKindFixed,
            VolumeKind.Removable => Strings.Storage_DriveKindRemovable,
            VolumeKind.Network => Strings.Storage_DriveKindNetwork,
            VolumeKind.Optical => Strings.Storage_DriveKindOptical,
            VolumeKind.RamDisk => Strings.Storage_DriveKindRamDisk,
            _ => string.Empty,
        };
        KindText = sample.FileSystem.Length == 0 ? kind : $"{kind}, {sample.FileSystem}";
        UsedPercent = sample.UsedPercent;
        FreeText = string.Format(CultureInfo.CurrentCulture, Strings.Storage_FreeOfTotal, ByteSize.Format(sample.Free), ByteSize.Format(sample.Total));
        IsCritical = sample.Total > 0 && sample.FreePercent < CriticalFreePercent;
        CanScan = sample.Kind is VolumeKind.Fixed or VolumeKind.Removable or VolumeKind.RamDisk or VolumeKind.Network;
        IsFast = sample.Kind is VolumeKind.Fixed or VolumeKind.RamDisk;
    }
}

public sealed record BreadcrumbItem(int Node, string Name);

public sealed record FolderItem(int Node, string Name, string Path, long Bytes, string SizeText, double Percent, string Caption, bool CanEnter);

public sealed record FileItem(string Name, string Path, string Directory, string SizeText, string ModifiedText);

public sealed record CategoryItem(string Name, string SizeText, string CountText, double Percent);

public sealed record ApplicationItem(string Name, string Publisher, string Version, string SizeText, string InstalledText, string? Location);

public sealed record ArtifactItem(string Path, string Name, string Kind, string Description, string SizeText, string ChangedText, bool IsStale);

public sealed record LocationItem(string Name, string Group, string Description, string? Note, string SizeText, string CountText, bool Exists, IReadOnlyList<string> Paths);
