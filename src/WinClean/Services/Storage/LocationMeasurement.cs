using WinClean.Core.Storage;

namespace WinClean.Services.Storage;

/// <summary>The size of one known location on this machine, with the paths it resolved to.</summary>
public sealed record LocationMeasurement(KnownLocation Location, IReadOnlyList<string> Paths, long Bytes, int Files)
{
    public bool Exists => Paths.Count > 0;
}
