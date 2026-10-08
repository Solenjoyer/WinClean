namespace WinClean.Core.Hardware.Smbios;

public sealed record ProcessorInformation(
    string SocketDesignation,
    string Manufacturer,
    string Version,
    int ExternalClockMHz,
    int MaxSpeedMHz,
    int CurrentSpeedMHz,
    int CoreCount,
    int ThreadCount,
    bool Populated);
