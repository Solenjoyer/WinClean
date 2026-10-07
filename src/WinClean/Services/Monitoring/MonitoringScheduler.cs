using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using WinClean.Core.Monitoring;
using WinClean.Native;
using WinClean.Services.Processes;

namespace WinClean.Services.Monitoring;

/// <summary>
/// Owns the sampler thread. Cheap readers run every tick, slower ones every few ticks, and nothing runs
/// while no page or tray icon needs data. Each tick produces one immutable SystemSample that reaches the
/// UI thread coalesced: a sample the UI has not consumed yet is replaced, never queued.
/// </summary>
public sealed class MonitoringScheduler : IDisposable
{
    private static readonly TimeSpan ResumeThreshold = TimeSpan.FromSeconds(5);

    private readonly ILogger<MonitoringScheduler> _logger;

    private readonly SynchronizationContext? _ui;

    private readonly AutoResetEvent _wake = new(false);

    private readonly CpuSampler _cpu = new();

    private readonly MemorySampler _memory = new();

    private readonly VolumeSampler _volumes = new();

    private readonly DiskActivitySampler _disk = new();

    private readonly NetworkSampler _network = new();

    private readonly BatterySampler _battery = new();

    private readonly GpuSampler _gpu = new();

    private readonly FrequencySampler _frequency = new();

    private readonly ProcessSnapshotReader _processes;

    private readonly WindowInventory _windows = new();

    private Thread? _thread;

    private volatile bool _stopping;

    private volatile MonitoringDemand _demand = MonitoringDemand.None;

    private SystemSample? _pending;

    private int _postScheduled;

    private long _sequence;

    private long _lastTickTicks;

    private IReadOnlyList<VolumeSample> _cachedVolumes = [];

    private long _volumesSampledAt = long.MinValue;

    private BatterySample? _cachedBattery;

    private long _batterySampledAt = long.MinValue;

    private double? _cachedFrequency;

    private IReadOnlyList<GpuSample> _cachedGpus = [];

    private long _detailedSampledAt = long.MinValue;

    public MonitoringScheduler(ILogger<MonitoringScheduler> logger, ProcessDetailsCache details)
    {
        _logger = logger;
        _processes = new ProcessSnapshotReader(details);
        _ui = SynchronizationContext.Current ?? new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
    }

    /// <summary>Raised on the UI thread (when created there) with the newest sample.</summary>
    public event EventHandler<SystemSample>? SampleReady;

    public MetricHistory History { get; } = new();

    public SystemSample? Latest { get; private set; }

    public int ProcessorCount => _cpu.ProcessorCount;

    public int BaseFrequencyMHz => _frequency.BaseMHz;

    public MonitoringDemand Demand
    {
        get => _demand;
        set
        {
            if (value != _demand)
            {
                _demand = value;
                _wake.Set();
            }
        }
    }

    public IReadOnlyList<SamplerStatus> Statuses =>
    [
        new("CPU usage", _cpu.Reason is null, _cpu.Reason),
        new("CPU per core", _cpu.CoresReason is null, _cpu.CoresReason),
        new("CPU frequency", _frequency.Reason is null, _frequency.Reason),
        new("Memory", _memory.Reason is null, _memory.Reason),
        new("Volumes", _volumes.Reason is null, _volumes.Reason),
        new("Disk activity", _disk.Reason is null, _disk.Reason),
        new("Network", _network.Reason is null, _network.Reason),
        new("Battery", _battery.Reason is null, _battery.Reason),
        new("GPU", _gpu.Reason is null, _gpu.Reason),
        new("Processes", _processes.Reason is null, _processes.Reason),
    ];

    public void Start()
    {
        if (_thread is not null)
        {
            return;
        }

        _thread = new Thread(Loop)
        {
            Name = "WinClean.Sampler",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
        };
        _thread.Start();
    }

    /// <summary>Runs the next tick now instead of at the end of the interval, for instance after an action.</summary>
    public void Poke() => _wake.Set();

