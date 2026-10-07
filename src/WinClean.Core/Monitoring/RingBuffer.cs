namespace WinClean.Core.Monitoring;

/// <summary>
/// A fixed-size history for sparklines. Writers append, readers copy a snapshot; both are cheap and
/// nothing is allocated after construction.
/// </summary>
public sealed class RingBuffer<T>
{
    private readonly T[] _items;

    private readonly object _gate = new();

    private int _start;

    private int _count;

    private int _version;

    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _items = new T[capacity];
    }

    public int Capacity => _items.Length;

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _count;
            }
        }
    }

    /// <summary>Changes on every append, so a reader can skip redrawing when nothing moved.</summary>
    public int Version => Volatile.Read(ref _version);

    public void Add(T item)
    {
        lock (_gate)
        {
            if (_count < _items.Length)
            {
                _items[(_start + _count) % _items.Length] = item;
                _count++;
            }
            else
            {
                _items[_start] = item;
                _start = (_start + 1) % _items.Length;
            }

            _version++;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _start = 0;
            _count = 0;
            _version++;
        }
    }

    /// <summary>Copies the history oldest first and returns how many items were written.</summary>
    public int CopyTo(Span<T> destination)
    {
        lock (_gate)
        {
            var count = Math.Min(_count, destination.Length);
            var skip = _count - count;

            for (var index = 0; index < count; index++)
            {
                destination[index] = _items[(_start + skip + index) % _items.Length];
            }

            return count;
        }
    }

    public T[] ToArray()
    {
        lock (_gate)
        {
            var copy = new T[_count];
            CopyTo(copy);
            return copy;
        }
    }
}
