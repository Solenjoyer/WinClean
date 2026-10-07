using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinClean.Services.Shell;

/// <summary>
/// Draws the notification area icons at the exact pixel size the taskbar asks for. Bars stay readable at
/// 16 pixels where digits do not; the number style exists for people who prefer a figure.
/// </summary>
internal static class TrayIconRenderer
{
    private static readonly FontFamily DigitFont = new("Segoe UI Variable Display, Segoe UI");

    /// <summary>Vertical bars for each value, 0 to 100, inside a thin rounded frame; returned as PNG bytes.</summary>
    public static byte[] RenderBars(IReadOnlyList<double> values, int size, bool darkTaskbar)
    {
        ArgumentNullException.ThrowIfNull(values);

        var foreground = Foreground(darkTaskbar);
        var frame = new Pen(new SolidColorBrush(foreground) { Opacity = 0.55 }, 1);
        frame.Freeze();
        var fill = new SolidColorBrush(foreground);
        fill.Freeze();

        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            var radius = Math.Max(1.5, size / 8.0);
            context.DrawRoundedRectangle(null, frame, new Rect(0.5, 0.5, size - 1, size - 1), radius, radius);

            var inset = size >= 24 ? 3 : 2;
            var innerHeight = size - 2 * inset;
            var innerWidth = size - 2 * inset;
            var gap = values.Count > 1 ? 1 : 0;
            var barWidth = Math.Max(1, (innerWidth - gap * (values.Count - 1)) / Math.Max(1, values.Count));
            var left = inset + (innerWidth - (barWidth * values.Count + gap * (values.Count - 1))) / 2.0;

            for (var index = 0; index < values.Count; index++)
            {
                var height = BarHeight(values[index], innerHeight);

                if (height > 0)
                {
                    var x = left + index * (barWidth + gap);
                    context.DrawRectangle(fill, null, new Rect(x, size - inset - height, barWidth, height));
                }
            }
        }

        return Encode(visual, size);
    }

    /// <summary>A two-digit figure that fills the icon; white digits get a dark edge so they read on any wallpaper.</summary>
    public static byte[] RenderNumber(int value, int size, bool darkTaskbar)
    {
        var text = Math.Clamp(value, 0, 99).ToString(CultureInfo.InvariantCulture);
        var foreground = new SolidColorBrush(Foreground(darkTaskbar));
        foreground.Freeze();
        var typeface = new Typeface(DigitFont, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var fontSize = size * 0.82;
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, foreground, 1.0);

        if (formatted.Width > size - 1)
        {
            formatted.SetFontSize(fontSize * (size - 1) / formatted.Width);
        }

        var origin = new Point((size - formatted.Width) / 2, (size - formatted.Height) / 2);
        var geometry = formatted.BuildGeometry(origin);
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            if (darkTaskbar)
            {
                var edge = new Pen(new SolidColorBrush(Color.FromArgb(0x90, 0, 0, 0)), 1.5) { LineJoin = PenLineJoin.Round };
                edge.Freeze();
                context.DrawGeometry(null, edge, geometry);
            }

            context.DrawGeometry(foreground, null, geometry);
        }

        return Encode(visual, size);
    }

    /// <summary>The height in whole pixels, so that unchanged readings produce byte-identical icons.</summary>
    public static int BarHeight(double percent, int innerHeight)
    {
        if (double.IsNaN(percent) || percent <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(innerHeight * Math.Min(percent, 100) / 100), 1, innerHeight);
    }

    private static Color Foreground(bool darkTaskbar) => darkTaskbar ? Colors.White : Color.FromRgb(0x1B, 0x1B, 0x1B);

    private static byte[] Encode(DrawingVisual visual, int size)
    {
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
