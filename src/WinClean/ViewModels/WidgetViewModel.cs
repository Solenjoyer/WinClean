using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Applications;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Core.Settings;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Monitoring;

namespace WinClean.ViewModels;

/// <summary>
/// The figures on the desktop widget: the headline metrics with their history, the developer
/// tools and agents running right now, and the system drive. It reads the same samples as the pages.
/// </summary>
public sealed partial class WidgetViewModel : ObservableObject, IDisposable
{
    private const int ToolRows = 4;

    private const string Pending = "—";

    private static readonly ApplicationCategory[] ToolCategories =
    [
        ApplicationCategory.AiAgent,
        ApplicationCategory.Ide,
        ApplicationCategory.Editor,
        ApplicationCategory.Container,
        ApplicationCategory.VirtualMachine,
        ApplicationCategory.Runtime,
    ];

    private readonly MonitoringScheduler? _scheduler;

    private readonly SettingsStore? _settings;

    private WidgetSettings _widget;

    private bool _sensorsEnabled;

    private TemperatureUnit _unit;

    public WidgetViewModel(MonitoringScheduler scheduler, SettingsStore settings)
        : this(scheduler.History, settings.Current)
    {
        _scheduler = scheduler;
        _settings = settings;
        _scheduler.SampleReady += OnSample;
        _settings.Changed += OnSettingsChanged;
    }

    /// <summary>For tests: the figures without a sampler behind them.</summary>
    internal WidgetViewModel(MetricHistory history, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        History = history;
        _widget = settings.Widget;
        CpuValue = Pending;
        MemoryValue = Pending;
        DiskValue = Pending;
        NetworkValue = Pending;
        GpuValue = Pending;
        TemperatureValue = Pending;
        CompactText = Pending;
        StorageText = string.Empty;
        ApplySettings(settings);
    }

    public MetricHistory History { get; }

    public ObservableCollection<WidgetToolRow> Tools { get; } = [];

    [ObservableProperty]
    public partial int HistoryVersion { get; private set; }

    [ObservableProperty]
    public partial string CpuValue { get; private set; }

    [ObservableProperty]
    public partial string MemoryValue { get; private set; }

    [ObservableProperty]
    public partial string DiskValue { get; private set; }

    [ObservableProperty]
    public partial string NetworkValue { get; private set; }

    [ObservableProperty]
    public partial string GpuValue { get; private set; }

    [ObservableProperty]
    public partial bool GpuVisible { get; private set; }

    [ObservableProperty]
    public partial string TemperatureValue { get; private set; }

    [ObservableProperty]
    public partial bool TemperatureVisible { get; private set; }

    [ObservableProperty]
    public partial string CompactText { get; private set; }

    [ObservableProperty]
    public partial string StorageText { get; private set; }

    [ObservableProperty]
    public partial bool StorageVisible { get; private set; }

    [ObservableProperty]
    public partial bool ToolsVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsCompact { get; private set; }

    [ObservableProperty]
    public partial double Opacity { get; private set; }

    public void Dispose()
    {
        if (_scheduler is not null)
        {
            _scheduler.SampleReady -= OnSample;
        }

        if (_settings is not null)
        {
            _settings.Changed -= OnSettingsChanged;
        }
    }

    internal void Apply(SystemSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        var culture = CultureInfo.CurrentCulture;

        CpuValue = sample.CpuPercent is { } cpu ? Percent.Format(cpu, 0) : Pending;
        MemoryValue = sample.Memory is { } memory ? ByteSize.Format(memory.Used, ByteSize.UnitFor(memory.Total)) : Pending;
        DiskValue = sample.Disk is { } disk ? Rate.Format(disk.ReadBytesPerSecond + disk.WriteBytesPerSecond) : Pending;
        NetworkValue = sample.Network is { } network ? Rate.Format(network.ReceiveBytesPerSecond + network.SendBytesPerSecond) : Pending;
        CompactText = string.Format(culture, Strings.Widget_Compact, CpuValue, MemoryValue, DiskValue, NetworkValue);

        if (sample.PrimaryGpu is { } gpu)
        {
            GpuVisible = true;
            GpuValue = Percent.Format(gpu.UtilizationPercent, 0);
        }
        else
        {
            GpuVisible = false;
        }

        UpdateTemperature(sample.Sensors);
        UpdateStorage(sample.Volumes);
        UpdateTools(sample.Processes);
        HistoryVersion = History.Cpu.Version;
    }

