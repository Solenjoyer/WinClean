using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace WinClean.Services.Shell;

public sealed class ClipboardService : IClipboard
{
    private readonly ILogger<ClipboardService> _logger;

    public ClipboardService(ILogger<ClipboardService> logger)
    {
        _logger = logger;
    }

    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            Clipboard.SetText(text);
        }
        catch (COMException exception)
        {
            // Another application had the clipboard open; the user can simply try again.
            _logger.LogWarning(exception, "The clipboard was not available.");
        }
    }
}
