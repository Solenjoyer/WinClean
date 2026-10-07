using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Resources;
using WinClean.Services.Monitoring;

namespace WinClean.ViewModels;

public sealed partial class OverviewViewModel : ObservableObject
{
    private const string Pending = "—";

    private readonly MonitoringScheduler _scheduler;

    public OverviewViewModel(MonitoringScheduler scheduler)
    {
        _scheduler = scheduler;
        History = scheduler.History;

        CpuValue = Pending;
        CpuCaption = string.Format(CultureInfo.CurrentCulture, Strings.Overview_Processors, scheduler.ProcessorCount);
        MemoryValue = Pending;
        MemoryCaption = string.Empty;
        DiskValue = Pending;
        DiskCaption = string.Empty;
        NetworkValue = Pending;
        NetworkCaption = string.Empty;
        GpuValue = Pending;
        GpuCaption = string.Empty;
        UptimeText = Pending;
        ProcessesText = Pending;
        CommitText = Pending;
        BatteryText = string.Empty;

        scheduler.SampleReady += OnSample;
    }

    public MetricHistory History { get; }

    public ObservableCollection<VolumeItem> Volumes { get; } = [];

    [ObservableProperty]
    public partial int HistoryVersion { get; private set; }

    [ObservableProperty]
    public partial string CpuValue { get; private set; }

    [ObservableProperty]
    public partial string CpuCaption { get; private set; }

    [ObservableProperty]
    public partial string MemoryValue { get; private set; }

    [ObservableProperty]
    public partial string MemoryCaption { get; private set; }

    [ObservableProperty]
    public partial string DiskValue { get; private set; }

    [ObservableProperty]
    public partial string DiskCaption { get; private set; }

    [ObservableProperty]
    public partial string NetworkValue { get; private set; }

    [ObservableProperty]
    public partial string NetworkCaption { get; private set; }

    [ObservableProperty]
    public partial bool GpuVisible { get; private set; }

    [ObservableProperty]
    public partial string GpuValue { get; private set; }

    [ObservableProperty]
    public partial string GpuCaption { get; private set; }

    [ObservableProperty]
    public partial string UptimeText { get; private set; }

    [ObservableProperty]
    public partial string ProcessesText { get; private set; }

    [ObservableProperty]
    public partial string CommitText { get; private set; }

    [ObservableProperty]
    public partial bool BatteryVisible { get; private set; }

    [ObservableProperty]
    public partial string BatteryText { get; private set; }

    private void OnSample(object? sender, SystemSample sample)
    {
        var culture = CultureInfo.CurrentCulture;

        CpuValue = sample.CpuPercent is { } cpu ? Percent.Format(cpu, 0) : Pending;
        CpuCaption = sample.CpuFrequencyMHz is { } megahertz
            ? string.Format(culture, Strings.Overview_FrequencyAndProcessors, (megahertz / 1000).ToString("0.00", culture), sample.ProcessorCount)
            : string.Format(culture, Strings.Overview_Processors, sample.ProcessorCount);

        if (sample.Memory is { } memory)
        {
            var unit = ByteSize.UnitFor(memory.Total);
            MemoryValue = ByteSize.Format(memory.Used, unit);
            MemoryCaption = string.Format(culture, Strings.Overview_OfTotalAvailable, ByteSize.Format(memory.Total, unit), ByteSize.Format(memory.Available));
            ProcessesText = string.Format(culture, Strings.Overview_ProcessesThreadsHandles, memory.Processes.ToString("N0", culture), memory.Threads.ToString("N0", culture), memory.Handles.ToString("N0", culture));
            CommitText = string.Format(culture, Strings.Overview_CommitOfLimit, ByteSize.Format(memory.CommitTotal), ByteSize.Format(memory.CommitLimit));
        }

        var systemVolume = sample.Volumes.FirstOrDefault(volume => volume.IsSystemVolume) ?? sample.Volumes.FirstOrDefault(volume => volume.Kind == VolumeKind.Fixed);

        if (systemVolume is not null)
        {
            DiskValue = Percent.Format(systemVolume.UsedPercent, 0);
            DiskCaption = sample.Disk is { } disk
                ? string.Format(culture, Strings.Overview_ReadWrite, Rate.Format(disk.ReadBytesPerSecond), Rate.Format(disk.WriteBytesPerSecond))
                : string.Format(culture, Strings.Overview_FreeOfTotal, ByteSize.Format(systemVolume.Free), ByteSize.Format(systemVolume.Total));
        }

        if (sample.Network is { } network)
        {
            NetworkValue = Rate.Format(network.ReceiveBytesPerSecond + network.SendBytesPerSecond);
            NetworkCaption = string.Format(culture, Strings.Overview_DownUp, Rate.Format(network.ReceiveBytesPerSecond), Rate.Format(network.SendBytesPerSecond));
        }

        if (sample.PrimaryGpu is { } gpu)
        {
            GpuVisible = true;
            GpuValue = Percent.Format(gpu.UtilizationPercent, 0);
            GpuCaption = string.Format(culture, Strings.Overview_DedicatedMemoryInUse, ByteSize.Format(gpu.DedicatedUsed));
        }

        UptimeText = Durations.Format(sample.Uptime);

        if (sample.Battery is { } battery)
        {
            BatteryVisible = true;
            var level = battery.Percent is { } percent ? Percent.Format(percent, 0) : Pending;
            BatteryText = battery.Charging
                ? string.Format(culture, Strings.Overview_BatteryCharging, level)
                : battery.OnAcPower
                    ? string.Format(culture, Strings.Overview_BatteryPluggedIn, level)
                    : battery.Remaining is { } remaining
                        ? string.Format(culture, Strings.Overview_BatteryRemaining, level, Durations.Format(remaining))
                        : level;
        }
        else
        {
            BatteryVisible = false;
        }

        UpdateVolumes(sample.Volumes);
        HistoryVersion = History.Cpu.Version;
    }

    private void UpdateVolumes(IReadOnlyList<VolumeSample> volumes)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sample in volumes)
        {
            if (!sample.CanBeScanned && sample.Kind != VolumeKind.Network)
            {
                continue;
            }

            seen.Add(sample.Root);
            var existing = Volumes.FirstOrDefault(item => string.Equals(item.Root, sample.Root, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                Volumes.Add(new VolumeItem(sample));
            }
            else
            {
                existing.Update(sample);
            }
        }

        for (var index = Volumes.Count - 1; index >= 0; index--)
        {
            if (!seen.Contains(Volumes[index].Root))
            {
                Volumes.RemoveAt(index);
            }
        }
    }
}
