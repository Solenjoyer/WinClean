using WinClean.Native;

namespace WinClean.Services;

/// <summary>
/// A WinExe has no console. When started from a terminal, attaching to the parent's console lets
/// --version and --self-check print where the user is looking.
/// </summary>
internal static class ConsoleOutput
{
    public static bool Attach()
    {
        if (!Kernel32.AttachConsole(Kernel32.ATTACH_PARENT_PROCESS))
        {
            return false;
        }

        // The shell has already printed its prompt on the current line.
        Console.WriteLine();
        return true;
    }
}
