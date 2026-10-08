using System.Runtime.InteropServices;
using WinClean.Core.Monitoring;
using WinClean.Core.Monitoring.Parsers;
using WinClean.Native;

namespace WinClean.Services.Monitoring;

internal sealed class MemorySampler
{
    private readonly byte[] _memoryListBuffer = new byte[MemoryListParser.Size];

    private long _installed = -1;

    public string? Reason { get; private set; }

    public MemorySample? Sample()
    {
        var status = new Kernel32.MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<Kernel32.MEMORYSTATUSEX>() };

        if (!Kernel32.GlobalMemoryStatusEx(ref status))
        {
            Reason = Win32Reason.LastError();
            return null;
        }

        Reason = null;

        var performance = new Kernel32.PERFORMANCE_INFORMATION { cb = (uint)Marshal.SizeOf<Kernel32.PERFORMANCE_INFORMATION>() };
        var hasPerformance = Kernel32.K32GetPerformanceInfo(ref performance, performance.cb);
        var pageSize = hasPerformance ? (long)performance.PageSize : Environment.SystemPageSize;

        long cached = 0;

        if (NtDll.NtQuerySystemInformation(NtDll.SystemMemoryListInformation, _memoryListBuffer, (uint)_memoryListBuffer.Length, out _) == NtDll.STATUS_SUCCESS
            && MemoryListParser.TryParse(_memoryListBuffer, pageSize, out var standby, out var modified))
        {
            cached = standby + modified;
        }
        else if (hasPerformance)
        {
            cached = (long)performance.SystemCache * pageSize;
        }

        return new MemorySample(
            (long)status.ullTotalPhys,
            (long)status.ullAvailPhys,
            Installed(),
            hasPerformance ? (long)performance.CommitTotal * pageSize : (long)(status.ullTotalPageFile - status.ullAvailPageFile),
            hasPerformance ? (long)performance.CommitLimit * pageSize : (long)status.ullTotalPageFile,
            cached,
            hasPerformance ? (long)performance.KernelPaged * pageSize : 0,
            hasPerformance ? (long)performance.KernelNonpaged * pageSize : 0,
            hasPerformance ? (int)performance.ProcessCount : 0,
            hasPerformance ? (int)performance.ThreadCount : 0,
            hasPerformance ? (int)performance.HandleCount : 0);
    }

    private long Installed()
    {
        if (_installed < 0)
        {
            _installed = Kernel32.GetPhysicallyInstalledSystemMemory(out var kilobytes) ? (long)kilobytes * 1024 : 0;
        }

        return _installed;
    }
}
