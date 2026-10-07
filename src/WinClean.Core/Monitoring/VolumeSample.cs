namespace WinClean.Core.Monitoring;

public sealed record VolumeSample(
    string Root,
    string Label,
    string FileSystem,
    VolumeKind Kind,
    long Total,
    long Free,
    bool IsSystemVolume)
{
    /// <summary>"C:" for "C:\".</summary>
    public string Letter => Root.TrimEnd('\\');

    public long Used => Total - Free;

    public double UsedPercent => Total <= 0 ? 0 : Used * 100.0 / Total;

    public double FreePercent => Total <= 0 ? 0 : Free * 100.0 / Total;

    public bool CanBeScanned => Kind is VolumeKind.Fixed or VolumeKind.Removable or VolumeKind.RamDisk;
}
