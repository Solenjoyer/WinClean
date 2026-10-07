namespace WinClean.Core.Monitoring;

public sealed record AdapterSample(
    long Luid,
    string Name,
    string Description,
    double ReceiveBytesPerSecond,
    double SendBytesPerSecond,
    ulong LinkSpeedBitsPerSecond,
    bool IsPhysical);
