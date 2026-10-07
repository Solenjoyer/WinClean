using System.Globalization;
using WinClean.Core.Formatting;

namespace WinClean.Core.Tests.Formatting;

public class ByteSizeTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1,023 B")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(1536, "1.50 KB")]
    [InlineData(100_864, "98.5 KB")]
    [InlineData(431_820_800, "412 MB")]
    [InlineData(1_331_439_862, "1.24 GB")]
    [InlineData(34_359_738_368, "32.0 GB")]
    [InlineData(1_099_511_627_776, "1.00 TB")]
    public void Format_UsesBinaryUnitsAndThreeSignificantDigits(long bytes, string expected)
    {
        Assert.Equal(expected, ByteSize.Format(bytes, Invariant));
    }

    [Fact]
    public void Format_RoundingUpToTheNextDigitCount_KeepsThreeSignificantDigits()
    {
        var almostTen = (long)(9.996 * 1024 * 1024);

        Assert.Equal("10.0 MB", ByteSize.Format(almostTen, Invariant));
    }

    [Fact]
    public void Format_WithForcedUnit_KeepsTheUnit()
    {
        var bytes = 18L * 1024 * 1024 * 1024 + 200L * 1024 * 1024;

        Assert.Equal("18.2 GB", ByteSize.Format(bytes, ByteUnit.Gigabytes, Invariant));
        Assert.Equal("18,632 MB", ByteSize.Format(bytes, ByteUnit.Megabytes, Invariant));
    }

    [Fact]
    public void Format_RespectsCulture()
    {
        var german = CultureInfo.GetCultureInfo("de-DE");

        Assert.Equal("1,24 GB", ByteSize.Format(1_331_439_862, german));
    }

    [Theory]
    [InlineData(0, ByteUnit.Bytes)]
    [InlineData(1023, ByteUnit.Bytes)]
    [InlineData(1024, ByteUnit.Kilobytes)]
    [InlineData(1_048_576, ByteUnit.Megabytes)]
    [InlineData(long.MaxValue, ByteUnit.Petabytes)]
    public void UnitFor_PicksTheLargestUnitBelow1024(long bytes, ByteUnit expected)
    {
        Assert.Equal(expected, ByteSize.UnitFor(bytes));
    }

    [Theory]
    [InlineData(0, "0 B/s")]
    [InlineData(2_202_009.6, "2.10 MB/s")]
    [InlineData(double.NaN, "—")]
    public void Rate_FormatsPerSecond(double bytesPerSecond, string expected)
    {
        Assert.Equal(expected, Rate.Format(bytesPerSecond, Invariant));
    }
}
