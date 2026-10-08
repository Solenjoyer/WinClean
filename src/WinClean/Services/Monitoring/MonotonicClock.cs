using System.Diagnostics;

namespace WinClean.Services.Monitoring;

/// <summary>A clock that never jumps: Stopwatch ticks expressed in 100-nanosecond units for rate calculations.</summary>
internal static class MonotonicClock
{
    private static readonly double TicksPerStopwatchTick = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    public static long Ticks => (long)(Stopwatch.GetTimestamp() * TicksPerStopwatchTick);
}
