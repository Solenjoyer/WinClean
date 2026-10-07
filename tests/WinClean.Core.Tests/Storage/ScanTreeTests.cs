using WinClean.Core.Storage;

namespace WinClean.Core.Tests.Storage;

public class ScanTreeTests
{
    [Fact]
    public void Aggregate_RollsSizesAndCountsUpToTheRoot()
    {
        var tree = new ScanTree();
        var root = tree.AddRoot(@"C:\");
        var users = tree.AddChild(root, "Users");
        var dev = tree.AddChild(users, "dev");
        var downloads = tree.AddChild(dev, "Downloads");
        var windows = tree.AddChild(root, "Windows");

        tree.AddFile(root, 10, 12, cloudOnly: false, lastWriteTicks: 5);
        tree.AddFile(downloads, 100, 104, cloudOnly: false, lastWriteTicks: 50);
        tree.AddFile(downloads, 200, 200, cloudOnly: true, lastWriteTicks: 70);
        tree.AddFile(windows, 1000, 1000, cloudOnly: false, lastWriteTicks: 20);
        tree.Mark(windows, FolderState.AccessDenied);
        tree.Aggregate();

        Assert.Equal(1310, tree.TotalBytes(root));
        Assert.Equal(1316, tree.TotalDiskBytes(root));
        Assert.Equal(300, tree.TotalBytes(users));
        Assert.Equal(200, tree.CloudOnlyBytes(root));
        Assert.Equal(4, tree.TotalFiles(root));
        Assert.Equal(2, tree.TotalFiles(dev));
        Assert.Equal(4, tree.TotalDirs(root));
        Assert.Equal(2, tree.TotalDirs(users));
        Assert.Equal(0, tree.TotalDirs(downloads));
        Assert.Equal(1, tree.DeniedFolders(root));
        Assert.Equal(0, tree.DeniedFolders(users));
        Assert.Equal(70, tree.NewestWriteTicks(root));
        Assert.Equal(20, tree.NewestWriteTicks(windows));
    }

    [Fact]
    public void ChildrenBySize_OrdersLargestFirst()
    {
        var tree = new ScanTree();
        var root = tree.AddRoot(@"D:\");
        var small = tree.AddChild(root, "small");
        var large = tree.AddChild(root, "large");
        var medium = tree.AddChild(root, "medium");
        tree.AddFile(small, 1, 1, false, 0);
        tree.AddFile(large, 300, 300, false, 0);
        tree.AddFile(medium, 20, 20, false, 0);
        tree.Aggregate();

        Assert.Equal([large, medium, small], tree.ChildrenBySize(root));
        Assert.Equal(3, tree.Children(root).Count());
    }

    [Fact]
    public void Path_ReconstructsFromTheRoot()
    {
        var tree = new ScanTree();
        var root = tree.AddRoot(@"C:\");
        var users = tree.AddChild(root, "Users");
        var dev = tree.AddChild(users, "dev");
        var otherRoot = tree.AddRoot(@"D:\Projects");
        var app = tree.AddChild(otherRoot, "app");

        Assert.Equal(@"C:\", tree.Path(root));
        Assert.Equal(@"C:\Users\dev", tree.Path(dev));
        Assert.Equal(@"D:\Projects\app", tree.Path(app));
        Assert.Equal(users, tree.Parent(dev));
        Assert.Equal(-1, tree.Parent(root));
    }

    [Fact]
    public void Add_GrowsAcrossChunks()
    {
        var tree = new ScanTree();
        var root = tree.AddRoot("root");

        for (var index = 0; index < 70_000; index++)
        {
            var child = tree.AddChild(root, "child" + index);
            tree.AddFile(child, 1, 1, false, 0);
        }

        tree.Aggregate();

        Assert.Equal(70_001, tree.Count);
        Assert.Equal(70_000, tree.TotalBytes(root));
        Assert.Equal(70_000, tree.TotalDirs(root));
        Assert.Equal("child69999", tree.Name(70_000));
    }

    [Fact]
    public void AddChild_FromParallelWorkersOnDifferentParents_IsSafe()
    {
        var tree = new ScanTree();
        var root = tree.AddRoot("root");
        var parents = Enumerable.Range(0, 16).Select(index => tree.AddChild(root, "p" + index)).ToArray();

        Parallel.ForEach(parents, parent =>
        {
            for (var index = 0; index < 2000; index++)
            {
                var child = tree.AddChild(parent, "c" + index);
                tree.AddFile(child, 2, 2, false, 0);
            }
        });

        tree.Aggregate();

        Assert.Equal(1 + 16 + 16 * 2000, tree.Count);
        Assert.Equal(16 * 2000 * 2, tree.TotalBytes(root));
        Assert.All(parents, parent => Assert.Equal(2000, tree.TotalDirs(parent)));
    }
}
