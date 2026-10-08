using System.Globalization;

namespace WinClean.Core.Formatting;

/// <summary>
/// Sizes the way Explorer and Task Manager show them: powers of 1024 with the KB/MB/GB labels,
/// three significant digits, so WinClean never disagrees with the rest of Windows.
/// </summary>
public static class ByteSize
{
    private static readonly string[] Symbols = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Symbol(ByteUnit unit) => Symbols[(int)unit];

    /// <summary>The largest unit that keeps the value below 1024.</summary>
    public static ByteUnit UnitFor(long bytes)
    {
        var magnitude = Math.Abs((double)bytes);
        var unit = 0;

        while (magnitude >= 1024 && unit < Symbols.Length - 1)
        {
            magnitude /= 1024;
            unit++;
        }

        return (ByteUnit)unit;
    }

    public static string Format(long bytes, CultureInfo? culture = null) => Format(bytes, UnitFor(bytes), culture);

    public static string Format(long bytes, ByteUnit unit, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (unit == ByteUnit.Bytes)
        {
            return bytes.ToString("N0", culture) + " B";
        }

        return Value(bytes / Math.Pow(1024, (int)unit), culture) + " " + Symbols[(int)unit];
    }

    /// <summary>Three significant digits: 1.24, 98.5, 412.</summary>
    internal static string Value(double value, CultureInfo culture)
    {
        var magnitude = Math.Abs(value);
        var decimals = Math.Round(magnitude, 2) < 10 ? 2 : Math.Round(magnitude, 1) < 100 ? 1 : 0;
        return value.ToString("N" + decimals, culture);
    }
}
