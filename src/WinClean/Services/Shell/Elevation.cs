using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;
using WinClean.Native;

namespace WinClean.Services.Shell;

/// <summary>Starts an elevated copy of WinClean that takes over from this one; the user sees the standard consent prompt.</summary>
public sealed class Elevation
{
    private readonly ILogger<Elevation> _logger;

    public Elevation(ILogger<Elevation> logger)
    {
        _logger = logger;
    }

    public static bool IsElevated => ProcessContext.IsElevated;

    /// <summary>True when the elevated instance was started and this one is shutting down; false when the user declined.</summary>
    public bool RestartElevated(string? page)
    {
        var executable = Environment.ProcessPath;

        if (executable is null)
        {
            return false;
        }

        var arguments = $"--elevated --replace-pid {Environment.ProcessId}";

        if (page is not null)
        {
            arguments += $" --page {page}";
        }

        var verb = Marshal.StringToCoTaskMemUni("runas");
        var file = Marshal.StringToCoTaskMemUni(executable);
        var parameters = Marshal.StringToCoTaskMemUni(arguments);

        try
        {
            var info = new Shell32.SHELLEXECUTEINFOW
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.SHELLEXECUTEINFOW>(),
                lpVerb = verb,
                lpFile = file,
                lpParameters = parameters,
                nShow = Shell32.SW_SHOWNORMAL,
            };

            if (!Shell32.ShellExecuteExW(ref info))
            {
                var error = Marshal.GetLastPInvokeError();

                if (error != Shell32.ERROR_CANCELLED)
                {
                    _logger.LogWarning("Restarting as administrator failed: {Error}", new Win32Exception(error).Message);
                }

                return false;
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(verb);
            Marshal.FreeCoTaskMem(file);
            Marshal.FreeCoTaskMem(parameters);
        }

        _logger.LogInformation("An elevated instance is taking over.");
        ((App)Application.Current).ExitApplication();
        return true;
    }
}
