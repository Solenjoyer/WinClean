using System.Buffers.Binary;
using System.Text;
using WinClean.Core.Monitoring.Parsers;

namespace WinClean.Core.Tests.Monitoring;

public class NetworkInterfaceTableParserTests
{
    [Fact]
    public void Parse_ReadsEveryRowAtTheDocumentedOffsets()
    {
        var table = new byte[NetworkInterfaceTableParser.TableHeaderSize + 2 * NetworkInterfaceTableParser.RowSize];
        BinaryPrimitives.WriteUInt32LittleEndian(table, 2);
        WriteRow(table.AsSpan(8, NetworkInterfaceTableParser.RowSize), luid: 0x0006_0000_0000_0001, index: 7, alias: "Ethernet", description: "Intel(R) Ethernet Controller I225-V",
            type: 6, flags: 0b0000_0101, operStatus: 1, mediaState: 1, speed: 2_500_000_000, inOctets: 123_456_789, outOctets: 987_654);
        WriteRow(table.AsSpan(8 + NetworkInterfaceTableParser.RowSize, NetworkInterfaceTableParser.RowSize), luid: 0x0018_0000_0000_0002, index: 1, alias: "Loopback Pseudo-Interface 1", description: "Software Loopback Interface 1",
            type: 24, flags: 0, operStatus: 1, mediaState: 1, speed: 1_073_741_824, inOctets: 0, outOctets: 0);

        var rows = NetworkInterfaceTableParser.Parse(table);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Ethernet", rows[0].Alias);
        Assert.Equal("Intel(R) Ethernet Controller I225-V", rows[0].Description);
        Assert.Equal(7, rows[0].Index);
        Assert.Equal(6, rows[0].Type);
        Assert.True(rows[0].HardwareInterface);
        Assert.True(rows[0].ConnectorPresent);
        Assert.Equal(2_500_000_000UL, rows[0].TransmitLinkSpeed);
        Assert.Equal(123_456_789UL, rows[0].InOctets);
        Assert.Equal(987_654UL, rows[0].OutOctets);
        Assert.True(rows[0].CountsTowardsTotals);
        Assert.False(rows[1].CountsTowardsTotals);
        Assert.False(rows[1].HardwareInterface);
    }

    [Fact]
    public void Parse_TruncatedTable_ReturnsOnlyCompleteRows()
    {
        var table = new byte[NetworkInterfaceTableParser.TableHeaderSize + NetworkInterfaceTableParser.RowSize + 100];
        BinaryPrimitives.WriteUInt32LittleEndian(table, 2);
        WriteRow(table.AsSpan(8, NetworkInterfaceTableParser.RowSize), 1, 1, "Wi-Fi", "Adapter", 71, 0b101, 1, 1, 0, 0, 0);

        Assert.Single(NetworkInterfaceTableParser.Parse(table));
        Assert.Empty(NetworkInterfaceTableParser.Parse(new byte[4]));
    }

    [Theory]
    [InlineData(6, 1, 0b001, true)]
    [InlineData(71, 1, 0b100, true)]
    [InlineData(243, 1, 0b001, true)]
    [InlineData(6, 2, 0b001, false)]
    [InlineData(23, 1, 0b000, false)]
    [InlineData(6, 1, 0b000, false)]
    public void CountsTowardsTotals_RequiresAnUpPhysicalAdapter(int type, int operStatus, byte flags, bool expected)
    {
        var row = new byte[NetworkInterfaceTableParser.RowSize];
        WriteRow(row, 1, 1, "x", "x", type, flags, operStatus, 1, 0, 0, 0);

        Assert.Equal(expected, NetworkInterfaceTableParser.ParseRow(row).CountsTowardsTotals);
    }

    private static void WriteRow(Span<byte> row, long luid, int index, string alias, string description, int type, byte flags, int operStatus, int mediaState, ulong speed, ulong inOctets, ulong outOctets)
    {
        BinaryPrimitives.WriteInt64LittleEndian(row, luid);
        BinaryPrimitives.WriteInt32LittleEndian(row[8..], index);
        Encoding.Unicode.GetBytes(alias).CopyTo(row[28..]);
        Encoding.Unicode.GetBytes(description).CopyTo(row[542..]);
        BinaryPrimitives.WriteInt32LittleEndian(row[1128..], type);
        row[1152] = flags;
        BinaryPrimitives.WriteInt32LittleEndian(row[1156..], operStatus);
        BinaryPrimitives.WriteInt32LittleEndian(row[1164..], mediaState);
        BinaryPrimitives.WriteUInt64LittleEndian(row[1192..], speed);
        BinaryPrimitives.WriteUInt64LittleEndian(row[1200..], speed);
        BinaryPrimitives.WriteUInt64LittleEndian(row[1208..], inOctets);
        BinaryPrimitives.WriteUInt64LittleEndian(row[1280..], outOctets);
    }
}
