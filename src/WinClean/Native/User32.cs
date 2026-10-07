using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class User32
{
    internal const nint HWND_BROADCAST = 0xFFFF;

    internal const uint MSGFLT_ALLOW = 1;

    internal const int GWL_STYLE = -16;

    internal const long WS_MINIMIZEBOX = 0x00020000;

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial uint RegisterWindowMessageW(string lpString);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessageW(nint hWnd, uint Msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ChangeWindowMessageFilterEx(nint hwnd, uint message, uint action, nint pChangeFilterStruct);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint GetWindowLongPtrW(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);
}
