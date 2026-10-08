namespace WinClean.Core.Health;

/// <summary>One present device and the driver it runs, as Device Manager would list it.</summary>
public sealed record DriverInfo(
    string Device,
    string? ClassName,
    DeviceGroup Group,
    string? Provider,
    string? Version,
    DateOnly? Date,
    string? InfPath,
    string? Manufacturer,
    IReadOnlyList<string> HardwareIds,
    int ProblemCode)
{
    public string DisplayVersion => DriverVersionFormat.Display(Provider, Version);

    public DriverSource? Source => DriverSources.Find(HardwareIds, Provider);

    public string? Problem => CmProblemCodes.Describe(ProblemCode);
}
