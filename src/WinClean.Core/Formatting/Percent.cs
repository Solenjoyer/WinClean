using System.Globalization;

namespace WinClean.Core.Formatting;

public static class Percent
{
    /// <summary>Shown wherever a number cannot be produced.</summary>
    public const string NotAvailable = "—";

    /// <summary>Clamps to 0–100 and renders without a space before the sign: "12.4%".</summary>
    public static string Format(double percent, int decimals = 1, CultureInfo? culture = null)
    {
        if (double.IsNaN(percent))
        {
            return NotAvailable;
        }

        var clamped = Math.Clamp(percent, 0, 100);
        return clamped.ToString("F" + Math.Clamp(decimals, 0, 3), culture ?? CultureInfo.CurrentCulture) + "%";
    }
}
