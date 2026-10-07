using System.Buffers.Binary;
using System.Text;

namespace WinClean.Core.Monitoring.Parsers;

/// <summary>
/// Reads the buffer PdhGetFormattedCounterArrayW fills: an array of 24-byte items (a name pointer and a
/// formatted value) followed by the names themselves. Name pointers are absolute, so the buffer's own
/// address is needed to turn them into offsets.
/// </summary>
public static class CounterArrayParser
{
    public const int ItemSize = 24;

    public static List<CounterArrayItem> Parse(ReadOnlySpan<byte> buffer, long bufferAddress, int itemCount)
    {
        var items = new List<CounterArrayItem>(Math.Max(itemCount, 0));

        for (var index = 0; index < itemCount; index++)
        {
            var offset = index * ItemSize;

            if (offset + ItemSize > buffer.Length)
            {
                break;
            }

            var namePointer = BinaryPrimitives.ReadInt64LittleEndian(buffer[offset..]);
            var status = BinaryPrimitives.ReadUInt32LittleEndian(buffer[(offset + 8)..]);
            var value = BinaryPrimitives.ReadDoubleLittleEndian(buffer[(offset + 16)..]);
            var nameOffset = namePointer - bufferAddress;

            if (nameOffset < 0 || nameOffset >= buffer.Length)
            {
                continue;
            }

            items.Add(new CounterArrayItem(ReadString(buffer[(int)nameOffset..]), status, value));
        }

        return items;
    }

    private static string ReadString(ReadOnlySpan<byte> utf16)
    {
        var length = 0;

        while (length + 1 < utf16.Length && (utf16[length] != 0 || utf16[length + 1] != 0))
        {
            length += 2;
        }

        return Encoding.Unicode.GetString(utf16[..length]);
    }
}
