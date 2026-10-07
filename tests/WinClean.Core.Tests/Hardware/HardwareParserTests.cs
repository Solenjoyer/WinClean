using System.Buffers.Binary;
using System.Text;
using WinClean.Core.Hardware;
using WinClean.Core.Hardware.Parsers;

namespace WinClean.Core.Tests.Hardware;

public class HardwareParserTests
{
    [Fact]
    public void LogicalProcessorInfo_CountsCoresThreadsCachesAndHybridClasses()
    {
        var buffer = new List<byte>();
        buffer.AddRange(Core(efficiencyClass: 1, mask: 0b11));
        buffer.AddRange(Core(efficiencyClass: 1, mask: 0b1100));
        buffer.AddRange(Core(efficiencyClass: 0, mask: 0b10000));
        buffer.AddRange(Core(efficiencyClass: 0, mask: 0b100000));
        buffer.AddRange(Cache(level: 1, kind: 2, size: 48 * 1024, mask: 0b11));
        buffer.AddRange(Cache(level: 1, kind: 2, size: 48 * 1024, mask: 0b1100));
        buffer.AddRange(Cache(level: 3, kind: 0, size: 24 * 1024 * 1024, mask: 0b111111));
        buffer.AddRange(Cache(level: 3, kind: 0, size: 24 * 1024 * 1024, mask: 0b111111));
        buffer.AddRange(Package(mask: 0b111111));

        var topology = LogicalProcessorInfoParser.Parse(buffer.ToArray());

        Assert.Equal(4, topology.Cores);
        Assert.Equal(6, topology.LogicalProcessors);
        Assert.Equal(1, topology.Packages);
        Assert.Equal(2, topology.PerformanceCores);
        Assert.Equal(2, topology.EfficiencyCores);
        Assert.True(topology.IsHybrid);
        Assert.Equal(2, topology.Caches.Count);
        Assert.Equal(new CacheInfo(1, CacheKind.Data, 48 * 1024, 2), topology.Caches[0]);
        Assert.Equal(new CacheInfo(3, CacheKind.Unified, 24 * 1024 * 1024, 1), topology.Caches[1]);
        Assert.Equal(ProcessorTopology.Empty with { Caches = topology.Caches }, LogicalProcessorInfoParser.Parse(new byte[4]) with { Caches = topology.Caches });
    }

    [Fact]
    public void StorageDescriptor_ReadsStringsAtTheirOffsets()
    {
        var strings = Encoding.ASCII.GetBytes("Samsung\0SSD 990 PRO 2TB\0S7KH\0SERIAL123\0");
        var buffer = new byte[StorageDeviceDescriptorParser.FixedLength + strings.Length];
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(12), 36);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(16), 36 + 8);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(20), 36 + 8 + 16);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(24), 36 + 8 + 16 + 5);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(28), 17);
        strings.CopyTo(buffer, StorageDeviceDescriptorParser.FixedLength);

        Assert.True(StorageDeviceDescriptorParser.TryParse(buffer, out var descriptor));
        Assert.Equal("Samsung", descriptor.Vendor);
        Assert.Equal("SSD 990 PRO 2TB", descriptor.Product);
        Assert.Equal("Samsung SSD 990 PRO 2TB", descriptor.Model);
        Assert.Equal("SERIAL123", descriptor.SerialNumber);
        Assert.Equal("NVMe", descriptor.BusName);
        Assert.False(descriptor.RemovableMedia);
        Assert.False(StorageDeviceDescriptorParser.TryParse(new byte[10], out _));
    }

    private static byte[] Core(byte efficiencyClass, ulong mask) => Relation(0, efficiencyClass, mask);

    private static byte[] Package(ulong mask) => Relation(3, 0, mask);

    private static byte[] Relation(int relationship, byte efficiencyClass, ulong mask)
    {
        var record = new byte[48];
        BinaryPrimitives.WriteInt32LittleEndian(record, relationship);
        BinaryPrimitives.WriteInt32LittleEndian(record.AsSpan(4), record.Length);
        record[9] = efficiencyClass;
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(30), 1);
        BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(32), mask);
        return record;
    }

    private static byte[] Cache(byte level, int kind, uint size, ulong mask)
    {
        var record = new byte[56];
        BinaryPrimitives.WriteInt32LittleEndian(record, 2);
        BinaryPrimitives.WriteInt32LittleEndian(record.AsSpan(4), record.Length);
        record[8] = level;
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(12), size);
        BinaryPrimitives.WriteInt32LittleEndian(record.AsSpan(16), kind);
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(38), 1);
        BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(40), mask);
        return record;
    }
}
