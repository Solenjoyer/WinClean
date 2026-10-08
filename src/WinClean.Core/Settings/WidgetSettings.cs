namespace WinClean.Core.Settings;

public sealed record WidgetSettings
{
    public const int MinimumOpacity = 60;

    public const int MaximumOpacity = 100;

    public bool Enabled { get; set; }

    public WidgetPlacement Placement { get; set; } = WidgetPlacement.AlwaysOnTop;

    public WidgetLayout Layout { get; set; } = WidgetLayout.Full;

    /// <summary>Window opacity in percent, between <see cref="MinimumOpacity"/> and <see cref="MaximumOpacity"/>.</summary>
    public int Opacity { get; set; } = 90;

    public bool Locked { get; set; }

    /// <summary>Clicks reach whatever is under the widget; the widget itself can then only be reached from Settings or the notification area.</summary>
    public bool ClickThrough { get; set; }

    public bool ShowsTools { get; set; } = true;

    public bool ShowsStorage { get; set; } = true;

    /// <summary>Last position in device-independent pixels; null means the top-right corner of the primary work area.</summary>
    public double? Left { get; set; }

    public double? Top { get; set; }
}
