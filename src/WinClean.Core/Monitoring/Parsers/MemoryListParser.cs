using System.Buffers.Binary;

namespace WinClean.Core.Monitoring.Parsers;

/// <summary>
/// Reads SYSTEM_MEMORY_LIST_INFORMATION (22 pointer-sized counters on 64-bit). Standby plus modified
/// pages are what Task Manager calls "Cached".
/// </summary>
public static class MemoryListParser
{
    public const int Size = 22 * 8;

    public static bool TryParse(ReadOnlySpan<byte> buffer, long pageSize, out long standbyBytes, out long modifiedBytes)
    {
        standbyBytes = 0;
        modifiedBytes = 0;

        if (buffer.Length < Size || pageSize <= 0)
        {
            return false;
        }

        var modifiedPages = BinaryPrimitives.ReadInt64LittleEndian(buffer[16..]);
        long standbyPages = 0;

        for (var priority = 0; priority < 8; priority++)
        {
            standbyPages += BinaryPrimitives.ReadInt64LittleEndian(buffer[(40 + priority * 8)..]);
        }

        standbyBytes = standbyPages * pageSize;
        modifiedBytes = modifiedPages * pageSize;
        return true;
    }
}
