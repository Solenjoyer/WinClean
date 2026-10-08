namespace WinClean.Core.Monitoring;

/// <summary>
/// Turns a cumulative counter (bytes transferred, octets received) into a per-second rate. The first
/// sample after a reset yields nothing, and a counter that went backwards is treated as a fresh start.
/// </summary>
public sealed class RateCalculator
{
    private ulong _previousValue;

    private long _previousTicks;

    private bool _primed;

    /// <param name="value">The cumulative counter.</param>
    /// <param name="timestampTicks">A monotonic timestamp in 100-nanosecond ticks.</param>
    public double? Update(ulong value, long timestampTicks)
    {
        if (!_primed || value < _previousValue || timestampTicks <= _previousTicks)
        {
            _previousValue = value;
            _previousTicks = timestampTicks;
            _primed = true;
            return null;
        }

        var delta = value - _previousValue;
        var seconds = (timestampTicks - _previousTicks) / (double)TimeSpan.TicksPerSecond;
        _previousValue = value;
        _previousTicks = timestampTicks;
        return delta / seconds;
    }

    public void Reset() => _primed = false;
}
