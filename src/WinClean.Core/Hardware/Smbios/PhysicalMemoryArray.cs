namespace WinClean.Core.Hardware.Smbios;

public sealed record PhysicalMemoryArray(
    ushort Handle,
    long? MaximumCapacityBytes,
    int DeviceCount,
    bool IsSystemMemory,
    bool HasErrorCorrection);
