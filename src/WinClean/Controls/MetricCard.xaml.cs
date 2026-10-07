using System.Windows;
using System.Windows.Controls;

namespace WinClean.Controls;

/// <summary>A title, a large value, one caption line and room for a chart underneath.</summary>
public partial class MetricCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ChartProperty =
        DependencyProperty.Register(nameof(Chart), typeof(object), typeof(MetricCard), new PropertyMetadata(null));

    public MetricCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public object? Chart
    {
        get => GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }
}
