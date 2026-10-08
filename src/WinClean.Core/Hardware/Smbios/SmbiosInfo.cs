namespace WinClean.Core.Hardware.Smbios;

public sealed record SmbiosInfo(
    Version SpecificationVersion,
    BiosInformation? Bios,
    SystemInformation? System,
    BaseboardInformation? Baseboard,
    IReadOnlyList<ProcessorInformation> Processors,
    IReadOnlyList<PhysicalMemoryArray> MemoryArrays,
    IReadOnlyList<MemoryDevice> MemoryDevices)
{
    public static SmbiosInfo Empty { get; } = new(new Version(0, 0), null, null, null, [], [], []);

    /// <summary>Installed memory as the firmware reports it: the sum of populated modules.</summary>
    public long InstalledMemoryBytes => MemoryDevices.Sum(device => device.SizeBytes ?? 0);
}
