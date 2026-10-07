using System.Globalization;

namespace WinClean.Core.Health;

/// <summary>
/// Driver versions as the vendor writes them. NVIDIA and Intel encode their marketing version in the
/// last two parts of the INF version; everyone else's four-part version is shown as is.
/// </summary>
public static class DriverVersionFormat
{
    public static string Display(string? provider, string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var parts = version.Split('.');

        if (parts.Length != 4 || provider is null)
        {
            return version;
        }

        if (provider.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            // 32.0.15.6094 -> "15" + "6094" -> 56094 -> 560.94
            var digits = parts[2] + parts[3];

            if (digits.Length >= 5 && int.TryParse(digits[^5..], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{number / 100}.{number % 100:00}");
            }
        }

        if (provider.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return parts[2] + "." + parts[3];
        }

        return version;
    }
}
