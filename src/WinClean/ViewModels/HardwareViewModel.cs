using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinClean.Core.Formatting;
using WinClean.Core.Hardware;
using WinClean.Core.Hardware.Smbios;
using WinClean.Core.Health;
using WinClean.Core.Monitoring;
using WinClean.Core.Settings;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Hardware;
using WinClean.Services.Health;
using WinClean.Services.Monitoring;
using WinClean.Services.Shell;

namespace WinClean.ViewModels;

/// <summary>Everything the firmware, the kernel and the drivers say about the machine, in cards of plain facts.</summary>
public sealed partial class HardwareViewModel : ObservableObject
{
    private const string Mask = "••••";

    private readonly MonitoringScheduler _scheduler;

    private readonly SensorProvider _sensors;

    private readonly SettingsStore _settings;

    private readonly IClipboard _clipboard;

    private readonly ILogger<HardwareViewModel> _logger;

    private readonly List<(FactItem Fact, string Value)> _sensitive = [];

    private readonly FactItem _cpuTemperature = new(Strings.Hardware_Temperature);

    private readonly Dictionary<string, FactItem> _gpuTemperatures = new(StringComparer.OrdinalIgnoreCase);

    private HardwareCard? _sensorCard;

    private bool _loaded;

    public HardwareViewModel(MonitoringScheduler scheduler, SensorProvider sensors, SettingsStore settings, IClipboard clipboard, ILogger<HardwareViewModel> logger)
    {
        _scheduler = scheduler;
        _sensors = sensors;
        _settings = settings;
        _clipboard = clipboard;
        _logger = logger;
        scheduler.SampleReady += OnSample;
    }

    public ObservableCollection<HardwareCard> Cards { get; } = [];

    [ObservableProperty]
    public partial bool ShowIdentifiers { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    public async Task ActivateAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadAsync();
    }

    partial void OnShowIdentifiersChanged(bool value)
    {
        foreach (var (fact, raw) in _sensitive)
        {
            fact.Value = value ? raw : Mask;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private void CopySummary()
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"{AppInfo.Name} {AppInfo.Version} hardware summary, {DateTimeOffset.Now:u}");

        foreach (var card in Cards)
        {
            text.AppendLine();
            text.AppendLine(card.Title);

            foreach (var fact in card.Facts.Where(fact => fact.IsVisible))
            {
                text.Append("  ").Append(fact.Label.PadRight(22)).Append(": ").AppendLine(fact.Value);
            }
        }

        _clipboard.SetText(text.ToString());
    }

