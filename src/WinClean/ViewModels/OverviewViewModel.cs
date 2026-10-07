using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Core.Settings;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Monitoring;

namespace WinClean.ViewModels;

public sealed partial class OverviewViewModel : ObservableObject
{
    private const string Pending = "—";

    private const int TopApplicationCount = 8;

    private readonly MonitoringScheduler _scheduler;

    private readonly SettingsStore _settings;

    public OverviewViewModel(MonitoringScheduler scheduler, SettingsStore settings)
    {
        _scheduler = scheduler;
        _settings = settings;
        History = scheduler.History;
        TemperaturesText = string.Empty;

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

    public ObservableCollection<TopApplicationItem> TopApplications { get; } = [];

    [ObservableProperty]
    public partial bool HasTopApplications { get; private set; }

    [ObservableProperty]
    public partial string TemperaturesText { get; private set; }

    [ObservableProperty]
    public partial bool TemperaturesVisible { get; private set; }

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
        UpdateTopApplications(sample);
        UpdateTemperatures(sample.Sensors);
        HistoryVersion = History.Cpu.Version;
    }

    private void UpdateTopApplications(SystemSample sample)
    {
        if (sample.Processes is not { } snapshot || sample.Memory is not { } memory)
        {
            return;
        }

        var byPid = new Dictionary<int, ProcessSample>(snapshot.Processes.Count);

        foreach (var process in snapshot.Processes)
        {
            byPid[process.Pid] = process;
        }

        var top = snapshot.Grouping.Groups
            .Select(group => (Group: group, Memory: group.MemberPids.Sum(pid => byPid.TryGetValue(pid, out var process) ? process.PrivateWorkingSet : 0)))
            .Where(pair => pair.Memory > 0)
            .OrderByDescending(pair => pair.Memory)
            .Take(TopApplicationCount)
            .ToList();

        for (var index = 0; index < top.Count; index++)
        {
            var (group, bytes) = top[index];
            TopApplicationItem item;

            if (index < TopApplications.Count && string.Equals(TopApplications[index].Key, group.Key, StringComparison.Ordinal))
            {
                item = TopApplications[index];
            }
            else
            {
                item = new TopApplicationItem(group.Key);

                if (index < TopApplications.Count)
                {
                    TopApplications[index] = item;
                }
                else
                {
                    TopApplications.Add(item);
                }
            }

            item.Name = group.DisplayName;
            item.Category = group.Application is null ? string.Empty : CategoryNames.For(group.Category);
            item.MemoryText = ByteSize.Format(bytes);
            item.Percent = memory.Total > 0 ? 100.0 * bytes / memory.Total : 0;
        }

        while (TopApplications.Count > top.Count)
        {
            TopApplications.RemoveAt(TopApplications.Count - 1);
        }

        HasTopApplications = TopApplications.Count > 0;
    }

    private void UpdateTemperatures(IReadOnlyList<SensorReading>? readings)
    {
        if (!_settings.Current.SensorsEnabled)
        {
            TemperaturesVisible = false;
            return;
        }

        var parts = new List<string>();
        var unit = _settings.Current.TemperatureUnit;

        if (readings is not null)
        {
            foreach (var kind in new[] { SensorHardwareKind.Cpu, SensorHardwareKind.Gpu, SensorHardwareKind.Storage })
            {
                var temperatures = readings.Where(reading => reading.HardwareKind == kind && reading.Kind == SensorKind.Temperature).ToList();

                if (temperatures.Count > 0)
                {
                    var label = kind switch { SensorHardwareKind.Cpu => Strings.Overview_Cpu, SensorHardwareKind.Gpu => Strings.Overview_Gpu, _ => Strings.Overview_Disk };
                    var value = temperatures.Max(reading => reading.Value);
                    parts.Add(label + " " + (unit == TemperatureUnit.Fahrenheit ? (value * 9 / 5 + 32).ToString("0", CultureInfo.CurrentCulture) + " °F" : value.ToString("0", CultureInfo.CurrentCulture) + " °C"));
                }
            }
        }

        TemperaturesVisible = true;
        TemperaturesText = parts.Count == 0 ? Strings.NotAvailable : string.Join("   ", parts);
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
