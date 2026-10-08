namespace WinClean.Core.Monitoring;

public sealed record DiskActivitySample(double ReadBytesPerSecond, double WriteBytesPerSecond, double ActivePercent, int DiskCount);
