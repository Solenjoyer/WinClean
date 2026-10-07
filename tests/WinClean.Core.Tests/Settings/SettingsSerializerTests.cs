using WinClean.Core.Settings;

namespace WinClean.Core.Tests.Settings;

public class SettingsSerializerTests
{
    [Fact]
    public void Serialize_ThenDeserialize_PreservesValues()
    {
        var original = new AppSettings
        {
            Theme = ThemePreference.Dark,
            RefreshIntervalSeconds = 5,
            ShowSystemProcesses = true,
            TemperatureUnit = TemperatureUnit.Fahrenheit,
            Tray = new TraySettings { Enabled = true, Style = TrayIconStyle.Number, ShowDisk = true },
            Window = new WindowPlacement(10, 20, 1200, 800, Maximized: false),
        };

        var result = SettingsSerializer.Deserialize(SettingsSerializer.Serialize(original));

        Assert.Null(result.Error);
        Assert.Equal(original, result.Settings);
    }

    [Fact]
    public void Serialize_WritesCamelCaseNamesAndEnumNames()
    {
        var json = SettingsSerializer.Serialize(new AppSettings { Theme = ThemePreference.Light });

        Assert.Contains("\"theme\": \"Light\"", json, StringComparison.Ordinal);
        Assert.Contains("\"refreshIntervalSeconds\": 1", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_MissingKeys_UseDefaults()
    {
        var result = SettingsSerializer.Deserialize("""{ "theme": "Dark" }""");

        Assert.Null(result.Error);
        Assert.Equal(ThemePreference.Dark, result.Settings.Theme);
        Assert.True(result.Settings.GroupByApplication);
        Assert.Equal(24, result.Settings.Cleanup.TemporaryFileMinimumAgeHours);
        Assert.True(result.Settings.Tray.ShowCpu);
    }

    [Fact]
    public void Deserialize_UnknownKeysAndComments_AreIgnored()
    {
        const string json = """
            {
              // added by hand
              "theme": "Light",
              "somethingFromTheFuture": { "nested": true },
            }
            """;

        var result = SettingsSerializer.Deserialize(json);

        Assert.Null(result.Error);
        Assert.Equal(ThemePreference.Light, result.Settings.Theme);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deserialize_EmptyContent_ReturnsDefaultsWithError(string? json)
    {
        var result = SettingsSerializer.Deserialize(json);

        Assert.NotNull(result.Error);
        Assert.Equal(new AppSettings(), result.Settings);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("""{ "refreshIntervalSeconds": "fast" }""")]
    public void Deserialize_DamagedContent_ReturnsDefaultsWithError(string json)
    {
        var result = SettingsSerializer.Deserialize(json);

        Assert.NotNull(result.Error);
        Assert.Equal(new AppSettings(), result.Settings);
    }

    [Fact]
    public void Deserialize_NullContent_ReturnsDefaults()
    {
        var result = SettingsSerializer.Deserialize("null");

        Assert.NotNull(result.Error);
        Assert.Equal(new AppSettings(), result.Settings);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(60, 10)]
    public void Deserialize_RefreshIntervalOutOfRange_IsClamped(int stored, int expected)
    {
        var result = SettingsSerializer.Deserialize($$"""{ "refreshIntervalSeconds": {{stored}} }""");

        Assert.Equal(expected, result.Settings.RefreshIntervalSeconds);
    }

    [Fact]
    public void Deserialize_NullSections_AreReplacedByDefaults()
    {
        var result = SettingsSerializer.Deserialize("""{ "tray": null, "cleanup": null }""");

        Assert.Equal(new TraySettings(), result.Settings.Tray);
        Assert.Equal(new CleanupSettings(), result.Settings.Cleanup);
    }
}
