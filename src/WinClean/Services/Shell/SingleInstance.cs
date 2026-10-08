using System.Diagnostics;
using WinClean.Native;
using WinClean.ViewModels;

namespace WinClean.Services.Shell;

/// <summary>One WinClean per session. A second launch hands its page request to the running window and exits.</summary>
internal sealed class SingleInstance : IDisposable
{
    private const string Identity = "WinClean.3E2F0C58-7B6A-4D1E-9C44-5A1F0B2D8E61";

    private readonly Mutex _mutex;

    private SingleInstance(Mutex mutex, bool isFirst)
    {
        _mutex = mutex;
        IsFirst = isFirst;
    }

    /// <summary>Broadcast by a second instance with the page index as wParam; the main window listens for it.</summary>
    public static uint ActivationMessage { get; } = User32.RegisterWindowMessageW(Identity + ".Activate");

    public bool IsFirst { get; }

    public static SingleInstance Acquire(int? replacedProcessId)
    {
        if (replacedProcessId is int processId)
        {
            WaitForExit(processId);
        }

        var mutex = new Mutex(initiallyOwned: true, @"Local\" + Identity, out var createdNew);
        return new SingleInstance(mutex, createdNew || TryTakeOver(mutex));
    }

    public static void ActivateRunningInstance(string? page)
    {
        User32.PostMessageW(User32.HWND_BROADCAST, ActivationMessage, PageKeys.IndexOf(page), 0);
    }

    public void Dispose()
    {
        if (IsFirst)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }

    private static bool TryTakeOver(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            // The previous instance died without releasing it; ownership has passed to this one.
            return true;
        }
    }

    private static void WaitForExit(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.WaitForExit(5000);
        }
        catch (ArgumentException)
        {
            // Already gone.
        }
        catch (InvalidOperationException)
        {
        }
    }
}
