using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class Advapi32
{
    internal const uint TOKEN_QUERY = 0x0008;

    internal const int TokenUser = 1;

    internal const int TokenElevation = 20;

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool OpenProcessToken(SafeProcessHandle ProcessHandle, uint DesiredAccess, out SafeAccessTokenHandle TokenHandle);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetTokenInformation(SafeAccessTokenHandle TokenHandle, int TokenInformationClass, Span<byte> TokenInformation, uint TokenInformationLength, out uint ReturnLength);
}
