namespace WinClean.Core.Hardware.Smbios;

public sealed record MemoryDevice(
    ushort Handle,
    ushort ArrayHandle,
    long? SizeBytes,
    string DeviceLocator,
    string BankLocator,
    byte TypeCode,
    byte FormFactorCode,
    int? SpeedMTps,
    int? ConfiguredSpeedMTps,
    int? ConfiguredVoltageMillivolts,
    string Manufacturer,
    string SerialNumber,
    string PartNumber)
{
    /// <summary>Empty slots are reported too; they have a locator but no size.</summary>
    public bool IsPopulated => SizeBytes > 0;

    public string TypeName => SmbiosNames.MemoryType(TypeCode);

    public string FormFactorName => SmbiosNames.FormFactor(FormFactorCode);
}
