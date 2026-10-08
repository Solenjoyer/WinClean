using System.Buffers.Binary;
using System.Text;

namespace WinClean.Core.Monitoring.Parsers;

/// <summary>
/// Reads a MIB_IF_TABLE2 as returned by GetIfTable2 straight from its bytes. The row layout is the
/// documented 64-bit one (1352 bytes per row, rows starting at offset 8).
/// </summary>
public static class NetworkInterfaceTableParser
{
    public const int TableHeaderSize = 8;

    public const int RowSize = 1352;

    private const int LuidOffset = 0;
    private const int IndexOffset = 8;
    private const int AliasOffset = 28;
    private const int DescriptionOffset = 542;
    private const int TypeOffset = 1128;
    private const int FlagsOffset = 1152;
    private const int OperStatusOffset = 1156;
    private const int MediaConnectStateOffset = 1164;
    private const int TransmitLinkSpeedOffset = 1192;
    private const int ReceiveLinkSpeedOffset = 1200;
    private const int InOctetsOffset = 1208;
    private const int OutOctetsOffset = 1280;
    private const int NameCharacters = 257;

    public static IReadOnlyList<NetworkInterfaceRow> Parse(ReadOnlySpan<byte> table)
    {
        if (table.Length < TableHeaderSize)
        {
            return [];
        }

        var count = BinaryPrimitives.ReadUInt32LittleEndian(table);
        var rows = new List<NetworkInterfaceRow>((int)Math.Min(count, 256));

        for (var index = 0; index < count; index++)
        {
            var offset = TableHeaderSize + index * RowSize;

            if (offset + RowSize > table.Length)
            {
                break;
            }

            rows.Add(ParseRow(table.Slice(offset, RowSize)));
        }

        return rows;
    }

    public static NetworkInterfaceRow ParseRow(ReadOnlySpan<byte> row)
    {
        if (row.Length < RowSize)
        {
            throw new ArgumentException($"A MIB_IF_ROW2 is {RowSize} bytes.", nameof(row));
        }

        var flags = row[FlagsOffset];

        return new NetworkInterfaceRow(
            BinaryPrimitives.ReadInt64LittleEndian(row[LuidOffset..]),
            BinaryPrimitives.ReadInt32LittleEndian(row[IndexOffset..]),
            ReadString(row.Slice(AliasOffset, NameCharacters * 2)),
            ReadString(row.Slice(DescriptionOffset, NameCharacters * 2)),
            BinaryPrimitives.ReadInt32LittleEndian(row[TypeOffset..]),
            BinaryPrimitives.ReadInt32LittleEndian(row[OperStatusOffset..]),
            BinaryPrimitives.ReadInt32LittleEndian(row[MediaConnectStateOffset..]),
            HardwareInterface: (flags & 0x01) != 0,
            ConnectorPresent: (flags & 0x04) != 0,
            BinaryPrimitives.ReadUInt64LittleEndian(row[TransmitLinkSpeedOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(row[ReceiveLinkSpeedOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(row[InOctetsOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(row[OutOctetsOffset..]));
    }

    private static string ReadString(ReadOnlySpan<byte> utf16)
    {
        var text = Encoding.Unicode.GetString(utf16);
        var terminator = text.IndexOf('\0', StringComparison.Ordinal);
        return terminator < 0 ? text : text[..terminator];
    }
}
