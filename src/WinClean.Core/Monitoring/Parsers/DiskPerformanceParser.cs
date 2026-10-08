using System.Buffers.Binary;

namespace WinClean.Core.Monitoring.Parsers;

public static class DiskPerformanceParser
{
    /// <summary>sizeof(DISK_PERFORMANCE) on 64-bit Windows.</summary>
    public const int Size = 88;

    public static bool TryParse(ReadOnlySpan<byte> buffer, out DiskPerformance performance)
    {
        if (buffer.Length < 64)
        {
            performance = default;
            return false;
        }

        performance = new DiskPerformance(
            BinaryPrimitives.ReadInt64LittleEndian(buffer),
            BinaryPrimitives.ReadInt64LittleEndian(buffer[8..]),
            BinaryPrimitives.ReadInt64LittleEndian(buffer[16..]),
            BinaryPrimitives.ReadInt64LittleEndian(buffer[24..]),
            BinaryPrimitives.ReadInt64LittleEndian(buffer[32..]),
            BinaryPrimitives.ReadUInt32LittleEndian(buffer[40..]),
            BinaryPrimitives.ReadUInt32LittleEndian(buffer[44..]),
            BinaryPrimitives.ReadUInt32LittleEndian(buffer[48..]),
            BinaryPrimitives.ReadInt64LittleEndian(buffer[56..]));
        return true;
    }
}