    private void OnSample(object? sender, SystemSample sample) => Apply(sample);

    private void OnSettingsChanged(object? sender, AppSettings settings) => ApplySettings(settings);

    private void ApplySettings(AppSettings settings)
    {
        _widget = settings.Widget;
        _sensorsEnabled = settings.SensorsEnabled;
        _unit = settings.TemperatureUnit;
        IsCompact = _widget.Layout == WidgetLayout.Compact;
        Opacity = Math.Clamp(_widget.Opacity, WidgetSettings.MinimumOpacity, WidgetSettings.MaximumOpacity) / 100.0;

        if (!_widget.ShowsTools)
        {
            ToolsVisible = false;
        }

        if (!_widget.ShowsStorage)
        {
            StorageVisible = false;
        }

        if (!_sensorsEnabled)
        {
            TemperatureVisible = false;
        }
    }

    private void UpdateTemperature(IReadOnlyList<SensorReading>? readings)
    {
        if (!_sensorsEnabled || readings is null)
        {
            TemperatureVisible = false;
            return;
        }

        double? hottest = null;

        foreach (var reading in readings)
        {
            if (reading.HardwareKind == SensorHardwareKind.Cpu && reading.Kind == SensorKind.Temperature)
            {
                hottest = Math.Max(hottest ?? double.MinValue, reading.Value);
            }
        }

        if (hottest is not { } celsius)
        {
            TemperatureVisible = false;
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        TemperatureValue = _unit == TemperatureUnit.Fahrenheit
            ? (celsius * 9 / 5 + 32).ToString("0", culture) + " °F"
            : celsius.ToString("0", culture) + " °C";
        TemperatureVisible = true;
    }

    private void UpdateStorage(IReadOnlyList<VolumeSample> volumes)
    {
        var volume = volumes.FirstOrDefault(item => item.IsSystemVolume) ?? volumes.FirstOrDefault(item => item.Kind == VolumeKind.Fixed);

        if (!_widget.ShowsStorage || volume is null)
        {
            StorageVisible = false;
            return;
        }

        StorageText = string.Format(CultureInfo.CurrentCulture, Strings.Widget_FreeSpace, volume.Letter, ByteSize.Format(volume.Free));
        StorageVisible = true;
    }

    private void UpdateTools(ProcessSnapshot? snapshot)
    {
        if (!_widget.ShowsTools || snapshot is null)
        {
            ToolsVisible = false;
            return;
        }

        var byPid = new Dictionary<int, ProcessSample>(snapshot.Processes.Count);

        foreach (var process in snapshot.Processes)
        {
            byPid[process.Pid] = process;
        }

        var tools = new List<(ProcessGroup Group, double Cpu, long Memory)>();

        foreach (var group in snapshot.Grouping.Groups)
        {
            if (Array.IndexOf(ToolCategories, group.Category) < 0)
            {
                continue;
            }

            double cpu = 0;
            long memory = 0;

            foreach (var pid in group.MemberPids)
            {
                if (byPid.TryGetValue(pid, out var process))
                {
                    cpu += process.CpuPercent;
                    memory += process.PrivateWorkingSet;
                }
            }

            if (memory > 0)
            {
                tools.Add((group, Math.Min(cpu, 100), memory));
            }
        }

        var top = tools
            .OrderByDescending(tool => tool.Cpu)
            .ThenByDescending(tool => tool.Memory)
            .Take(ToolRows)
            .ToList();

        for (var index = 0; index < top.Count; index++)
        {
            var (group, cpu, memory) = top[index];
            WidgetToolRow row;

            if (index < Tools.Count && string.Equals(Tools[index].Key, group.Key, StringComparison.Ordinal))
            {
                row = Tools[index];
            }
            else
            {
                row = new WidgetToolRow(group.Key);

                if (index < Tools.Count)
                {
                    Tools[index] = row;
                }
                else
                {
                    Tools.Add(row);
                }
            }

            row.Name = group.DisplayName;
            row.CpuText = Percent.Format(cpu, 1);
            row.MemoryText = ByteSize.Format(memory);
        }

        while (Tools.Count > top.Count)
        {
            Tools.RemoveAt(Tools.Count - 1);
        }

        ToolsVisible = Tools.Count > 0;
    }
}
