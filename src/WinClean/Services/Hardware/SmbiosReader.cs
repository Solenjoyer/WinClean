using WinClean.Core.Hardware.Smbios;
using WinClean.Native;

namespace WinClean.Services.Hardware;

internal static class SmbiosReader
{
    public static SmbiosInfo Read()
    {
        var size = Kernel32.GetSystemFirmwareTable(Kernel32.FirmwareTableProviderSmbios, 0, [], 0);

        if (size == 0)
        {
            return SmbiosInfo.Empty;
        }

        var buffer = new byte[size];
        var written = Kernel32.GetSystemFirmwareTable(Kernel32.FirmwareTableProviderSmbios, 0, buffer, size);
        return written == 0 ? SmbiosInfo.Empty : SmbiosParser.ParseFirmwareTable(buffer.AsSpan(0, (int)Math.Min(written, size)));
    }
}
