using WinClean.Core.Monitoring.Parsers;

namespace WinClean.Core.Monitoring;

public static class CpuMath
{
    /// <summary>Busy percentage between two readings; kernel time includes idle time, as Windows reports it.</summary>
    public static double Usage(ProcessorTimes previous, ProcessorTimes current)
    {
        var total = current.Total - previous.Total;

        if (total <= 0)
        {
            return 0;
        }

        var idle = current.Idle - previous.Idle;
        return Math.Clamp(100.0 * (total - idle) / total, 0, 100);
    }

    /// <summary>Share of elapsed wall-clock time one process spent on all processors, as Task Manager shows it.</summary>
    public static double ProcessUsage(long cpuTimeDeltaTicks, long elapsedTicks, int processorCount)
    {
        if (elapsedTicks <= 0 || processorCount <= 0)
        {
            return 0;
        }

        return Math.Clamp(100.0 * cpuTimeDeltaTicks / (elapsedTicks * (double)processorCount), 0, 100);
    }
}
