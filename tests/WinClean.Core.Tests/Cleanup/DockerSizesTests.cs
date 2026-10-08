using WinClean.Core.Cleanup;

namespace WinClean.Core.Tests.Cleanup;

public class DockerSizesTests
{
    [Theory]
    [InlineData("1.2GB (40%)", 1_200_000_000)]
    [InlineData("512kB", 512_000)]
    [InlineData("0B", 0)]
    [InlineData("3.5MiB", 3_670_016)]
    [InlineData("17.37MB", 17_370_000)]
    public void TryParse_ReadsDockerUnits(string text, long expected)
    {
        Assert.True(DockerSizes.TryParse(text, out var bytes));
        Assert.Equal(expected, bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("N/A")]
    [InlineData("parsecs")]
    [InlineData("1.2XB")]
    public void TryParse_RejectsOtherText(string text)
    {
        Assert.False(DockerSizes.TryParse(text, out _));
    }
}
