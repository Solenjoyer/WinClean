using WinClean.Core.Settings;

namespace WinClean.Core.Tests.Settings;

public class SettingsLocationTests
{
    private static readonly string ApplicationDirectory = Path.Combine("C:", "Tools", "WinClean");

    private static readonly string LocalAppData = Path.Combine("C:", "Users", "dev", "AppData", "Local");

    [Fact]
    public void Resolve_WithPortableMarker_KeepsDataNextToExecutable()
    {
        var location = SettingsLocation.Resolve(ApplicationDirectory, LocalAppData, portableMarkerExists: true);

        Assert.True(location.IsPortable);
        Assert.Equal(Path.Combine(ApplicationDirectory, "data"), location.DataDirectory);
        Assert.Equal(Path.Combine(ApplicationDirectory, "data", "settings.json"), location.SettingsFile);
        Assert.Equal(Path.Combine(ApplicationDirectory, "data", "logs"), location.LogDirectory);
    }

    [Fact]
    public void Resolve_WithoutMarker_UsesLocalAppData()
    {
        var location = SettingsLocation.Resolve(ApplicationDirectory, LocalAppData, portableMarkerExists: false);

        Assert.False(location.IsPortable);
        Assert.Equal(Path.Combine(LocalAppData, "WinClean"), location.DataDirectory);
    }

    [Theory]
    [InlineData("", "C:\\Users\\dev")]
    [InlineData("C:\\Tools", "")]
    public void Resolve_WithEmptyDirectories_Throws(string applicationDirectory, string localAppData)
    {
        Assert.Throws<ArgumentException>(() => SettingsLocation.Resolve(applicationDirectory, localAppData, portableMarkerExists: false));
    }
}
