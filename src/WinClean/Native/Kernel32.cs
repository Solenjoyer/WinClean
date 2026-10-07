using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class Kernel32
{
    internal const uint ATTACH_PARENT_PROCESS = unchecked((uint)-1);

    internal const uint SEM_FAILCRITICALERRORS = 0x0001;

    internal const uint DRIVE_REMOVABLE = 2;
    internal const uint DRIVE_FIXED = 3;
    internal const uint DRIVE_REMOTE = 4;
    internal const uint DRIVE_CDROM = 5;
    internal const uint DRIVE_RAMDISK = 6;

    internal const uint FILE_SHARE_READ = 0x00000001;
    internal const uint FILE_SHARE_WRITE = 0x00000002;
    internal const uint OPEN_EXISTING = 3;

    internal const uint IOCTL_DISK_PERFORMANCE = 0x00070020;

    internal const nint INVALID_HANDLE_VALUE = -1;

    internal const uint FirmwareTypeBios = 1;
    internal const uint FirmwareTypeUefi = 2;

    internal const uint INVALID_FILE_SIZE = 0xFFFFFFFF;

    internal const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
    internal const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;
    internal const uint IO_REPARSE_TAG_CLOUD = 0x9000001A;
    internal const uint IO_REPARSE_TAG_CLOUD_MASK = 0x0000F000;

    internal const uint PROCESS_TERMINATE = 0x0001;
    internal const uint PROCESS_SUSPEND_RESUME = 0x0800;
    internal const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    internal const int ERROR_ACCESS_DENIED = 5;
    internal const int ERROR_INVALID_PARAMETER = 87;
    internal const int ERROR_INSUFFICIENT_BUFFER = 122;

    internal const ushort IMAGE_FILE_MACHINE_UNKNOWN = 0;
    internal const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;
    internal const ushort IMAGE_FILE_MACHINE_ARMNT = 0x01c4;
    internal const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
    internal const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;

    internal const byte BATTERY_FLAG_NO_BATTERY = 128;
    internal const byte BATTERY_PERCENTAGE_UNKNOWN = 255;
    internal const uint BATTERY_LIFE_UNKNOWN = 0xFFFFFFFF;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AttachConsole(uint dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool K32GetPerformanceInfo(ref PERFORMANCE_INFORMATION pPerformanceInformation, uint cb);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetPhysicallyInstalledSystemMemory(out ulong TotalMemoryInKilobytes);

    [LibraryImport("kernel32.dll")]
    internal static partial ulong GetTickCount64();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetFirmwareType(out uint FirmwareType);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint GetLogicalDrives();

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint GetDriveTypeW(string lpRootPathName);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetDiskFreeSpaceExW(string lpDirectoryName, out ulong lpFreeBytesAvailableToCaller, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetVolumeInformationW(
        string lpRootPathName,
        Span<char> lpVolumeNameBuffer,
        uint nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        out uint lpMaximumComponentLength,
        out uint lpFileSystemFlags,
        Span<char> lpFileSystemNameBuffer,
        uint nFileSystemNameSize);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        nint lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        nint hTemplateFile);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ReadOnlySpan<byte> lpInBuffer,
        uint nInBufferSize,
        Span<byte> lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        nint lpOverlapped);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial SafeProcessHandle OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryFullProcessImageNameW(SafeProcessHandle hProcess, uint dwFlags, Span<char> lpExeName, ref uint lpdwSize);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetProcessTimes(SafeProcessHandle hProcess, out long lpCreationTime, out long lpExitTime, out long lpKernelTime, out long lpUserTime);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TerminateProcess(SafeProcessHandle hProcess, uint uExitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsProcessCritical(SafeProcessHandle hProcess, [MarshalAs(UnmanagedType.Bool)] out bool Critical);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWow64Process2(SafeProcessHandle hProcess, out ushort pProcessMachine, out ushort pNativeMachine);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial nint FindFirstFileW(string lpFileName, out WIN32_FIND_DATAW lpFindFileData);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FindClose(nint hFindFile);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial uint GetCompressedFileSizeW(string lpFileName, out uint lpFileSizeHigh);

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public long ftCreationTime;
        public long ftLastAccessTime;
        public long ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        public fixed char cFileName[260];
        public fixed char cAlternateFileName[14];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PERFORMANCE_INFORMATION
    {
        public uint cb;
        public nuint CommitTotal;
        public nuint CommitLimit;
        public nuint CommitPeak;
        public nuint PhysicalTotal;
        public nuint PhysicalAvailable;
        public nuint SystemCache;
        public nuint KernelTotal;
        public nuint KernelPaged;
        public nuint KernelNonpaged;
        public nuint PageSize;
        public uint HandleCount;
        public uint ProcessCount;
        public uint ThreadCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }
}
