using WinClean.Core.Formatting;

namespace WinClean.Core.Tests.Formatting;

public class DurationsTests
{
    [Theory]
    [InlineData(0, 0, 0, 45, "45 seconds")]
    [InlineData(0, 0, 1, 0, "1 minute")]
    [InlineData(0, 0, 12, 30, "12 minutes")]
    [InlineData(0, 4, 12, 0, "4 hours, 12 minutes")]
    [InlineData(0, 1, 0, 0, "1 hour")]
    [InlineData(3, 4, 0, 0, "3 days, 4 hours")]
    [InlineData(23, 0, 59, 0, "23 days")]
    public void Format_ShowsTheTwoLargestUnits(int days, int hours, int minutes, int seconds, string expected)
    {
        Assert.Equal(expected, Durations.Format(new TimeSpan(days, hours, minutes, seconds)));
    }

    [Fact]
    public void Format_NegativeDuration_IsTreatedAsZero()
    {
        Assert.Equal("0 seconds", Durations.Format(TimeSpan.FromMinutes(-5)));
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(30, "just now")]
    [InlineData(300, "5 minutes ago")]
    [InlineData(3 * 3600, "3 hours ago")]
    [InlineData(36 * 3600, "yesterday")]
    [InlineData(6 * 86400, "6 days ago")]
    [InlineData(21 * 86400, "3 weeks ago")]
    [InlineData(70 * 86400, "2 months ago")]
    [InlineData(430 * 86400, "14 months ago")]
    [InlineData(800 * 86400, "2 years ago")]
    public void FormatRelative_IsCoarse(int secondsAgo, string expected)
    {
        var now = new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(expected, Durations.FormatRelative(now.AddSeconds(-secondsAgo), now));
    }
}