    private async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var volumes = _scheduler.Latest?.Volumes.Select(volume => volume.Root).ToList() ?? [];
            var facts = await Task.Run(() => new Facts(
                SmbiosReader.Read(),
                ProcessorReader.Read(),
                GpuReader.Read(),
                PhysicalDiskReader.Read(volumes),
                NetworkAdapterReader.Read(),
                DisplayReader.Read(),
                DriverInventory.Read(),
                FirmwareReader.IsUefi(),
                FirmwareReader.SecureBootEnabled(),
                TpmReader.Version()));
            Present(facts);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _logger.LogError(exception, "The hardware facts could not be read.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Present(Facts facts)
    {
        var culture = CultureInfo.CurrentCulture;
        Cards.Clear();
        _sensitive.Clear();
        _gpuTemperatures.Clear();

        var processor = facts.Processor;
        var topology = processor.Topology;
        var processorFacts = new List<FactItem>
        {
            Fact(Strings.Hardware_Name, processor.Name.Length == 0 ? Strings.NotAvailable : processor.Name),
            Fact(Strings.Hardware_CoresThreads, topology.IsHybrid
                ? string.Format(culture, Strings.Hardware_HybridCores, topology.PerformanceCores, topology.EfficiencyCores, topology.LogicalProcessors)
                : string.Format(culture, Strings.Hardware_CoresThreadsValue, topology.Cores, topology.LogicalProcessors)),
            Fact(Strings.Hardware_BaseClock, processor.BaseMHz > 0 ? (processor.BaseMHz / 1000.0).ToString("0.00", culture) + " GHz" : Strings.NotAvailable),
            Fact(Strings.Hardware_Caches, topology.Caches.Count == 0 ? Strings.NotAvailable : string.Join(", ", topology.Caches.Select(CacheText))),
            Fact(Strings.Hardware_Architecture, processor.Architecture),
            Fact(Strings.Hardware_Features, string.Join(", ", new[] { processor.Avx2 ? "AVX2" : null, processor.Avx512 ? "AVX-512" : null }.Where(feature => feature is not null)) is { Length: > 0 } features ? features : Strings.NotAvailable),
            Fact(Strings.Hardware_Virtualization, processor.VirtualizationEnabledInFirmware ? Strings.Hardware_Enabled : Strings.Hardware_Disabled),
            _cpuTemperature,
        };

        if (topology.Packages > 1)
        {
            processorFacts.Insert(2, Fact(Strings.Hardware_Packages, topology.Packages.ToString(culture)));
        }

        Cards.Add(new HardwareCard(Strings.Hardware_Processor, processorFacts));

        var displayDrivers = facts.Drivers.Where(driver => driver.Group == DeviceGroup.Display).ToList();

        foreach (var gpu in facts.Gpus)
        {
            var driver = displayDrivers.FirstOrDefault(driver => string.Equals(driver.Device, gpu.Name, StringComparison.OrdinalIgnoreCase));
            var temperature = new FactItem(Strings.Hardware_Temperature);
            _gpuTemperatures[gpu.Name] = temperature;

            Cards.Add(new HardwareCard(Strings.Hardware_Graphics,
            [
                Fact(Strings.Hardware_Name, gpu.Name),
                Fact(Strings.Hardware_DedicatedMemory, gpu.DedicatedVideoMemory > 0 ? ByteSize.Format(gpu.DedicatedVideoMemory) : Strings.NotAvailable),
                Fact(Strings.Hardware_SharedMemory, ByteSize.Format(gpu.SharedSystemMemory)),
                Fact(Strings.Hardware_Driver, driver is null ? Strings.NotAvailable : string.Format(culture, Strings.Hardware_DriverValue, driver.DisplayVersion, driver.Date?.ToString("d", culture) ?? Strings.NotAvailable)),
                temperature,
            ]));
        }

        var smbios = facts.Smbios;
        var modules = smbios.MemoryDevices;
        var memoryFacts = new List<FactItem>
        {
            Fact(Strings.Hardware_Installed, smbios.InstalledMemoryBytes > 0 ? ByteSize.Format(smbios.InstalledMemoryBytes) : Strings.NotAvailable),
            Fact(Strings.Hardware_SlotsUsed, modules.Count == 0 ? Strings.NotAvailable : string.Format(culture, Strings.Hardware_SlotsUsedValue, modules.Count(module => module.IsPopulated), modules.Count)),
        };

        var maximum = smbios.MemoryArrays.Where(array => array.IsSystemMemory).Sum(array => array.MaximumCapacityBytes ?? 0);

        if (maximum > 0)
        {
            memoryFacts.Add(Fact(Strings.Hardware_MaximumCapacity, ByteSize.Format(maximum)));
        }

        foreach (var module in modules)
        {
            var label = module.DeviceLocator.Length == 0 ? module.BankLocator : module.DeviceLocator;

            if (!module.IsPopulated)
            {
                memoryFacts.Add(Fact(label, Strings.Hardware_EmptySlot));
                continue;
            }

            var size = ByteSize.Format(module.SizeBytes ?? 0);
            var speed = module.SpeedMTps?.ToString(culture) ?? Strings.NotAvailable;
            var value = module.ConfiguredSpeedMTps is { } configured && configured != module.SpeedMTps
                ? string.Format(culture, Strings.Hardware_ModuleConfigured, size, module.TypeName, speed, configured)
                : string.Format(culture, Strings.Hardware_ModuleValue, size, module.TypeName, speed);
            var detail = string.Format(culture, Strings.Hardware_ModuleDetail, module.Manufacturer, module.PartNumber).Trim();
            memoryFacts.Add(Fact(label, detail.Length == 0 ? value : value + ", " + detail));

            if (module.SerialNumber.Length > 0)
            {
                memoryFacts.Add(Sensitive(label + " " + Strings.Hardware_SerialNumber.ToLowerInvariant(), module.SerialNumber));
            }
        }

        Cards.Add(new HardwareCard(Strings.Hardware_Memory, memoryFacts));

        if (smbios.Baseboard is { } board)
        {
            Cards.Add(new HardwareCard(Strings.Hardware_Motherboard,
            [
                Fact(Strings.Hardware_Manufacturer, board.Manufacturer),
                Fact(Strings.Hardware_Product, board.Product),
                Fact(Strings.Hardware_Version, board.Version),
                Sensitive(Strings.Hardware_SerialNumber, board.SerialNumber),
            ]));
        }

        if (smbios.System is { } system)
        {
            Cards.Add(new HardwareCard(Strings.Hardware_System,
            [
                Fact(Strings.Hardware_Manufacturer, system.Manufacturer),
                Fact(Strings.Hardware_Product, system.ProductName),
                Fact(Strings.Hardware_Family, system.Family),
                Sensitive(Strings.Hardware_SerialNumber, system.SerialNumber),
                Sensitive(Strings.Hardware_Uuid, system.Uuid?.ToString() ?? string.Empty),
            ]));
        }

        var bios = smbios.Bios;
        Cards.Add(new HardwareCard(Strings.Hardware_Firmware,
        [
            Fact(Strings.Hardware_Vendor, bios?.Vendor ?? Strings.NotAvailable),
            Fact(Strings.Hardware_Version, bios?.Version ?? Strings.NotAvailable),
            Fact(Strings.Hardware_ReleaseDate, bios?.ReleaseDate ?? Strings.NotAvailable),
            Fact(Strings.Hardware_Mode, facts.Uefi switch { true => Strings.Health_FirmwareUefi, false => Strings.Health_FirmwareBios, null => Strings.NotAvailable }),
            Fact(Strings.Hardware_SecureBoot, facts.SecureBoot switch { true => Strings.Health_On, false => Strings.Health_Off, null => Strings.NotAvailable }),
            Fact(Strings.Hardware_Tpm, facts.TpmVersion ?? Strings.Hardware_NotPresent),
        ]));

        foreach (var disk in facts.Disks)
        {
            Cards.Add(new HardwareCard(string.Format(culture, Strings.Hardware_Disk, disk.Number),
            [
                Fact(Strings.Hardware_Name, disk.Model),
                Fact(Strings.Hardware_Bus, disk.BusName),
                Fact(Strings.Hardware_MediaType, disk.IsSolidState switch { true => Strings.Hardware_SolidState, false => Strings.Hardware_Rotational, null => Strings.NotAvailable }),
                Fact(Strings.Hardware_Capacity, disk.Capacity > 0 ? ByteSize.Format(disk.Capacity) : Strings.NotAvailable),
                Fact(Strings.Hardware_Trim, disk.TrimEnabled switch { true => Strings.Hardware_Enabled, false => Strings.Hardware_Disabled, null => Strings.NotAvailable }),
                Fact(Strings.Hardware_Volumes, disk.Volumes.Count == 0 ? Strings.NotAvailable : string.Join(", ", disk.Volumes)),
                Sensitive(Strings.Hardware_SerialNumber, disk.SerialNumber),
            ]));
        }

        foreach (var adapter in facts.Adapters)
        {
            var speed = adapter.SpeedBitsPerSecond >= 1_000_000_000
                ? string.Format(culture, Strings.Hardware_SpeedGbit, (adapter.SpeedBitsPerSecond / 1e9).ToString("0.#", culture))
                : string.Format(culture, Strings.Hardware_SpeedValue, (adapter.SpeedBitsPerSecond / 1e6).ToString("0", culture));

            Cards.Add(new HardwareCard(Strings.Hardware_Network,
            [
                Fact(Strings.Hardware_Name, adapter.Name),
                Fact(Strings.Hardware_Adapter, adapter.Description),
                Fact(Strings.Hardware_Type, adapter.Type),
                Fact(Strings.Hardware_Status, adapter.IsUp ? Strings.Hardware_Up : Strings.Hardware_Down),
                Fact(Strings.Hardware_Speed, adapter.IsUp && adapter.SpeedBitsPerSecond > 0 ? speed : Strings.NotAvailable),
                Sensitive(Strings.Hardware_PhysicalAddress, adapter.PhysicalAddress),
            ]));
        }

        foreach (var display in facts.Displays)
        {
            Cards.Add(new HardwareCard(Strings.Hardware_Displays,
            [
                Fact(Strings.Hardware_Name, display.IsPrimary ? $"{display.Name} ({Strings.Hardware_Primary})" : display.Name),
                Fact(Strings.Hardware_Resolution, string.Format(culture, Strings.Hardware_ResolutionValue, display.Width, display.Height, display.RefreshHz)),
                Fact(Strings.Hardware_Adapter, display.Adapter),
            ]));
        }

        _sensorCard = new HardwareCard(Strings.Hardware_Sensors, []);
        Cards.Add(_sensorCard);
        UpdateSensors(_scheduler.Latest?.Sensors);
    }

    private void OnSample(object? sender, SystemSample sample)
    {
        if (_loaded && _sensorCard is not null)
        {
            UpdateSensors(sample.Sensors);
        }
    }

    private void UpdateSensors(IReadOnlyList<SensorReading>? readings)
    {
        if (_sensorCard is null)
        {
            return;
        }

        var unit = _settings.Current.TemperatureUnit;
        string? message = !SensorProvider.IsSupported ? Strings.Hardware_SensorsUnsupported
            : !_sensors.Enabled ? Strings.Hardware_SensorsOff
            : readings is null || readings.Count == 0 ? _sensors.Reason ?? Strings.Hardware_SensorsNone
            : null;

        _sensorCard.Facts.Clear();

        if (message is not null)
        {
            _sensorCard.Facts.Add(new FactItem(Strings.Hardware_Status) { Value = message });
            _cpuTemperature.Value = _sensors.Enabled ? (_sensors.Reason is null ? Strings.Hardware_TemperatureNotReported : Strings.Hardware_TemperatureNeedsAdmin) : Strings.Hardware_TemperatureNeedsSensors;

            foreach (var fact in _gpuTemperatures.Values)
            {
                fact.Value = _cpuTemperature.Value;
            }

            return;
        }

        foreach (var reading in readings!)
        {
            _sensorCard.Facts.Add(new FactItem(reading.Hardware + ", " + reading.Name) { Value = Format(reading, unit) });
        }

        var cpu = readings.Where(reading => reading is { HardwareKind: SensorHardwareKind.Cpu, Kind: SensorKind.Temperature }).ToList();
        _cpuTemperature.Value = cpu.Count == 0
            ? (_sensors.Reason is null ? Strings.Hardware_TemperatureNotReported : Strings.Hardware_TemperatureNeedsAdmin)
            : Temperature(cpu.Max(reading => reading.Value), unit);

        foreach (var (name, fact) in _gpuTemperatures)
        {
            var gpu = readings.Where(reading => reading is { HardwareKind: SensorHardwareKind.Gpu, Kind: SensorKind.Temperature } && reading.Hardware.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            fact.Value = gpu.Count == 0 ? Strings.Hardware_TemperatureNotReported : Temperature(gpu.Max(reading => reading.Value), unit);
        }
    }

    private static string Format(SensorReading reading, TemperatureUnit unit)
    {
        var culture = CultureInfo.CurrentCulture;

        return reading.Kind switch
        {
            SensorKind.Temperature => Temperature(reading.Value, unit),
            SensorKind.Fan => reading.Value.ToString("0", culture) + " RPM",
            _ => reading.Value.ToString("0.0", culture) + " W",
        };
    }

    private static string Temperature(double celsius, TemperatureUnit unit)
    {
        var culture = CultureInfo.CurrentCulture;
        return unit == TemperatureUnit.Fahrenheit
            ? (celsius * 9 / 5 + 32).ToString("0", culture) + " °F"
            : celsius.ToString("0", culture) + " °C";
    }

    private static string CacheText(CacheInfo cache)
    {
        var kind = cache.Kind switch
        {
            CacheKind.Instruction => "I",
            CacheKind.Data => "D",
            _ => string.Empty,
        };
        var size = cache.TotalBytes >= 1024 * 1024 ? (cache.TotalBytes / (1024.0 * 1024)).ToString("0.#", CultureInfo.CurrentCulture) + " MB" : (cache.TotalBytes / 1024.0).ToString("0", CultureInfo.CurrentCulture) + " KB";
        return $"L{cache.Level}{kind} {size}";
    }

    private static FactItem Fact(string label, string value) => new(label) { Value = value.Length == 0 ? Strings.NotAvailable : value };

    private FactItem Sensitive(string label, string value)
    {
        var fact = new FactItem(label) { IsSensitive = true, Value = value.Length == 0 ? Strings.NotAvailable : ShowIdentifiers ? value : Mask };

        if (value.Length > 0)
        {
            _sensitive.Add((fact, value));
        }

        return fact;
    }

    private sealed record Facts(
        SmbiosInfo Smbios,
        ProcessorFacts Processor,
        IReadOnlyList<GpuAdapter> Gpus,
        IReadOnlyList<PhysicalDisk> Disks,
        IReadOnlyList<NetworkAdapterFacts> Adapters,
        IReadOnlyList<DisplayFacts> Displays,
        IReadOnlyList<DriverInfo> Drivers,
        bool? Uefi,
        bool? SecureBoot,
        string? TpmVersion);
}
