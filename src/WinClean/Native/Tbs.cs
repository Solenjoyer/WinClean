using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class Tbs
{
    internal const uint TBS_SUCCESS = 0;

    [LibraryImport("tbs.dll")]
    internal static partial uint Tbsi_GetDeviceInfo(uint Size, out TPM_DEVICE_INFO Info);

    [StructLayout(LayoutKind.Sequential)]
    internal struct TPM_DEVICE_INFO
    {
        public uint structVersion;
        public uint tpmVersion;
        public uint tpmInterfaceType;
        public uint tpmImpRevision;
    }
}
