using System.Buffers.Binary;
using System.Text;

namespace WinClean.Core.Hardware.Parsers;

/// <summary>Reads STORAGE_DEVICE_DESCRIPTOR: fixed fields followed by ASCII strings at the offsets it names.</summary>
public static class StorageDeviceDescriptorParser
{
    public const int FixedLength = 36;

    public static bool TryParse(ReadOnlySpan<byte> buffer, out StorageDeviceDescriptor descriptor)
    {
        descriptor = default;

        if (buffer.Length < FixedLength)
        {
            return false;
        }

        descriptor = new StorageDeviceDescriptor(
            buffer[10] != 0,
            ReadText(buffer, BinaryPrimitives.ReadInt32LittleEndian(buffer[12..])),
            ReadText(buffer, BinaryPrimitives.ReadInt32LittleEndian(buffer[16..])),
            ReadText(buffer, BinaryPrimitives.ReadInt32LittleEndian(buffer[20..])),
            ReadText(buffer, BinaryPrimitives.ReadInt32LittleEndian(buffer[24..])),
            BinaryPrimitives.ReadInt32LittleEndian(buffer[28..]));
        return true;
    }

    private static string ReadText(ReadOnlySpan<byte> buffer, int offset)
    {
        if (offset <= 0 || offset >= buffer.Length)
        {
            return string.Empty;
        }

        var end = buffer[offset..].IndexOf((byte)0);
        var text = end < 0 ? buffer[offset..] : buffer.Slice(offset, end);
        return Encoding.ASCII.GetString(text).Trim();
    }
}
