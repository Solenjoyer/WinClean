using WinClean.Core.Storage;

namespace WinClean.Core.Tests.Storage;

public class ScanTreeLookupTests
{
    [Fact]
    public void FindByPath_WalksSegmentsIgnoringCase()
    {
        var tree = new ScanTree();
        var root = tree.AddRoot(@"C:\");
        var users = tree.AddChild(root, "Users");
        var dev = tree.AddChild(users, "dev");
        tree.AddChild(root, "Windows");

        Assert.Equal(root, tree.FindByPath(@"C:\"));
        Assert.Equal(root, tree.FindByPath("c:"));
        Assert.Equal(dev, tree.FindByPath(@"c:\users\DEV\"));
        Assert.Equal(-1, tree.FindByPath(@"C:\Users\other"));
        Assert.Equal(-1, tree.FindByPath(@"D:\Users"));
        Assert.Equal(-1, tree.FindByPath(@"C:\Use"));
    }

    [Fact]
    public void LargestFiles_Accepts_ReflectsTheFloor()
    {
        var largest = new LargestFiles(2);
        Assert.True(largest.Accepts(1));

        largest.Offer(new FileEntry("a", 100, DateTime.UnixEpoch));
        largest.Offer(new FileEntry("b", 200, DateTime.UnixEpoch));

        Assert.False(largest.Accepts(100));
        Assert.True(largest.Accepts(101));
    }
}
