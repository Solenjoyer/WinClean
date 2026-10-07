using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class Shell32
{
    internal const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;

    internal const int SW_SHOWNORMAL = 1;

    internal const uint SHGFI_ICON = 0x000000100;

    internal const uint SHGFI_SMALLICON = 0x000000001;

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint ILCreateFromPathW(string pszPath);

    [LibraryImport("shell32.dll")]
    internal static partial void ILFree(nint pidl);

    [LibraryImport("shell32.dll")]
    internal static partial int SHOpenFolderAndSelectItems(nint pidlFolder, uint cidl, nint apidl, uint dwFlags);

    [LibraryImport("shell32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShellExecuteExW(ref SHELLEXECUTEINFOW pExecInfo);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint SHGetFileInfoW(string pszPath, uint dwFileAttributes, ref SHFILEINFOW psfi, uint cbFileInfo, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SHELLEXECUTEINFOW
    {
        public uint cbSize;
        public uint fMask;
        public nint hwnd;
        public nint lpVerb;
        public nint lpFile;
        public nint lpParameters;
        public nint lpDirectory;
        public int nShow;
        public nint hInstApp;
        public nint lpIDList;
        public nint lpClass;
        public nint hkeyClass;
        public uint dwHotKey;
        public nint hIcon;
        public nint hProcess;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SHFILEINFOW
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;
        public fixed char szDisplayName[260];
        public fixed char szTypeName[80];
    }
}
