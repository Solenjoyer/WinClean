namespace WinClean.Services.Hardware;

public sealed record NetworkAdapterFacts(string Name, string Description, string Type, long SpeedBitsPerSecond, string PhysicalAddress, bool IsUp);
