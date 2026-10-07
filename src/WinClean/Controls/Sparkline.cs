using System.Windows;
using System.Windows.Media;
using WinClean.Core.Monitoring;

namespace WinClean.Controls;

/// <summary>
/// The last minute of one metric as a thin line with a soft fill. It reads straight from a ring buffer
/// and redraws only when the Revision property changes, so a page full of sparklines costs one geometry
/// per metric per second and nothing in between.
/// </summary>
public sealed class Sparkline : FrameworkElement
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(RingBuffer<float>), typeof(Sparkline), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(
        nameof(Revision), typeof(int), typeof(Sparkline), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(Sparkline), new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AutoScaleProperty = DependencyProperty.Register(
        nameof(AutoScale), typeof(bool), typeof(Sparkline), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(Sparkline), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(Sparkline), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private float[] _values = [];

    public Sparkline()
    {
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
    }

    public RingBuffer<float>? Source
    {
        get => (RingBuffer<float>?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Bump it whenever the source changed; the control has no other way of knowing.</summary>
    public int Revision
    {
        get => (int)GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Scale to the largest value in view instead of Maximum, for rates without a natural ceiling.</summary>
    public bool AutoScale
    {
        get => (bool)GetValue(AutoScaleProperty);
        set => SetValue(AutoScaleProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var source = Source;
        var width = ActualWidth;
        var height = ActualHeight;

        if (source is null || width <= 0 || height <= 0)
        {
            return;
        }

        if (_values.Length != source.Capacity)
        {
            _values = new float[source.Capacity];
        }

        var count = source.CopyTo(_values);

        if (count < 2)
        {
            return;
        }

        var maximum = Maximum;

        if (AutoScale)
        {
            maximum = 1;

            for (var index = 0; index < count; index++)
            {
                if (!float.IsNaN(_values[index]) && _values[index] > maximum)
                {
                    maximum = _values[index];
                }
            }

            maximum *= 1.15;
        }

        var step = width / (_values.Length - 1);
        var left = width - (count - 1) * step;
        var line = new StreamGeometry();
        var area = Fill is null ? null : new StreamGeometry();

        using (var lineContext = line.Open())
        using (var areaContext = area?.Open())
        {
            var open = false;
            var figureStartX = 0.0;

            for (var index = 0; index < count; index++)
            {
                var value = _values[index];
                var x = left + index * step;

                if (float.IsNaN(value))
                {
                    if (open)
                    {
                        areaContext?.LineTo(new Point(left + (index - 1) * step, height), false, false);
                        areaContext?.LineTo(new Point(figureStartX, height), false, false);
                        open = false;
                    }

                    continue;
                }

                var y = height - Math.Clamp(value / maximum, 0, 1) * (height - 1.5) - 0.75;
                var point = new Point(x, y);

                if (!open)
                {
                    lineContext.BeginFigure(point, false, false);
                    areaContext?.BeginFigure(new Point(x, height), true, true);
                    areaContext?.LineTo(point, false, false);
                    figureStartX = x;
                    open = true;
                }
                else
                {
                    lineContext.LineTo(point, true, false);
                    areaContext?.LineTo(point, false, false);
                }
            }

            if (open)
            {
                areaContext?.LineTo(new Point(left + (count - 1) * step, height), false, false);
            }
        }

        line.Freeze();
        area?.Freeze();

        if (area is not null)
        {
            drawingContext.DrawGeometry(Fill, null, area);
        }

        var pen = new Pen(Stroke, 1.5) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        drawingContext.DrawGeometry(null, pen, line);
    }
}
