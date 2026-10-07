using System.Globalization;
using System.Windows.Data;
using WinClean.ViewModels;

namespace WinClean.Converters;

/// <summary>
/// The chevron next to the column a table is sorted by. Takes the header's column name, the current
/// sort column and the direction.
/// </summary>
public sealed class SortGlyphConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length < 3 || values[0] is not string column || values[1] is not Enum sortColumn || values[2] is not bool descending)
        {
            return string.Empty;
        }

        return string.Equals(column, sortColumn.ToString(), StringComparison.Ordinal)
            ? descending ? Glyphs.ChevronDown : Glyphs.ChevronUp
            : string.Empty;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
