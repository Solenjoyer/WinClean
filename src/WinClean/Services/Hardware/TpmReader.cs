using System.Runtime.InteropServices;
using WinClean.Native;

namespace WinClean.Services.Hardware;

internal static class TpmReader
{
    /// <summary>"2.0", "1.2", null when there is no TPM or the base services cannot be reached.</summary>
    public static string? Version()
    {
        try
        {
            if (Tbs.Tbsi_GetDeviceInfo((uint)Marshal.SizeOf<Tbs.TPM_DEVICE_INFO>(), out var info) != Tbs.TBS_SUCCESS)
            {
                return null;
            }

            return info.tpmVersion switch
            {
                1 => "1.2",
                2 => "2.0",
                _ => null,
            };
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }
}
