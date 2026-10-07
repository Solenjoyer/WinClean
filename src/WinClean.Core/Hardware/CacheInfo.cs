namespace WinClean.Core.Hardware;

/// <summary>One cache level as the processor reports it: size per instance and how many instances exist.</summary>
public sealed record CacheInfo(int Level, CacheKind Kind, long SizeBytes, int Instances)
{
    public long TotalBytes => SizeBytes * Instances;
}
