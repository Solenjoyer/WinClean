using System.Globalization;
using WinClean.Core.Formatting;

namespace WinClean.Core.Tests.Formatting;

public class PercentTests
{
    [Theory]
    [InlineData(12.44, 1, "12.4%")]
    [InlineData(12.44, 0, "12%")]
    [InlineData(0.26, 1, "0.3%")]
    [InlineData(100, 1, "100.0%")]
    public void Format_RendersWithoutSpace(double value, int decimals, string expected)
    {
        Assert.Equal(expected, Percent.Format(value, decimals, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(-5, "0.0%")]
    [InlineData(140, "100.0%")]
    public void Format_ClampsToRange(double value, string expected)
    {
        Assert.Equal(expected, Percent.Format(value, 1, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Format_NaN_RendersAsDash()
    {
        Assert.Equal("—", Percent.Format(double.NaN, 1, CultureInfo.InvariantCulture));
    }
}
