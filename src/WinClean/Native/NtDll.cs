using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class NtDll
{
    internal const int STATUS_SUCCESS = 0;

    internal const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

    internal const int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);

    internal const int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);

    internal const int STATUS_ACCESS_DENIED = unchecked((int)0xC0000022);

    internal const int ProcessCommandLineInformation = 60;

    internal const int SystemProcessInformation = 5;

    internal const int SystemProcessorPerformanceInformation = 8;

    internal const int SystemMemoryListInformation = 80;

    [LibraryImport("ntdll.dll")]
    internal static partial int NtQuerySystemInformation(int SystemInformationClass, Span<byte> SystemInformation, uint SystemInformationLength, out uint ReturnLength);

    [LibraryImport("ntdll.dll")]
    internal static partial int NtQueryInformationProcess(SafeProcessHandle ProcessHandle, int ProcessInformationClass, Span<byte> ProcessInformation, uint ProcessInformationLength, out uint ReturnLength);

    [LibraryImport("ntdll.dll")]
    internal static partial int NtSuspendProcess(SafeProcessHandle ProcessHandle);

    [LibraryImport("ntdll.dll")]
    internal static partial int NtResumeProcess(SafeProcessHandle ProcessHandle);

    [LibraryImport("ntdll.dll")]
    internal static partial uint RtlNtStatusToDosError(int Status);
}
