namespace WinClean.Core.Settings;

public sealed record TraySettings
{
    public bool Enabled { get; set; }

    public TrayIconStyle Style { get; set; } = TrayIconStyle.Bars;

    public bool ShowCpu { get; set; } = true;

    public bool ShowMemory { get; set; } = true;

    public bool ShowDisk { get; set; }

    public bool CloseToTray { get; set; }
}