    /// <summary>One synchronous tick, for diagnostics and the self-check.</summary>
    public SystemSample SampleOnce(bool processes = false)
    {
        var demand = new MonitoringDemand(true, true, processes, 1);
        Tick(demand, MonotonicClock.Ticks);
        Thread.Sleep(1000);
        return Tick(demand, MonotonicClock.Ticks);
    }

    public void Dispose()
    {
        _stopping = true;
        _wake.Set();
        _thread?.Join(2000);
        _disk.Dispose();
        _gpu.Dispose();
        _frequency.Dispose();
        _wake.Dispose();
    }

    private void Loop()
    {
        while (!_stopping)
        {
            var demand = _demand;

            if (!demand.Any)
            {
                History.PushGap();
                ResetBaselines();
                _wake.WaitOne(TimeSpan.FromSeconds(5));
                continue;
            }

            var started = Stopwatch.GetTimestamp();
            var now = MonotonicClock.Ticks;
            var resumed = _lastTickTicks != 0 && now - _lastTickTicks > ResumeThreshold.Ticks + demand.IntervalSeconds * TimeSpan.TicksPerSecond;

            if (resumed)
            {
                // Counters jumped while the machine slept; take a baseline and skip the spike.
                ResetBaselines();
            }

            try
            {
                var sample = Tick(demand, now);

                if (!resumed)
                {
                    Publish(sample);
                }
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogError(exception, "A monitoring tick failed.");
            }

            _lastTickTicks = now;
            var remaining = TimeSpan.FromSeconds(demand.IntervalSeconds) - Stopwatch.GetElapsedTime(started);

            if (remaining > TimeSpan.Zero)
            {
                _wake.WaitOne(remaining);
            }
        }
    }

    private SystemSample Tick(MonitoringDemand demand, long now)
    {
        var (cpu, cores) = _cpu.Sample();
        var memory = _memory.Sample();
        var network = _network.Sample(now);
        var disk = _disk.Sample(now);

        if (now - _volumesSampledAt > 5 * TimeSpan.TicksPerSecond)
        {
            _cachedVolumes = _volumes.Sample();
            _volumesSampledAt = now;
        }

        if (now - _batterySampledAt > 5 * TimeSpan.TicksPerSecond)
        {
            _cachedBattery = _battery.Sample();
            _batterySampledAt = now;
        }

        if (demand.Detailed && now - _detailedSampledAt >= 2 * TimeSpan.TicksPerSecond - TimeSpan.TicksPerMillisecond * 50)
        {
            _cachedGpus = _gpu.Sample();
            _cachedFrequency = _frequency.Sample();
            _detailedSampledAt = now;
        }

        var processes = demand.Processes
            ? _processes.Read(now, _cpu.ProcessorCount, _gpu.EngineReadings, _gpu.ProcessMemoryReadings, _windows.Scan())
            : null;

        var sample = new SystemSample(
            DateTimeOffset.Now,
            ++_sequence,
            cpu,
            cores,
            demand.Detailed ? _cachedFrequency : null,
            _cpu.ProcessorCount,
            memory,
            _cachedVolumes,
            disk,
            network,
            _cachedBattery,
            demand.Detailed ? _cachedGpus : [],
            TimeSpan.FromMilliseconds(Kernel32.GetTickCount64()),
            processes);

        Latest = sample;
        return sample;
    }

    private void ResetBaselines()
    {
        _cpu.Reset();
        _disk.Reset();
        _network.Reset();
        _processes.Reset();
        _lastTickTicks = 0;
    }

    private void Publish(SystemSample sample)
    {
        History.Push(sample);

        if (_ui is null)
        {
            SampleReady?.Invoke(this, sample);
            return;
        }

        Interlocked.Exchange(ref _pending, sample);

        if (Interlocked.Exchange(ref _postScheduled, 1) == 0)
        {
            _ui.Post(_ =>
            {
                Interlocked.Exchange(ref _postScheduled, 0);
                var latest = Interlocked.Exchange(ref _pending, null);

                if (latest is not null)
                {
                    SampleReady?.Invoke(this, latest);
                }
            }, null);
        }
    }
}
