namespace WinClean.Core.Monitoring;

public enum SensorKind
{
    Temperature,
    Fan,
    Power,
}

/// <summary>One sensor value from the optional hardware monitoring library, in degrees Celsius, RPM or watts.</summary>
public sealed record SensorReading(string Hardware, SensorHardwareKind HardwareKind, string Name, SensorKind Kind, double Value);

public enum SensorHardwareKind
{
    Cpu,
    Gpu,
    Motherboard,
    Storage,
    Memory,
    Other,
}
