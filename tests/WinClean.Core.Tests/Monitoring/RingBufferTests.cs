using WinClean.Core.Monitoring;

namespace WinClean.Core.Tests.Monitoring;

public class RingBufferTests
{
    [Fact]
    public void Add_WrapsAndKeepsTheNewestItems()
    {
        var buffer = new RingBuffer<int>(3);

        foreach (var value in new[] { 1, 2, 3, 4, 5 })
        {
            buffer.Add(value);
        }

        Assert.Equal(3, buffer.Count);
        Assert.Equal([3, 4, 5], buffer.ToArray());
    }

    [Fact]
    public void CopyTo_SmallerDestination_ReceivesTheNewestItems()
    {
        var buffer = new RingBuffer<int>(5);

        for (var value = 1; value <= 4; value++)
        {
            buffer.Add(value);
        }

        var destination = new int[2];
        var written = buffer.CopyTo(destination);

        Assert.Equal(2, written);
        Assert.Equal([3, 4], destination);
    }

    [Fact]
    public void Version_ChangesOnEveryWrite()
    {
        var buffer = new RingBuffer<float>(2);
        var before = buffer.Version;

        buffer.Add(1);
        buffer.Add(2);
        buffer.Clear();

        Assert.Equal(before + 3, buffer.Version);
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Constructor_RejectsZeroCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }
}
