using System.Runtime.CompilerServices;
using WinClean.Core.Monitoring.Parsers;
using WinClean.Native;
using WinClean.Native.Handles;

namespace WinClean.Services.Monitoring;

/// <summary>A PDH query with English counter paths, so it works on every Windows language.</summary>
internal sealed class PdhQuery : IDisposable
{
    private readonly PdhQueryHandle _handle;

    private byte[] _arrayBuffer = [];

    private PdhQuery(PdhQueryHandle handle)
    {
        _handle = handle;
    }

    public static PdhQuery? Open(out string? reason)
    {
        var status = Pdh.PdhOpenQueryW(0, 0, out var handle);

        if (status != Pdh.ERROR_SUCCESS)
        {
            reason = $"PdhOpenQuery failed (0x{status:X8})";
            handle.Dispose();
            return null;
        }

        reason = null;
        return new PdhQuery(handle);
    }

    /// <summary>Returns the counter handle, or null with the PDH status when the object or counter does not exist on this machine.</summary>
    public nint? AddCounter(string path, out uint status)
    {
        status = Pdh.PdhAddEnglishCounterW(_handle, path, 0, out var counter);
        return status == Pdh.ERROR_SUCCESS ? counter : null;
    }

    public bool Collect() => Pdh.PdhCollectQueryData(_handle) == Pdh.ERROR_SUCCESS;

    public static double? ReadValue(nint counter)
    {
        var status = Pdh.PdhGetFormattedCounterValue(counter, Pdh.PDH_FMT_DOUBLE | Pdh.PDH_FMT_NOCAP100, out _, out var value);
        return status == Pdh.ERROR_SUCCESS && value.CStatus is Pdh.PDH_CSTATUS_VALID_DATA or Pdh.PDH_CSTATUS_NEW_DATA ? value.doubleValue : null;
    }

    /// <summary>Every instance of a wildcard counter. Empty on the first collection, before PDH has two samples.</summary>
    public unsafe List<CounterArrayItem> ReadArray(nint counter)
    {
        uint size = 0;
        var status = Pdh.PdhGetFormattedCounterArrayW(counter, Pdh.PDH_FMT_DOUBLE | Pdh.PDH_FMT_NOCAP100, ref size, out var count, []);

        if (status != Pdh.PDH_MORE_DATA || size == 0)
        {
            return [];
        }

        if (_arrayBuffer.Length < size)
        {
            // Pinned, because the items point back into the buffer and the parser turns those pointers into offsets.
            _arrayBuffer = GC.AllocateUninitializedArray<byte>((int)size * 2, pinned: true);
        }

        size = (uint)_arrayBuffer.Length;
        status = Pdh.PdhGetFormattedCounterArrayW(counter, Pdh.PDH_FMT_DOUBLE | Pdh.PDH_FMT_NOCAP100, ref size, out count, _arrayBuffer);

        if (status != Pdh.ERROR_SUCCESS)
        {
            return [];
        }

        var address = (long)Unsafe.AsPointer(ref _arrayBuffer[0]);
        return CounterArrayParser.Parse(_arrayBuffer.AsSpan(0, (int)size), address, (int)count);
    }

    public void Dispose() => _handle.Dispose();

    public static string Describe(uint status) => status switch
    {
        Pdh.PDH_CSTATUS_NO_OBJECT => "the performance object does not exist on this system",
        Pdh.PDH_CSTATUS_NO_COUNTER => "the counter does not exist on this system",
        Pdh.PDH_CSTATUS_NO_INSTANCE => "the counter has no instances yet",
        _ => $"PDH status 0x{status:X8}",
    };
}
