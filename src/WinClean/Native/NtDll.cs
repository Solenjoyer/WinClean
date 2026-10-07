using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class NtDll
{
    internal const int STATUS_SUCCESS = 0;

    internal const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

    internal const int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);

    internal const int SystemProcessInformation = 5;

    internal const int SystemProcessorPerformanceInformation = 8;

    internal const int SystemMemoryListInformation = 80;

    [LibraryImport("ntdll.dll")]
    internal static partial int NtQuerySystemInformation(int SystemInformationClass, Span<byte> SystemInformation, uint SystemInformationLength, out uint ReturnLength);
}
