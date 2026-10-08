namespace WinClean.Core.Monitoring;

public sealed record NetworkSample(double ReceiveBytesPerSecond, double SendBytesPerSecond, IReadOnlyList<AdapterSample> Adapters);
