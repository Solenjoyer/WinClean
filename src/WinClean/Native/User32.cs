using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class User32
{
    internal const nint HWND_BROADCAST = 0xFFFF;

    internal const uint MSGFLT_ALLOW = 1;

    internal const int GWL_STYLE = -16;

    internal const int GWL_EXSTYLE = -20;

    internal const long WS_MINIMIZEBOX = 0x00020000;

    internal const long WS_EX_TOOLWINDOW = 0x00000080;

    internal const uint GW_OWNER = 4;

    internal const int SM_CXSMICON = 49;

    internal const int WM_SETTINGCHANGE = 0x001A;

    internal const int WM_LBUTTONUP = 0x0202;

    internal const int WM_CONTEXTMENU = 0x007B;

    internal const int WM_APP = 0x8000;

    internal const long WS_EX_TOOLWINDOW_STYLE = 0x00000080;

    internal const long WS_EX_TRANSPARENT = 0x00000020;

    internal const long WS_EX_APPWINDOW = 0x00040000;

    internal const long WS_EX_NOACTIVATE = 0x08000000;

    internal const nint HWND_TOP = 0;

    internal const nint HWND_BOTTOM = 1;

    internal const uint SWP_NOSIZE = 0x0001;

    internal const uint SWP_NOMOVE = 0x0002;

    internal const uint SWP_NOZORDER = 0x0004;

    internal const uint SWP_NOACTIVATE = 0x0010;

    internal const uint SWP_NOSENDCHANGING = 0x0400;

    internal const int WM_WINDOWPOSCHANGING = 0x0046;

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

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint GetWindowLongPtrW(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static unsafe partial bool EnumWindows(delegate* unmanaged<nint, nint, int> lpEnumFunc, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsHungAppWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    internal static partial nint GetWindow(nint hWnd, uint uCmd);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial int GetWindowTextW(nint hWnd, Span<char> lpString, int nMaxCount);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint PrivateExtractIconsW(string szFileName, int nIconIndex, int cxIcon, int cyIcon, Span<nint> phicon, Span<uint> piconid, uint nIcons, uint flags);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint hIcon);

    [LibraryImport("user32.dll")]
    internal static partial uint GetDpiForSystem();

    [LibraryImport("user32.dll")]
    internal static partial int GetSystemMetricsForDpi(int nIndex, uint dpi);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayDevicesW(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplaySettingsW(string? lpszDeviceName, int iModeNum, ref DEVMODEW lpDevMode);

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DISPLAY_DEVICEW
    {
        public uint cb;
        public fixed char DeviceName[32];
        public fixed char DeviceString[128];
        public uint StateFlags;
        public fixed char DeviceID[128];
        public fixed char DeviceKey[128];
    }

    /// <summary>The WM_WINDOWPOSCHANGING payload; a hook may edit it before the window moves.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPOS
    {
        public nint hwnd;
        public nint hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    /// <summary>DEVMODEW as a raw 220-byte block; the few fields used are read by offset.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DEVMODEW
    {
        public const int Size = 220;

        public fixed byte Data[Size];
    }

    internal const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1;
    internal const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x4;
    internal const uint DISPLAY_DEVICE_MIRRORING_DRIVER = 0x8;
    internal const int ENUM_CURRENT_SETTINGS = -1;

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint CreateIconFromResourceEx(ReadOnlySpan<byte> presbits, uint dwResSize, [MarshalAs(UnmanagedType.Bool)] bool fIcon, uint dwVer, int cxDesired, int cyDesired, uint Flags);
}
