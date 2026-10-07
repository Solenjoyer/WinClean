using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

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
