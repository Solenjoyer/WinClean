using System.Globalization;

namespace WinClean.Core.Health;

/// <summary>The installed Windows as read from the kernel and the registry.</summary>
public sealed record WindowsVersion(
    int Major,
    int Minor,
    int Build,
    int UpdateBuildRevision,
    string? DisplayVersion,
    string? EditionId,
    string? ProductName,
    string? InstallationType,
    DateTimeOffset? InstalledAt)
{
    /// <summary>The registry still says "Windows 10" on Windows 11; the build number tells the truth.</summary>
    public string ProductFamily => Build >= 22000 ? "Windows 11" : "Windows 10";

    /// <summary>"Windows 11 Pro" when the edition is known, otherwise the family alone.</summary>
    public string DisplayName
    {
        get
        {
            var edition = ProductName;

            if (edition is not null && edition.StartsWith("Windows 10 ", StringComparison.Ordinal) && Build >= 22000)
            {
                edition = "Windows 11 " + edition["Windows 10 ".Length..];
            }

            return edition ?? ProductFamily;
        }
    }

    /// <summary>"26100.4061"</summary>
    public string BuildString => UpdateBuildRevision > 0
        ? string.Create(CultureInfo.InvariantCulture, $"{Build}.{UpdateBuildRevision}")
        : Build.ToString(CultureInfo.InvariantCulture);
}
