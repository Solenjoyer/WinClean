using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WinClean.Native;

namespace WinClean.Services.Shell;

/// <summary>The one place the app hands something to the outside world: a link or a folder, only when the user asked.</summary>
public sealed class ShellLinks
{
    private readonly ILogger<ShellLinks> _logger;

    public ShellLinks(ILogger<ShellLinks> logger)
    {
        _logger = logger;
    }

    public void OpenUrl(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        Start(url.AbsoluteUri);
    }

    public void OpenFolder(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        Start(path);
    }

    /// <summary>Opens the containing folder in Explorer with the item selected.</summary>
    public void Reveal(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var item = Shell32.ILCreateFromPathW(path);

        if (item != 0)
        {
            try
            {
                if (Shell32.SHOpenFolderAndSelectItems(item, 0, 0, 0) >= 0)
                {
                    return;
                }
            }
            finally
            {
                Shell32.ILFree(item);
            }
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            _logger.LogWarning(exception, "Explorer could not be started for {Path}.", path);
        }
    }

    private void Start(string target)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            _logger.LogWarning(exception, "The shell could not open {Target}.", target);
        }
    }
}
