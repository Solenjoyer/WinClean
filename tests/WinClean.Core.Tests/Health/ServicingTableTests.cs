using WinClean.Core.Health;

namespace WinClean.Core.Tests.Health;

public class ServicingTableTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);

    [Theory]
    [InlineData(26200, "Core", "25H2", 2027, 10, 12)]
    [InlineData(26100, "Professional", "24H2", 2026, 10, 13)]
    [InlineData(26100, "Enterprise", "24H2", 2027, 10, 12)]
    [InlineData(26100, "EnterpriseS", "Enterprise LTSC 2024", 2029, 10, 9)]
    [InlineData(22631, "Education", "23H2", 2026, 11, 10)]
    public void Evaluate_SupportedBuilds_ReportTheRightColumn(int build, string edition, string version, int year, int month, int day)
    {
        var status = ServicingTable.Evaluate(build, edition, Today);

        Assert.Equal(ServicingState.Supported, status.State);
        Assert.Equal(version, status.Release!.Version);
        Assert.Equal(new DateOnly(year, month, day), status.EndOfServicing);
    }

    [Theory]
    [InlineData(19045, "Core")]
    [InlineData(19045, "Enterprise")]
    [InlineData(22631, "Professional")]
    [InlineData(22000, "Enterprise")]
    public void Evaluate_EndedBuilds_ReportEnded(int build, string edition)
    {
        var status = ServicingTable.Evaluate(build, edition, Today);

        Assert.Equal(ServicingState.Ended, status.State);
        Assert.NotNull(status.EndOfServicing);
    }

    [Fact]
    public void Evaluate_Windows10LongTermServicing_IsStillSupported()
    {
        var status = ServicingTable.Evaluate(19044, "EnterpriseS", Today);

        Assert.Equal(ServicingState.Supported, status.State);
        Assert.Equal("Enterprise LTSC 2021", status.Release!.Version);
        Assert.True(status.UsesEnterpriseDates);
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(10240)]
    [InlineData(0)]
    public void Evaluate_UnknownBuild_IsUnknown(int build)
    {
        var status = ServicingTable.Evaluate(build, "Professional", Today);

        Assert.Equal(ServicingState.Unknown, status.State);
        Assert.Null(status.Release);
    }

    [Fact]
    public void Evaluate_OnTheLastDay_IsStillSupported()
    {
        var status = ServicingTable.Evaluate(26100, "Professional", new DateOnly(2026, 10, 13));

        Assert.Equal(ServicingState.Supported, status.State);
    }

    [Fact]
    public void Releases_AreOrderedNewestFirstWithoutDuplicates()
    {
        var keys = ServicingTable.Releases.Select(release => (release.Build, release.LongTermServicing)).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Equal(keys.OrderByDescending(key => key.Build).ThenBy(key => key.LongTermServicing).ToList(), keys);
    }

    [Fact]
    public void Releases_EndDatesNeverPrecedeRelease()
    {
        foreach (var release in ServicingTable.Releases)
        {
            Assert.True(release.ConsumerEnd is null || release.ConsumerEnd > release.Released, release.Version);
            Assert.True(release.EnterpriseEnd is null || release.EnterpriseEnd > release.Released, release.Version);
        }
    }

    [Theory]
    [InlineData("Enterprise", true)]
    [InlineData("EnterpriseN", true)]
    [InlineData("Education", true)]
    [InlineData("IoTEnterpriseS", true)]
    [InlineData("Professional", false)]
    [InlineData("Core", false)]
    [InlineData(null, false)]
    public void IsEnterpriseEdition_FollowsTheEditionId(string? edition, bool expected)
    {
        Assert.Equal(expected, ServicingTable.IsEnterpriseEdition(edition));
    }
}
