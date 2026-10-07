using Microsoft.Win32;
using WinClean.Core.Health;

namespace WinClean.Services.Health;

/// <summary>The registry places where Windows notes that a restart is still owed.</summary>
internal static class PendingRestartReader
{
    public static PendingRestartFacts Read()
    {
        return new PendingRestartFacts(
            KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"),
            KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"),
            Value(@"SYSTEM\CurrentControlSet\Control\Session Manager", "PendingFileRenameOperations") is string[] { Length: > 0 },
            ComputerNameChanged(),
            Value(@"SOFTWARE\Microsoft\Updates", "UpdateExeVolatile") is int volatileFlag && volatileFlag != 0);
    }

    private static bool KeyExists(string path)
    {
        using var key = Registry.LocalMachine.OpenSubKey(path);
        return key is not null;
    }

    private static object? Value(string path, string name)
    {
        using var key = Registry.LocalMachine.OpenSubKey(path);
        return key?.GetValue(name);
    }

    private static bool ComputerNameChanged()
    {
        var active = Value(@"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName", "ComputerName") as string;
        var pending = Value(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName", "ComputerName") as string;
        return active is not null && pending is not null && !string.Equals(active, pending, StringComparison.OrdinalIgnoreCase);
    }
}
