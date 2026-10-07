using System.Globalization;

namespace WinClean.Core.Formatting;

public static class Rate
{
    /// <summary>Bytes per second in the same units as sizes: "2.1 MB/s".</summary>
    public static string Format(double bytesPerSecond, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (double.IsNaN(bytesPerSecond) || double.IsInfinity(bytesPerSecond))
        {
            return Percent.NotAvailable;
        }

        var rounded = (long)Math.Round(Math.Max(bytesPerSecond, 0));
        var unit = ByteSize.UnitFor(rounded);

        if (unit == ByteUnit.Bytes)
        {
            return rounded.ToString("N0", culture) + " B/s";
        }

        return ByteSize.Value(rounded / Math.Pow(1024, (int)unit), culture) + " " + ByteSize.Symbol(unit) + "/s";
    }
}
