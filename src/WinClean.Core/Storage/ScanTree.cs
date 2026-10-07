namespace WinClean.Core.Storage;

/// <summary>
/// The folder tree produced by a scan. Nodes live in chunked arrays and link to each other by index
/// (parent, first child, next sibling), which keeps a million folders in a few dozen megabytes.
/// Files are never stored individually: each folder only counts them.
/// </summary>
public sealed class ScanTree
{
    private const int ChunkSize = 1 << 16;

    private readonly List<DirNode[]> _nodes = [];

    private readonly List<string[]> _names = [];

    private readonly object _growLock = new();

    private int _count;

    public int Count => _count;

    public int AddRoot(string name) => Add(-1, name);

    /// <summary>Children of one folder are added by the worker listing it, so sibling links need no lock.</summary>
    public int AddChild(int parent, string name)
    {
        var index = Add(parent, name);
        ref var parentNode = ref NodeAt(parent);
        NodeAt(index).NextSibling = parentNode.FirstChild;
        parentNode.FirstChild = index;
        parentNode.OwnDirs++;
        return index;
    }

    public void AddFile(int node, long size, long sizeOnDisk, bool cloudOnly, long lastWriteTicks)
    {
        ref var n = ref NodeAt(node);
        n.OwnFiles++;
        n.OwnBytes += size;
        n.OwnDiskBytes += sizeOnDisk;

        if (cloudOnly)
        {
            n.CloudOnlyBytes += size;
        }

        if (lastWriteTicks > n.NewestWriteTicks)
        {
            n.NewestWriteTicks = lastWriteTicks;
        }
    }

    public void Mark(int node, FolderState state) => NodeAt(node).State |= state;

    /// <summary>Rolls sizes and counts up to the root. Children always have a larger index than their parent, so one reverse pass is enough.</summary>
    public void Aggregate()
    {
        for (var index = 0; index < _count; index++)
        {
            ref var node = ref NodeAt(index);
            node.TotalBytes = node.OwnBytes;
            node.TotalDiskBytes = node.OwnDiskBytes;
            node.TotalCloudOnlyBytes = node.CloudOnlyBytes;
            node.TotalFiles = node.OwnFiles;
            node.TotalDirs = node.OwnDirs;
            node.TotalNewestWriteTicks = node.NewestWriteTicks;
            node.TotalDenied = (node.State & FolderState.AccessDenied) != 0 ? 1 : 0;
        }

        for (var index = _count - 1; index > 0; index--)
        {
            ref var node = ref NodeAt(index);

            if (node.Parent < 0)
            {
                continue;
            }

            ref var parent = ref NodeAt(node.Parent);
            parent.TotalBytes += node.TotalBytes;
            parent.TotalDiskBytes += node.TotalDiskBytes;
            parent.TotalCloudOnlyBytes += node.TotalCloudOnlyBytes;
            parent.TotalFiles += node.TotalFiles;
            parent.TotalDirs += node.TotalDirs;
            parent.TotalDenied += node.TotalDenied;

            if (node.TotalNewestWriteTicks > parent.TotalNewestWriteTicks)
            {
                parent.TotalNewestWriteTicks = node.TotalNewestWriteTicks;
            }
        }
    }

    public string Name(int node) => _names[node / ChunkSize][node % ChunkSize];

    public int Parent(int node) => NodeAt(node).Parent;

    public long TotalBytes(int node) => NodeAt(node).TotalBytes;

    public long TotalDiskBytes(int node) => NodeAt(node).TotalDiskBytes;

    public long OwnBytes(int node) => NodeAt(node).OwnBytes;

    public long CloudOnlyBytes(int node) => NodeAt(node).TotalCloudOnlyBytes;

    public int TotalFiles(int node) => NodeAt(node).TotalFiles;

    public int OwnFiles(int node) => NodeAt(node).OwnFiles;

    public int TotalDirs(int node) => NodeAt(node).TotalDirs;

    /// <summary>Folders below this one (itself included) that could not be listed.</summary>
    public int DeniedFolders(int node) => NodeAt(node).TotalDenied;

    public long NewestWriteTicks(int node) => NodeAt(node).TotalNewestWriteTicks;

    public FolderState State(int node) => NodeAt(node).State;

    public IEnumerable<int> Children(int node)
    {
        for (var child = NodeAt(node).FirstChild; child >= 0; child = NodeAt(child).NextSibling)
        {
            yield return child;
        }
    }

    /// <summary>Direct children ordered by total size, largest first.</summary>
    public List<int> ChildrenBySize(int node)
    {
        var children = Children(node).ToList();
        children.Sort((left, right) => TotalBytes(right).CompareTo(TotalBytes(left)));
        return children;
    }

    /// <summary>The node for a path below the root, matched segment by segment without regard to case; -1 when absent.</summary>
    public int FindByPath(string path, char separator = '\\')
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_count == 0)
        {
            return -1;
        }

        var root = Name(0).TrimEnd(separator);
        var normalized = path.TrimEnd(separator);

        if (!normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        if (normalized.Length == root.Length)
        {
            return 0;
        }

        if (normalized[root.Length] != separator)
        {
            return -1;
        }

        var current = 0;

        foreach (var segment in normalized[(root.Length + 1)..].Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            var next = -1;

            foreach (var child in Children(current))
            {
                if (string.Equals(Name(child), segment, StringComparison.OrdinalIgnoreCase))
                {
                    next = child;
                    break;
                }
            }

            if (next < 0)
            {
                return -1;
            }

            current = next;
        }

        return current;
    }

    /// <summary>The full path: the root's name is the scanned path itself.</summary>
    public string Path(int node, char separator = '\\')
    {
        var parts = new Stack<string>();

        for (var current = node; current >= 0; current = NodeAt(current).Parent)
        {
            parts.Push(Name(current));
        }

        var root = parts.Pop();
        return parts.Count == 0 ? root : root.TrimEnd(separator) + separator + string.Join(separator, parts);
    }

    private int Add(int parent, string name)
    {
        int index;

        lock (_growLock)
        {
            index = _count;

            if (index / ChunkSize == _nodes.Count)
            {
                _nodes.Add(new DirNode[ChunkSize]);
                _names.Add(new string[ChunkSize]);
            }

            _count++;
        }

        _names[index / ChunkSize][index % ChunkSize] = name;
        ref var node = ref NodeAt(index);
        node.Parent = parent;
        node.FirstChild = -1;
        node.NextSibling = -1;
        return index;
    }

    private ref DirNode NodeAt(int index) => ref _nodes[index / ChunkSize][index % ChunkSize];

    private struct DirNode
    {
        public int Parent;
        public int FirstChild;
        public int NextSibling;
        public int OwnFiles;
        public int OwnDirs;
        public int TotalFiles;
        public int TotalDirs;
        public int TotalDenied;
        public long OwnBytes;
        public long OwnDiskBytes;
        public long CloudOnlyBytes;
        public long TotalBytes;
        public long TotalDiskBytes;
        public long TotalCloudOnlyBytes;
        public long NewestWriteTicks;
        public long TotalNewestWriteTicks;
        public FolderState State;
    }
}
