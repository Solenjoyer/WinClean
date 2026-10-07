namespace WinClean.Core.Health;

/// <summary>Where a driver officially comes from, decided from the PCI or USB vendor id first and the provider name second.</summary>
public static class DriverSources
{
    private static readonly DriverSource Nvidia = new("NVIDIA", new Uri("https://www.nvidia.com/drivers/"));

    private static readonly DriverSource Amd = new("AMD", new Uri("https://www.amd.com/en/support/download/drivers.html"));

    private static readonly DriverSource Intel = new("Intel", new Uri("https://www.intel.com/content/www/us/en/download-center/home.html"));

    private static readonly DriverSource Realtek = new("Realtek", new Uri("https://www.realtek.com/Download/List"));

    private static readonly DriverSource Broadcom = new("Broadcom", new Uri("https://www.broadcom.com/support/download-search"));

    private static readonly DriverSource Qualcomm = new("Qualcomm", new Uri("https://www.qualcomm.com/support"));

    private static readonly DriverSource MediaTek = new("MediaTek", new Uri("https://www.mediatek.com/products/connectivity-and-networking"));

    private static readonly DriverSource Microsoft = new("Windows Update", new Uri("ms-settings:windowsupdate"));

    private static readonly (string VendorId, DriverSource Source)[] ByVendorId =
    [
        ("10DE", Nvidia),
        ("1002", Amd),
        ("1022", Amd),
        ("8086", Intel),
        ("10EC", Realtek),
        ("0BDA", Realtek),
        ("14E4", Broadcom),
        ("17CB", Qualcomm),
        ("14C3", MediaTek),
    ];

    public static DriverSource? Find(IReadOnlyList<string>? hardwareIds, string? provider)
    {
        if (hardwareIds is not null)
        {
            foreach (var id in hardwareIds)
            {
                var vendor = VendorId(id);

                if (vendor is null)
                {
                    continue;
                }

                foreach (var (vendorId, source) in ByVendorId)
                {
                    if (string.Equals(vendorId, vendor, StringComparison.OrdinalIgnoreCase))
                    {
                        return source;
                    }
                }
            }
        }

        if (provider is null)
        {
            return null;
        }

        if (provider.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            return Nvidia;
        }

        if (provider.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase) || provider.Contains("AMD", StringComparison.Ordinal))
        {
            return Amd;
        }

        if (provider.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return Intel;
        }

        if (provider.Contains("Realtek", StringComparison.OrdinalIgnoreCase))
        {
            return Realtek;
        }

        if (provider.Contains("Broadcom", StringComparison.OrdinalIgnoreCase))
        {
            return Broadcom;
        }

        if (provider.Contains("Qualcomm", StringComparison.OrdinalIgnoreCase))
        {
            return Qualcomm;
        }

        if (provider.Contains("MediaTek", StringComparison.OrdinalIgnoreCase))
        {
            return MediaTek;
        }

        if (provider.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return Microsoft;
        }

        return null;
    }

    /// <summary>"PCI\VEN_10DE&amp;DEV_2684..." gives 10DE; "USB\VID_0BDA&amp;PID_8153" gives 0BDA.</summary>
    public static string? VendorId(string hardwareId)
    {
        ArgumentNullException.ThrowIfNull(hardwareId);

        foreach (var marker in new[] { "VEN_", "VID_" })
        {
            var index = hardwareId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (index >= 0 && hardwareId.Length >= index + marker.Length + 4)
            {
                return hardwareId.Substring(index + marker.Length, 4).ToUpperInvariant();
            }
        }

        return null;
    }
}
