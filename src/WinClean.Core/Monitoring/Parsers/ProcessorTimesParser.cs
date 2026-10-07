using System.Buffers.Binary;

namespace WinClean.Core.Monitoring.Parsers;

/// <summary>Reads the SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION array (48 bytes per processor).</summary>
public static class ProcessorTimesParser
{
    public const int EntrySize = 48;

    public static ProcessorTimes[] Parse(ReadOnlySpan<byte> buffer)
    {
        var count = buffer.Length / EntrySize;
        var times = new ProcessorTimes[count];

        for (var index = 0; index < count; index++)
        {
            var entry = buffer.Slice(index * EntrySize, EntrySize);
            times[index] = new ProcessorTimes(
                BinaryPrimitives.ReadInt64LittleEndian(entry),
                BinaryPrimitives.ReadInt64LittleEndian(entry[8..]),
                BinaryPrimitives.ReadInt64LittleEndian(entry[16..]));
        }

        return times;
    }
}
