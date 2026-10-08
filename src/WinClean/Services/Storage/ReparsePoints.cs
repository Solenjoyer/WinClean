using WinClean.Native;

namespace WinClean.Services.Storage;

/// <summary>Tells the reparse points a scan may enter (cloud folders) from the ones it must not (links, mount points, virtualised folders).</summary>
internal static class ReparsePoints
{
    public static bool TryReadTag(string path, out uint tag)
    {
        var handle = Kernel32.FindFirstFileW(path, out var data);

        if (handle == Kernel32.INVALID_HANDLE_VALUE)
        {
            tag = 0;
            return false;
        }

        Kernel32.FindClose(handle);
        tag = data.dwReserved0;
        return true;
    }

    /// <summary>OneDrive and other cloud providers mark their folders with a tag from the cloud family; the files inside are real entries.</summary>
    public static bool IsCloudFolder(uint tag) => (tag & ~Kernel32.IO_REPARSE_TAG_CLOUD_MASK) == Kernel32.IO_REPARSE_TAG_CLOUD;
}
