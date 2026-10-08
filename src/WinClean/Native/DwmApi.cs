using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class DwmApi
{
    internal const uint DWMWA_CLOAKED = 14;

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmGetWindowAttribute(nint hwnd, uint dwAttribute, out uint pvAttribute, uint cbAttribute);
}
