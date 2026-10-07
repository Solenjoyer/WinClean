using System.Runtime.InteropServices;
using WinClean.Native;

namespace WinClean.Services.Storage;

/// <summary>The shell's own figure for the Recycle Bin of one drive.</summary>
internal static class RecycleBinReader
{
    public static (long Bytes, long Items)? Query(string root)
    {
        var info = new Shell32.SHQUERYRBINFO { cbSize = (uint)Marshal.SizeOf<Shell32.SHQUERYRBINFO>() };
        return Shell32.SHQueryRecycleBinW(root, ref info) >= 0 ? (info.i64Size, info.i64NumItems) : null;
    }
}
