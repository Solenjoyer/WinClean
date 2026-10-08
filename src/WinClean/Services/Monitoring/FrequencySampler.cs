using Microsoft.Win32;

namespace WinClean.Services.Monitoring;

/// <summary>Current CPU frequency the way Task Manager computes it: processor performance percentage times the base clock.</summary>
internal sealed class FrequencySampler : IDisposable
{
    private const string ProcessorKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

    private PdhQuery? _query;

    private nint? _counter;

    private bool _initialized;

    public int BaseMHz { get; } = ReadBaseFrequency();

    public string? Reason { get; private set; }

    public double? Sample()
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (_query is null || _counter is null || BaseMHz <= 0 || !_query.Collect())
        {
            return null;
        }

        var performance = PdhQuery.ReadValue(_counter.Value);
        return performance is null ? null : BaseMHz * performance.Value / 100.0;
    }

    public void Dispose()
    {
        _query?.Dispose();
        _query = null;
    }

    private void Initialize()
    {
        _initialized = true;

        if (BaseMHz <= 0)
        {
            Reason = "The base clock is not in the registry.";
            return;
        }

        _query = PdhQuery.Open(out var reason);

        if (_query is null)
        {
            Reason = reason;
            return;
        }

        _counter = _query.AddCounter(@"\Processor Information(_Total)\% Processor Performance", out var status);
        Reason = _counter is null ? "Processor performance counter unavailable: " + PdhQuery.Describe(status) : null;
    }

    private static int ReadBaseFrequency()
    {
        using var key = Registry.LocalMachine.OpenSubKey(ProcessorKey);
        return key?.GetValue("~MHz") is int megahertz ? megahertz : 0;
    }
}
