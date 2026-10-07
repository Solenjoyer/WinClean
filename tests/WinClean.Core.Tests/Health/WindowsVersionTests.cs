using WinClean.Core.Health;

namespace WinClean.Core.Tests.Health;

public class WindowsVersionTests
{
    [Fact]
    public void DisplayName_RewritesTheRegistryNameOnWindows11()
    {
        var version = new WindowsVersion(10, 0, 26100, 4061, "24H2", "Professional", "Windows 10 Pro", "Client", null);

        Assert.Equal("Windows 11", version.ProductFamily);
        Assert.Equal("Windows 11 Pro", version.DisplayName);
        Assert.Equal("26100.4061", version.BuildString);
    }

    [Fact]
    public void DisplayName_KeepsWindows10Names()
    {
        var version = new WindowsVersion(10, 0, 19045, 5737, "22H2", "Core", "Windows 10 Home", "Client", null);

        Assert.Equal("Windows 10 Home", version.DisplayName);
    }

    [Fact]
    public void DisplayName_WithoutProductName_FallsBackToFamily()
    {
        var version = new WindowsVersion(10, 0, 22631, 0, null, null, null, null, null);

        Assert.Equal("Windows 11", version.DisplayName);
        Assert.Equal("22631", version.BuildString);
    }
}
