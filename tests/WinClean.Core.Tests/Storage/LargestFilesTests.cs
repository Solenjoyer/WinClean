using WinClean.Core.Storage;

namespace WinClean.Core.Tests.Storage;

public class LargestFilesTests
{
    [Fact]
    public void Offer_KeepsOnlyTheLargestEntries()
    {
        var largest = new LargestFiles(3);

        foreach (var size in new long[] { 5, 1, 9, 3, 7, 2, 8 })
        {
            largest.Offer(new FileEntry("file" + size, size, DateTime.UnixEpoch));
        }

        Assert.Equal([9, 8, 7], largest.ToList().Select(entry => entry.Size));
    }

    [Fact]
    public void Offer_TiesAreOrderedByPath()
    {
        var largest = new LargestFiles(4);
        largest.Offer(new FileEntry("b", 10, DateTime.UnixEpoch));
        largest.Offer(new FileEntry("a", 10, DateTime.UnixEpoch));
        largest.Offer(new FileEntry("c", 10, DateTime.UnixEpoch));

        Assert.Equal(["a", "b", "c"], largest.ToList().Select(entry => entry.Path));
    }

    [Fact]
    public void Offer_IsSafeFromManyThreads()
    {
        var largest = new LargestFiles(100);

        Parallel.For(0, 20_000, index => largest.Offer(new FileEntry("f" + index, index, DateTime.UnixEpoch)));

        var list = largest.ToList();
        Assert.Equal(100, list.Count);
        Assert.Equal(19_999, list[0].Size);
        Assert.Equal(19_900, list[^1].Size);
    }

    [Fact]
    public void Constructor_RejectsZeroCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LargestFiles(0));
    }
}
