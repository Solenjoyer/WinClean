using System.Security.Principal;

namespace WinClean.Services;

internal static class ProcessContext
{
    public static bool IsElevated { get; } = DetectElevation();

    private static bool DetectElevation()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
