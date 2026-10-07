using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WinClean.Controls;

/// <summary>A thin horizontal bar: used share of a volume, memory of an application, size of a folder relative to its parent.</summary>
public sealed class ProportionBar : RangeBase
{
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(ProportionBar), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CriticalFillProperty = DependencyProperty.Register(
        nameof(CriticalFill), typeof(Brush), typeof(ProportionBar), new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(
        nameof(Track), typeof(Brush), typeof(ProportionBar), new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsCriticalProperty = DependencyProperty.Register(
        nameof(IsCritical), typeof(bool), typeof(ProportionBar), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
        nameof(BarHeight), typeof(double), typeof(ProportionBar), new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    static ProportionBar()
    {
        MaximumProperty.OverrideMetadata(typeof(ProportionBar), new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));
        ValueProperty.OverrideMetadata(typeof(ProportionBar), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        FocusableProperty.OverrideMetadata(typeof(ProportionBar), new FrameworkPropertyMetadata(false));
    }

    public ProportionBar()
    {
        IsTabStop = false;
        SnapsToDevicePixels = true;
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Used instead of Fill when IsCritical is set, for instance below 10 % free space.</summary>
    public Brush CriticalFill
    {
        get => (Brush)GetValue(CriticalFillProperty);
        set => SetValue(CriticalFillProperty, value);
    }

    public Brush Track
    {
        get => (Brush)GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public bool IsCritical
    {
        get => (bool)GetValue(IsCriticalProperty);
        set => SetValue(IsCriticalProperty, value);
    }

    public double BarHeight
    {
        get => (double)GetValue(BarHeightProperty);
        set => SetValue(BarHeightProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var width = double.IsInfinity(constraint.Width) ? 0 : constraint.Width;
        return new Size(width, BarHeight);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = ActualWidth;
        var height = ActualHeight;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var radius = height / 2;
        drawingContext.DrawRoundedRectangle(Track, null, new Rect(0, 0, width, height), radius, radius);

        var range = Maximum - Minimum;
        var share = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        var filled = Math.Round(width * share);

        if (filled >= 1)
        {
            drawingContext.DrawRoundedRectangle(IsCritical ? CriticalFill : Fill, null, new Rect(0, 0, filled, height), radius, radius);
        }
    }
}
