using Microsoft.Win32;
using WinClean.Native;

namespace WinClean.Services.Health;

internal static class FirmwareReader
{
    /// <summary>True for UEFI, false for legacy BIOS, null when Windows does not say.</summary>
    public static bool? IsUefi()
    {
        if (!Kernel32.GetFirmwareType(out var type))
        {
            return null;
        }

        return type switch
        {
            Kernel32.FirmwareTypeUefi => true,
            Kernel32.FirmwareTypeBios => false,
            _ => null,
        };
    }

    public static bool? SecureBootEnabled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
        return key?.GetValue("UEFISecureBootEnabled") is int value ? value != 0 : null;
    }
}
