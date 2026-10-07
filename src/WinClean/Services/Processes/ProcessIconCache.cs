using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinClean.Native;

namespace WinClean.Services.Processes;

/// <summary>Executable icons at list size, extracted once per path and frozen so any thread can hand them to the UI.</summary>
public sealed class ProcessIconCache
{
    private readonly ConcurrentDictionary<string, ImageSource?> _icons = new(StringComparer.OrdinalIgnoreCase);

    private readonly int _size = (int)Math.Round(16 * User32.GetDpiForSystem() / 96.0);

    public ImageSource? Get(string path) => _icons.GetOrAdd(path, Extract);

    private ImageSource? Extract(string path)
    {
        var icon = ExtractFromFile(path);

        if (icon == 0)
        {
            icon = ShellIcon(path);
        }

        if (icon == 0)
        {
            return null;
        }

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            User32.DestroyIcon(icon);
        }
    }

    /// <summary>The first icon resource rendered at exactly the requested size, which stays crisp at any scale factor.</summary>
    private nint ExtractFromFile(string path)
    {
        Span<nint> icons = stackalloc nint[1];
        Span<uint> ids = stackalloc uint[1];
        var extracted = User32.PrivateExtractIconsW(path, 0, _size, _size, icons, ids, 1, 0);
        return extracted == 1 ? icons[0] : 0;
    }

    /// <summary>What Explorer would show, for files without icons of their own.</summary>
    private static nint ShellIcon(string path)
    {
        var info = default(Shell32.SHFILEINFOW);
        var result = Shell32.SHGetFileInfoW(path, 0, ref info, (uint)Marshal.SizeOf<Shell32.SHFILEINFOW>(), Shell32.SHGFI_ICON | Shell32.SHGFI_SMALLICON);
        return result == 0 ? 0 : info.hIcon;
    }
}
