using System.Globalization;

namespace WinClean.Core.Cleanup;

/// <summary>Docker prints sizes in its own decimal units ("1.2GB", "512kB"); this turns them back into bytes.</summary>
public static class DockerSizes
{
    public static bool TryParse(string? text, out long bytes)
    {
        bytes = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // "1.2GB (40%)" -> "1.2GB"
        var token = text.Trim().Split(' ', 2)[0];
        var digits = 0;

        while (digits < token.Length && (char.IsAsciiDigit(token[digits]) || token[digits] == '.'))
        {
            digits++;
        }

        if (digits == 0 || !double.TryParse(token[..digits], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var factor = token[digits..].Trim().ToUpperInvariant() switch
        {
            "" or "B" => 1d,
            "KB" => 1e3,
            "MB" => 1e6,
            "GB" => 1e9,
            "TB" => 1e12,
            "KIB" => 1024d,
            "MIB" => 1024d * 1024,
            "GIB" => 1024d * 1024 * 1024,
            _ => -1,
        };

        if (factor < 0)
        {
            return false;
        }

        bytes = (long)Math.Round(value * factor);
        return true;
    }
}
