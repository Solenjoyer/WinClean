namespace WinClean.Core.Hardware.Smbios;

public sealed record SystemInformation(
    string Manufacturer,
    string ProductName,
    string Version,
    string SerialNumber,
    Guid? Uuid,
    string SkuNumber,
    string Family);
