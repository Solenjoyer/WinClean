using Microsoft.Win32;

namespace WinClean.Services.Storage;

public sealed record WslDistribution(string Name, string BasePath, int Version);

/// <summary>The distributions registered for this account, from the registry WSL itself uses.</summary>
internal static class WslDistributions
{
    private const string LxssKey = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    public static IReadOnlyList<WslDistribution> Read()
    {
        var result = new List<WslDistribution>();

        using var lxss = Registry.CurrentUser.OpenSubKey(LxssKey);

        if (lxss is null)
        {
            return result;
        }

        foreach (var name in lxss.GetSubKeyNames())
        {
            using var key = lxss.OpenSubKey(name);

            if (key?.GetValue("DistributionName") is not string distribution || key.GetValue("BasePath") is not string basePath)
            {
                continue;
            }

            // Store-installed distributions register their path in the extended form.
            if (basePath.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                basePath = basePath[4..];
            }

            result.Add(new WslDistribution(distribution, basePath, key.GetValue("Version") is int version ? version : 2));
        }

        return result;
    }
}
