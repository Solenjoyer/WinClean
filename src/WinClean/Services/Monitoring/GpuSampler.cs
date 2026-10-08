using WinClean.Core.Monitoring;
using WinClean.Core.Monitoring.Parsers;
using WinClean.Native;

namespace WinClean.Services.Monitoring;

/// <summary>
/// GPU usage and memory from the "GPU Engine" and "GPU Adapter Memory" counters, the same source Task
/// Manager reads. Per-adapter usage is the busiest engine type, summed over processes; the raw engine
/// readings are kept for the per-process view.
/// </summary>
internal sealed class GpuSampler : IDisposable
{
    private PdhQuery? _query;

    private nint? _engineCounter;

    private nint? _dedicatedCounter;

    private nint? _sharedCounter;

    private nint? _processMemoryCounter;

    private bool _initialized;

    public string? Reason { get; private set; }

    public bool Available => _engineCounter is not null;

    /// <summary>Engine utilisation per counter instance from the last collection.</summary>
    public IReadOnlyList<(GpuCounterInstance Instance, double Value)> EngineReadings { get; private set; } = [];

    public IReadOnlyList<(GpuCounterInstance Instance, double Value)> ProcessMemoryReadings { get; private set; } = [];

    public IReadOnlyList<GpuSample> Sample()
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (_query is null || _engineCounter is null || !_query.Collect())
        {
            return [];
        }

        var engines = Parse(_query.ReadArray(_engineCounter.Value));
        EngineReadings = engines;
        ProcessMemoryReadings = _processMemoryCounter is null ? [] : Parse(_query.ReadArray(_processMemoryCounter.Value));

        var perAdapter = new Dictionary<long, Dictionary<string, double>>();

        foreach (var (instance, value) in engines)
        {
            if (!perAdapter.TryGetValue(instance.Luid, out var perType))
            {
                perType = new Dictionary<string, double>(StringComparer.Ordinal);
                perAdapter[instance.Luid] = perType;
            }

            var type = instance.EngineType ?? string.Empty;
            perType[type] = perType.GetValueOrDefault(type) + value;
        }

        var dedicated = SumByAdapter(_dedicatedCounter);
        var shared = SumByAdapter(_sharedCounter);
        var samples = new List<GpuSample>();

        foreach (var luid in perAdapter.Keys.Union(dedicated.Keys).OrderBy(luid => luid))
        {
            var utilization = perAdapter.TryGetValue(luid, out var perType) && perType.Count > 0 ? Math.Clamp(perType.Values.Max(), 0, 100) : 0;
            samples.Add(new GpuSample(luid, null, utilization, (long)dedicated.GetValueOrDefault(luid), null, (long)shared.GetValueOrDefault(luid)));
        }

        return samples;
    }

    public void Dispose()
    {
        _query?.Dispose();
        _query = null;
    }

    private void Initialize()
    {
        _initialized = true;
        _query = PdhQuery.Open(out var reason);

        if (_query is null)
        {
            Reason = reason;
            return;
        }

        _engineCounter = _query.AddCounter(@"\GPU Engine(*)\Utilization Percentage", out var status);

        if (_engineCounter is null)
        {
            Reason = "GPU counters are not available: " + PdhQuery.Describe(status);
            return;
        }

        _dedicatedCounter = _query.AddCounter(@"\GPU Adapter Memory(*)\Dedicated Usage", out _);
        _sharedCounter = _query.AddCounter(@"\GPU Adapter Memory(*)\Shared Usage", out _);
        _processMemoryCounter = _query.AddCounter(@"\GPU Process Memory(*)\Dedicated Usage", out _);
        Reason = null;
    }

    private Dictionary<long, double> SumByAdapter(nint? counter)
    {
        var totals = new Dictionary<long, double>();

        if (counter is null || _query is null)
        {
            return totals;
        }

        foreach (var (instance, value) in Parse(_query.ReadArray(counter.Value)))
        {
            totals[instance.Luid] = totals.GetValueOrDefault(instance.Luid) + value;
        }

        return totals;
    }

    private static List<(GpuCounterInstance, double)> Parse(List<CounterArrayItem> items)
    {
        var readings = new List<(GpuCounterInstance, double)>(items.Count);

        foreach (var item in items)
        {
            if (item.Status is Pdh.PDH_CSTATUS_VALID_DATA or Pdh.PDH_CSTATUS_NEW_DATA && GpuCounterInstance.TryParse(item.Instance, out var instance))
            {
                readings.Add((instance, item.Value));
            }
        }

        return readings;
    }
}
