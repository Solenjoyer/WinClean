using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using WinClean.Core.Monitoring;

namespace WinClean.Services.Monitoring;

/// <summary>
/// Temperatures, fans and power through LibreHardwareMonitor, only when the user turned sensors on.
/// CPU and motherboard sensors need the separately installed PawnIO driver and administrator rights;
/// WinClean ships neither and says so instead of guessing.
/// </summary>
public sealed class SensorProvider : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private readonly SettingsStore _settings;

    private readonly ILogger<SensorProvider> _logger;

    private readonly AutoResetEvent _wake = new(false);

    private volatile IReadOnlyList<SensorReading> _readings = [];

    private volatile bool _enabled;

    private volatile bool _stopping;

    private Thread? _thread;

    public SensorProvider(SettingsStore settings, ILogger<SensorProvider> logger)
    {
        _settings = settings;
        _logger = logger;
        _enabled = settings.Current.SensorsEnabled;
        _settings.Changed += (_, current) => Enabled = current.SensorsEnabled;
    }

    public IReadOnlyList<SensorReading> Readings => _readings;

    /// <summary>Why CPU sensors may be missing, for the diagnostics list and the tooltips.</summary>
    public string? Reason { get; private set; }

    public static bool IsSupported => RuntimeInformation.ProcessArchitecture != Architecture.Arm64;

    public bool Enabled
    {
        get => _enabled;
        private set
        {
            _enabled = value;

            if (value)
            {
                Start();
            }

            _wake.Set();
        }
    }

    public static bool PawnIoInstalled()
    {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
        return key is not null;
    }

    public void Start()
    {
        if (_thread is not null || !_enabled || !IsSupported)
        {
            return;
        }

        _thread = new Thread(Loop) { Name = "WinClean.Sensors", IsBackground = true, Priority = ThreadPriority.BelowNormal };
        _thread.Start();
    }

    public void Dispose()
    {
        _stopping = true;
        _wake.Set();
        _thread?.Join(3000);
        _wake.Dispose();
    }

    private void Loop()
    {
        Computer? computer = null;

        while (!_stopping)
        {
            if (!_enabled)
            {
                if (computer is not null)
                {
                    Close(computer);
                    computer = null;
                    _readings = [];
                }

                _wake.WaitOne(Interval);
                continue;
            }

            try
            {
                computer ??= Open();
                _readings = Collect(computer);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogWarning(exception, "Reading sensors failed.");
                Reason = exception.Message;
            }

            _wake.WaitOne(Interval);
        }

        if (computer is not null)
        {
            Close(computer);
        }
    }

    private Computer Open()
    {
        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsMemoryEnabled = false,
            IsNetworkEnabled = false,
            IsControllerEnabled = false,
        };
        computer.Open();

        if (!ProcessContext.IsElevated)
        {
            Reason = "CPU and motherboard sensors need administrator rights.";
        }
        else if (!PawnIoInstalled())
        {
            Reason = "CPU and motherboard sensors need the PawnIO driver (pawnio.eu).";
        }
        else
        {
            Reason = null;
        }

        return computer;
    }

    private void Close(Computer computer)
    {
        try
        {
            computer.Close();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _logger.LogDebug(exception, "Closing the sensor library failed.");
        }
    }

    private static List<SensorReading> Collect(Computer computer)
    {
        var readings = new List<SensorReading>();

        foreach (var hardware in computer.Hardware)
        {
            hardware.Update();
            Collect(hardware, readings);

            foreach (var sub in hardware.SubHardware)
            {
                sub.Update();
                Collect(sub, readings);
            }
        }

        return readings;
    }

    private static void Collect(IHardware hardware, List<SensorReading> readings)
    {
        var kind = hardware.HardwareType switch
        {
            HardwareType.Cpu => SensorHardwareKind.Cpu,
            HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => SensorHardwareKind.Gpu,
            HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => SensorHardwareKind.Motherboard,
            HardwareType.Storage => SensorHardwareKind.Storage,
            HardwareType.Memory => SensorHardwareKind.Memory,
            _ => SensorHardwareKind.Other,
        };

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not { } value || double.IsNaN(value))
            {
                continue;
            }

            SensorKind? sensorKind = sensor.SensorType switch
            {
                SensorType.Temperature => SensorKind.Temperature,
                SensorType.Fan => SensorKind.Fan,
                SensorType.Power => SensorKind.Power,
                _ => null,
            };

            if (sensorKind is { } resolved)
            {
                readings.Add(new SensorReading(hardware.Name, kind, sensor.Name, resolved, value));
            }
        }
    }
}
