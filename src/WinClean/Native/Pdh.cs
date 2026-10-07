using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WinClean.Native.Handles;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class Pdh
{
    internal const uint ERROR_SUCCESS = 0;
    internal const uint PDH_MORE_DATA = 0x800007D2;
    internal const uint PDH_NO_DATA = 0x800007D5;
    internal const uint PDH_CSTATUS_NO_INSTANCE = 0x800007D1;
    internal const uint PDH_CSTATUS_NO_OBJECT = 0xC0000BB8;
    internal const uint PDH_CSTATUS_NO_COUNTER = 0xC0000BB9;
    internal const uint PDH_INVALID_DATA = 0xC0000BC6;
    internal const uint PDH_CALC_NEGATIVE_DENOMINATOR = 0x800007D6;
    internal const uint PDH_CALC_NEGATIVE_VALUE = 0x800007D8;

    internal const uint PDH_CSTATUS_VALID_DATA = 0;
    internal const uint PDH_CSTATUS_NEW_DATA = 1;

    internal const uint PDH_FMT_DOUBLE = 0x00000200;
    internal const uint PDH_FMT_NOCAP100 = 0x00008000;

    /// <summary>sizeof(PDH_FMT_COUNTERVALUE_ITEM_W) on 64-bit: a name pointer followed by the value.</summary>
    internal const int CounterValueItemSize = 24;

    [LibraryImport("pdh.dll")]
    internal static partial uint PdhOpenQueryW(nint szDataSource, nint dwUserData, out PdhQueryHandle phQuery);

    [LibraryImport("pdh.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint PdhAddEnglishCounterW(PdhQueryHandle hQuery, string szFullCounterPath, nint dwUserData, out nint phCounter);

    [LibraryImport("pdh.dll")]
    internal static partial uint PdhRemoveCounter(nint hCounter);

    [LibraryImport("pdh.dll")]
    internal static partial uint PdhCollectQueryData(PdhQueryHandle hQuery);

    [LibraryImport("pdh.dll")]
    internal static partial uint PdhGetFormattedCounterValue(nint hCounter, uint dwFormat, out uint lpdwType, out PDH_FMT_COUNTERVALUE pValue);

    [LibraryImport("pdh.dll")]
    internal static partial uint PdhGetFormattedCounterArrayW(nint hCounter, uint dwFormat, ref uint lpdwBufferSize, out uint lpdwItemCount, Span<byte> ItemBuffer);

    [LibraryImport("pdh.dll")]
    internal static partial uint PdhCloseQuery(nint hQuery);

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct PDH_FMT_COUNTERVALUE
    {
        [FieldOffset(0)]
        public uint CStatus;

        [FieldOffset(8)]
        public double doubleValue;

        [FieldOffset(8)]
        public long largeValue;
    }
}
