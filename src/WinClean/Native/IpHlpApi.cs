using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class IpHlpApi
{
    internal const uint NO_ERROR = 0;

    [LibraryImport("iphlpapi.dll")]
    internal static partial uint GetIfTable2(out nint Table);

    [LibraryImport("iphlpapi.dll")]
    internal static partial void FreeMibTable(nint Memory);
}
