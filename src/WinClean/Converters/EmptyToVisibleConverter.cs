using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WinClean.Converters;

/// <summary>Visible when the text is empty; pass "Invert" to show something only when there is text.</summary>
public sealed class EmptyToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var empty = value is null || (value is string text && text.Length == 0);

        if (parameter is string option && string.Equals(option, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            empty = !empty;
        }

        return empty ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
