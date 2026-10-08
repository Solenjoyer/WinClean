using WinClean.Core.Monitoring;
using WinClean.Core.Monitoring.Parsers;

namespace WinClean.Core.Tests.Monitoring;

public class RateAndCpuMathTests
{
    [Fact]
    public void RateCalculator_FirstSampleYieldsNothing_ThenPerSecondRates()
    {
        var calculator = new RateCalculator();

        Assert.Null(calculator.Update(1000, 0));
        Assert.Equal(500.0, calculator.Update(2000, 2 * TimeSpan.TicksPerSecond));
        Assert.Equal(2000.0, calculator.Update(3000, 2 * TimeSpan.TicksPerSecond + TimeSpan.TicksPerSecond / 2));
    }

    [Fact]
    public void RateCalculator_CounterGoingBackwards_StartsOver()
    {
        var calculator = new RateCalculator();
        calculator.Update(5000, 0);

        Assert.Null(calculator.Update(100, TimeSpan.TicksPerSecond));
        Assert.Equal(100.0, calculator.Update(200, 2 * TimeSpan.TicksPerSecond));
    }

    [Fact]
    public void RateCalculator_Reset_DropsTheBaseline()
    {
        var calculator = new RateCalculator();
        calculator.Update(1000, 0);
        calculator.Reset();

        Assert.Null(calculator.Update(1_000_000, TimeSpan.TicksPerSecond));
    }

    [Fact]
    public void Usage_IsOneMinusIdleShare()
    {
        var previous = new ProcessorTimes(Idle: 1000, Kernel: 1500, User: 500);
        var current = new ProcessorTimes(Idle: 1250, Kernel: 2000, User: 1000);

        Assert.Equal(75.0, CpuMath.Usage(previous, current));
        Assert.Equal(0.0, CpuMath.Usage(current, current));
    }

    [Theory]
    [InlineData(10_000_000, 10_000_000, 8, 12.5)]
    [InlineData(80_000_000, 10_000_000, 8, 100)]
    [InlineData(0, 10_000_000, 8, 0)]
    [InlineData(5, 0, 8, 0)]
    public void ProcessUsage_NormalisesByProcessorCount(long cpuDelta, long elapsed, int processors, double expected)
    {
        Assert.Equal(expected, CpuMath.ProcessUsage(cpuDelta, elapsed, processors));
    }
}
