namespace WinClean.Core.Monitoring;

public sealed record GpuSample(long Luid, string? Name, double UtilizationPercent, long DedicatedUsed, long? DedicatedTotal, long SharedUsed);
