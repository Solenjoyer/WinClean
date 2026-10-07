namespace WinClean.Core.Health;

/// <summary>Maps setup class names to the groups the Health page shows; chipset is "System class, not from Microsoft".</summary>
public static class DeviceClasses
{
    public static DeviceGroup Classify(string? className, string? provider)
    {
        if (string.IsNullOrEmpty(className))
        {
            return DeviceGroup.Other;
        }

        return className.ToUpperInvariant() switch
        {
            "DISPLAY" => DeviceGroup.Display,
            "NET" => DeviceGroup.Network,
            "MEDIA" => DeviceGroup.Audio,
            "HDC" or "SCSIADAPTER" or "DISKDRIVE" or "STORAGEVOLUME" => DeviceGroup.Storage,
            "BLUETOOTH" => DeviceGroup.Bluetooth,
            "USB" => DeviceGroup.Usb,
            "FIRMWARE" => DeviceGroup.Firmware,
            "SYSTEM" when provider is not null && !provider.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) => DeviceGroup.Chipset,
            _ => DeviceGroup.Other,
        };
    }

    /// <summary>The groups shown by default: the drivers people actually update.</summary>
    public static bool IsHighlighted(DeviceGroup group) => group is DeviceGroup.Display or DeviceGroup.Network or DeviceGroup.Audio or DeviceGroup.Storage or DeviceGroup.Chipset;
}
