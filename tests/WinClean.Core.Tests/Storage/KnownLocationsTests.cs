using WinClean.Core.Storage;

namespace WinClean.Core.Tests.Storage;

public class KnownLocationsTests
{
    private static readonly Dictionary<string, string> Variables = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LOCALAPPDATA"] = @"C:\Users\dev\AppData\Local",
        ["APPDATA"] = @"C:\Users\dev\AppData\Roaming",
        ["USERPROFILE"] = @"C:\Users\dev",
        ["SYSTEMROOT"] = @"C:\Windows",
        ["SYSTEMDRIVE"] = "C:",
        ["PROGRAMDATA"] = @"C:\ProgramData",
    };

    [Fact]
    public void Expand_SubstitutesVariables()
    {
        var location = KnownLocations.Find("user-temp")!;

        var paths = KnownLocations.Expand(location, Variables, _ => []);

        Assert.Equal([@"C:\Users\dev\AppData\Local\Temp"], paths);
    }

    [Fact]
    public void Expand_WildcardSegments_UseTheDirectoryLister()
    {
        var location = KnownLocations.Find("firefox-cache")!;
        var profiles = new[] { @"C:\Users\dev\AppData\Local\Mozilla\Firefox\Profiles\abc.default-release", @"C:\Users\dev\AppData\Local\Mozilla\Firefox\Profiles\xyz.dev-edition" };

        var paths = KnownLocations.Expand(location, Variables, directory => directory.EndsWith("Profiles", StringComparison.Ordinal) ? profiles : []);

        Assert.Contains(@"C:\Users\dev\AppData\Local\Mozilla\Firefox\Profiles\abc.default-release\cache2", paths);
        Assert.Contains(@"C:\Users\dev\AppData\Local\Mozilla\Firefox\Profiles\xyz.dev-edition\startupCache", paths);
        Assert.Equal(6, paths.Count);
    }

    [Fact]
    public void Expand_WildcardWithPrefix_FiltersDirectoryNames()
    {
        var location = KnownLocations.Find("wsl-disks")!;
        var packages = new[]
        {
            @"C:\Users\dev\AppData\Local\Packages\CanonicalGroupLimited.Ubuntu24.04LTS_79rhkp1fndgsc",
            @"C:\Users\dev\AppData\Local\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe",
        };

        var paths = KnownLocations.Expand(location, Variables, directory => directory.EndsWith("Packages", StringComparison.Ordinal) ? packages : []);

        Assert.Equal([@"C:\Users\dev\AppData\Local\Packages\CanonicalGroupLimited.Ubuntu24.04LTS_79rhkp1fndgsc\LocalState\ext4.vhdx"], paths);
    }

    [Fact]
    public void Expand_MissingVariable_SkipsTheTemplate()
    {
        var location = KnownLocations.Find("system-error-reports")!;
        var withoutProgramData = Variables.Where(pair => pair.Key != "PROGRAMDATA").ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        Assert.Empty(KnownLocations.Expand(location, withoutProgramData, _ => []));
    }

    [Fact]
    public void Expand_NestedWildcards_ExpandEachLevel()
    {
        var location = new KnownLocation("test", LocationGroup.Caches, "Test", "Test", [@"%LOCALAPPDATA%\Vendor\*\*\cache"], LocationKind.ReportOnly, CleanupRisk.Safe);

        var paths = KnownLocations.Expand(location, Variables, directory => directory switch
        {
            @"C:\Users\dev\AppData\Local\Vendor" => [@"C:\Users\dev\AppData\Local\Vendor\1.0", @"C:\Users\dev\AppData\Local\Vendor\2.0"],
            @"C:\Users\dev\AppData\Local\Vendor\1.0" => [@"C:\Users\dev\AppData\Local\Vendor\1.0\a"],
            @"C:\Users\dev\AppData\Local\Vendor\2.0" => [@"C:\Users\dev\AppData\Local\Vendor\2.0\b"],
            _ => [],
        });

        Assert.Equal([@"C:\Users\dev\AppData\Local\Vendor\1.0\a\cache", @"C:\Users\dev\AppData\Local\Vendor\2.0\b\cache"], paths);
    }

    [Fact]
    public void Catalog_IsConsistent()
    {
        var ids = KnownLocations.All.Select(location => location.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());

        foreach (var location in KnownLocations.All)
        {
            Assert.NotEmpty(location.PathTemplates);
            Assert.All(location.PathTemplates, template => Assert.StartsWith("%", template, StringComparison.Ordinal));

            if (location.DefaultSelected)
            {
                Assert.Equal(LocationKind.Cleanable, location.Kind);
                Assert.Equal(CleanupRisk.Safe, location.Risk);
            }

            if (location.Kind == LocationKind.ReportOnly)
            {
                Assert.False(location.DefaultSelected);
            }
        }
    }

    [Fact]
    public void Catalog_OnlyPreselectsSafeLocations()
    {
        var preselected = KnownLocations.All.Where(location => location.DefaultSelected).Select(location => location.Id).ToList();

        Assert.Contains("user-temp", preselected);
        Assert.Contains("chrome-cache", preselected);
        Assert.DoesNotContain("crash-dumps", preselected);
        Assert.DoesNotContain("old-installers", preselected);
        Assert.DoesNotContain("downloads", preselected);
    }
}
