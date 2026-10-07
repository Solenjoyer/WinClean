namespace WinClean.Core.Storage;

/// <summary>Bytes and file counts per category, safe to add to from several scan workers.</summary>
public sealed class CategoryTotals
{
    private static readonly int CategoryCount = Enum.GetValues<FileCategory>().Length;

    private readonly long[] _bytes = new long[CategoryCount];

    private readonly int[] _files = new int[CategoryCount];

    public void Add(FileCategory category, long bytes)
    {
        Interlocked.Add(ref _bytes[(int)category], bytes);
        Interlocked.Increment(ref _files[(int)category]);
    }

    public long Bytes(FileCategory category) => Volatile.Read(ref _bytes[(int)category]);

    public int Files(FileCategory category) => Volatile.Read(ref _files[(int)category]);

    /// <summary>Largest category first; empty categories are left out.</summary>
    public IReadOnlyList<CategoryTotal> ToList()
    {
        return Enum.GetValues<FileCategory>()
            .Select(category => new CategoryTotal(category, Bytes(category), Files(category)))
            .Where(total => total.Files > 0)
            .OrderByDescending(total => total.Bytes)
            .ToList();
    }
}
