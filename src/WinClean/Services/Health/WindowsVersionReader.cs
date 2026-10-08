using Microsoft.Win32;
using WinClean.Core.Health;
using WinClean.Native;

namespace WinClean.Services.Health;

/// <summary>The kernel's own version numbers plus the registry details the Settings app shows.</summary>
internal static class WindowsVersionReader
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    public static WindowsVersion Read()
    {
        var info = new NtDll.OSVERSIONINFOEXW();

        unsafe
        {
            info.dwOSVersionInfoSize = (uint)sizeof(NtDll.OSVERSIONINFOEXW);
        }

        NtDll.RtlGetVersion(ref info);

        using var key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey);
        var revision = key?.GetValue("UBR") is int ubr ? ubr : 0;
        DateTimeOffset? installed = key?.GetValue("InstallDate") is int seconds && seconds > 0 ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;

        return new WindowsVersion(
            (int)info.dwMajorVersion,
            (int)info.dwMinorVersion,
            (int)info.dwBuildNumber,
            revision,
            key?.GetValue("DisplayVersion") as string,
            key?.GetValue("EditionID") as string,
            key?.GetValue("ProductName") as string,
            key?.GetValue("InstallationType") as string,
            installed);
    }
}
