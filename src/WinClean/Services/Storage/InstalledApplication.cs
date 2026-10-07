namespace WinClean.Services.Storage;

public sealed record InstalledApplication(string Name, string? Version, string? Publisher, long? EstimatedBytes, string? InstallLocation, DateTime? InstallDate);
