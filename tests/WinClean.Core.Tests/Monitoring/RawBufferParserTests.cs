using System.Buffers.Binary;
using System.Text;
using WinClean.Core.Monitoring.Parsers;

namespace WinClean.Core.Tests.Monitoring;

public class RawBufferParserTests
{
    [Fact]
    public void ProcessorTimes_ReadsOneEntryPerProcessor()
    {
        var buffer = new byte[2 * ProcessorTimesParser.EntrySize];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, 100);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8), 300);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(16), 50);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(48), 7);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(56), 9);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(64), 11);

        var times = ProcessorTimesParser.Parse(buffer);

        Assert.Equal(2, times.Length);
        Assert.Equal(new ProcessorTimes(100, 300, 50), times[0]);
        Assert.Equal(350, times[0].Total);
        Assert.Equal(new ProcessorTimes(7, 9, 11), times[1]);
        Assert.Empty(ProcessorTimesParser.Parse(new byte[10]));
    }

    [Fact]
    public void DiskPerformance_ReadsTheCounters()
    {
        var buffer = new byte[DiskPerformanceParser.Size];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, 1000);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(8), 2000);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(32), 5_000_000);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(40), 12);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(48), 3);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(56), 10_000_000);

        Assert.True(DiskPerformanceParser.TryParse(buffer, out var performance));
        Assert.Equal(1000, performance.BytesRead);
        Assert.Equal(2000, performance.BytesWritten);
        Assert.Equal(5_000_000, performance.IdleTime);
        Assert.Equal(12u, performance.ReadCount);
        Assert.Equal(3u, performance.QueueDepth);
        Assert.Equal(10_000_000, performance.QueryTime);
        Assert.False(DiskPerformanceParser.TryParse(new byte[10], out _));
    }

    [Fact]
    public void MemoryList_SumsStandbyPagesByPriority()
    {
        var buffer = new byte[MemoryListParser.Size];
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(16), 25);

        for (var priority = 0; priority < 8; priority++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(40 + priority * 8), 10);
        }

        Assert.True(MemoryListParser.TryParse(buffer, 4096, out var standby, out var modified));
        Assert.Equal(80 * 4096, standby);
        Assert.Equal(25 * 4096, modified);
        Assert.False(MemoryListParser.TryParse(new byte[20], 4096, out _, out _));
    }

    [Fact]
    public void CounterArray_ResolvesNamesThroughTheBufferAddress()
    {
        const long baseAddress = 0x10_0000;
        var names = new[] { "pid_4_luid_0x00000000_0x0000F1C9_phys_0_eng_0_engtype_3D", "pid_8_luid_0x00000000_0x0000F1C9_phys_0_eng_1_engtype_Copy" };
        var items = names.Length;
        var namesOffset = items * CounterArrayParser.ItemSize;
        var nameBytes = names.Select(name => Encoding.Unicode.GetBytes(name + "\0")).ToArray();
        var buffer = new byte[namesOffset + nameBytes.Sum(bytes => bytes.Length)];
        var cursor = namesOffset;

        for (var index = 0; index < items; index++)
        {
            var itemOffset = index * CounterArrayParser.ItemSize;
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(itemOffset), baseAddress + cursor);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(itemOffset + 8), 0);
            BinaryPrimitives.WriteDoubleLittleEndian(buffer.AsSpan(itemOffset + 16), 12.5 * (index + 1));
            nameBytes[index].CopyTo(buffer, cursor);
            cursor += nameBytes[index].Length;
        }

        var parsed = CounterArrayParser.Parse(buffer, baseAddress, items);

        Assert.Equal(2, parsed.Count);
        Assert.Equal(names[0], parsed[0].Instance);
        Assert.Equal(12.5, parsed[0].Value);
        Assert.Equal(names[1], parsed[1].Instance);
        Assert.Equal(25.0, parsed[1].Value);
    }

    [Fact]
    public void CounterArray_SkipsItemsWhosePointerLeavesTheBuffer()
    {
        var buffer = new byte[CounterArrayParser.ItemSize];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, 0x50);

        Assert.Empty(CounterArrayParser.Parse(buffer, 0x10_0000, 1));
    }
}
