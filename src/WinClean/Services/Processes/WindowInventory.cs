using System.Runtime.InteropServices;
using WinClean.Native;

namespace WinClean.Services.Processes;

/// <summary>
/// Which processes own a window the user can see, and whether that window answers. The same rules as
/// the taskbar: visible, unowned, not a tool window, not cloaked, with a title.
/// </summary>
internal sealed unsafe class WindowInventory
{
    private readonly char[] _title = new char[512];

    private Dictionary<int, WindowFacts> _result = [];

    public Dictionary<int, WindowFacts> Scan()
    {
        _result = [];
        var self = GCHandle.Alloc(this);

        try
        {
            User32.EnumWindows(&Visit, GCHandle.ToIntPtr(self));
        }
        finally
        {
            self.Free();
        }

        return _result;
    }

    [UnmanagedCallersOnly]
    private static int Visit(nint window, nint parameter)
    {
        try
        {
            var inventory = (WindowInventory)GCHandle.FromIntPtr(parameter).Target!;
            inventory.Inspect(window);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // An exception must not cross into user32; the window is simply skipped.
        }

        return 1;
    }

    private void Inspect(nint window)
    {
        if (!User32.IsWindowVisible(window) || User32.GetWindow(window, User32.GW_OWNER) != 0)
        {
            return;
        }

        if ((User32.GetWindowLongPtrW(window, User32.GWL_EXSTYLE) & User32.WS_EX_TOOLWINDOW) != 0)
        {
            return;
        }

        // Store apps parked in the background keep a cloaked window around.
        if (DwmApi.DwmGetWindowAttribute(window, DwmApi.DWMWA_CLOAKED, out var cloaked, sizeof(uint)) == 0 && cloaked != 0)
        {
            return;
        }

        var length = User32.GetWindowTextW(window, _title, _title.Length);

        if (length <= 0)
        {
            return;
        }

        User32.GetWindowThreadProcessId(window, out var pid);

        if (pid == 0)
        {
            return;
        }

        if (!_result.TryGetValue((int)pid, out var facts))
        {
            facts = new WindowFacts();
            _result[(int)pid] = facts;
        }

        facts.Titles.Add(new string(_title, 0, length));
        facts.IsHung |= User32.IsHungAppWindow(window);
    }
}
