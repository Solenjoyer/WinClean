using WinClean.Core.Monitoring;

namespace WinClean.Services.Monitoring;

/// <summary>Sixty points of each headline metric, enough for the sparklines; NaN marks a gap in monitoring.</summary>
public sealed class MetricHistory
{
    public const int Points = 60;

    public RingBuffer<float> Cpu { get; } = new(Points);

    public RingBuffer<float> Memory { get; } = new(Points);

    public RingBuffer<float> DiskRead { get; } = new(Points);

    public RingBuffer<float> DiskWrite { get; } = new(Points);

    public RingBuffer<float> NetworkReceive { get; } = new(Points);

    public RingBuffer<float> NetworkSend { get; } = new(Points);

    public RingBuffer<float> Gpu { get; } = new(Points);

    internal void Push(SystemSample sample)
    {
        Cpu.Add((float?)sample.CpuPercent ?? float.NaN);
        Memory.Add((float?)sample.Memory?.UsedPercent ?? float.NaN);
        DiskRead.Add((float?)sample.Disk?.ReadBytesPerSecond ?? float.NaN);
        DiskWrite.Add((float?)sample.Disk?.WriteBytesPerSecond ?? float.NaN);
        NetworkReceive.Add((float?)sample.Network?.ReceiveBytesPerSecond ?? float.NaN);
        NetworkSend.Add((float?)sample.Network?.SendBytesPerSecond ?? float.NaN);
        Gpu.Add((float?)sample.PrimaryGpu?.UtilizationPercent ?? float.NaN);
    }

    internal void PushGap()
    {
        foreach (var buffer in new[] { Cpu, Memory, DiskRead, DiskWrite, NetworkReceive, NetworkSend, Gpu })
        {
            buffer.Add(float.NaN);
        }
    }
}
