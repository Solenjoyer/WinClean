namespace WinClean.Core.Storage;

/// <summary>Keeps the N largest files seen during a scan; everything smaller than the current floor is dropped at once.</summary>
public sealed class LargestFiles
{
    private readonly PriorityQueue<FileEntry, long> _heap = new();

    private readonly int _capacity;

    private readonly object _gate = new();

    private long _floor;

    public LargestFiles(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public int Capacity => _capacity;

    public void Offer(FileEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Size <= Volatile.Read(ref _floor))
        {
            return;
        }

        lock (_gate)
        {
            if (_heap.Count >= _capacity && entry.Size <= _floor)
            {
                return;
            }

            _heap.Enqueue(entry, entry.Size);

            if (_heap.Count > _capacity)
            {
                _heap.Dequeue();
            }

            if (_heap.Count >= _capacity)
            {
                _heap.TryPeek(out _, out var smallest);
                Volatile.Write(ref _floor, smallest);
            }
        }
    }

    /// <summary>Largest first; equal sizes keep a stable order by path.</summary>
    public IReadOnlyList<FileEntry> ToList()
    {
        lock (_gate)
        {
            return _heap.UnorderedItems
                .Select(item => item.Element)
                .OrderByDescending(entry => entry.Size)
                .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
