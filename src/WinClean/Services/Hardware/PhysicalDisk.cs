namespace WinClean.Services.Hardware;

public sealed record PhysicalDisk(int Number, string Model, string BusName, string SerialNumber, long Capacity, bool? IsSolidState, bool? TrimEnabled, bool Removable, IReadOnlyList<string> Volumes);
