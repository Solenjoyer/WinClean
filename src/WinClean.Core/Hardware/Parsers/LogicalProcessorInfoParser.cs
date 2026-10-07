using System.Buffers.Binary;
using System.Numerics;

namespace WinClean.Core.Hardware.Parsers;

/// <summary>
/// Walks the SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX records of GetLogicalProcessorInformationEx
/// (RelationAll). Records are variable length; each carries its own size. Caches are deduplicated by
/// level, type and the processors they serve, so a shared L3 counts once.
/// </summary>
public static class LogicalProcessorInfoParser
{
    public const int RelationProcessorCore = 0;

    public const int RelationCache = 2;

    public const int RelationProcessorPackage = 3;

    private const int HeaderLength = 8;

    private const int GroupAffinityLength = 16;

    public static ProcessorTopology Parse(ReadOnlySpan<byte> buffer)
    {
        var cores = 0;
        var logical = 0;
        var packages = 0;
        var performance = 0;
        var efficiency = 0;
        var efficiencyClasses = new HashSet<int>();
        var coreClasses = new List<int>();
        var caches = new Dictionary<(int Level, CacheKind Kind, long Size, string Mask), int>();
        var offset = 0;

        while (offset + HeaderLength <= buffer.Length)
        {
            var relationship = BinaryPrimitives.ReadInt32LittleEndian(buffer[offset..]);
            var size = BinaryPrimitives.ReadInt32LittleEndian(buffer[(offset + 4)..]);

            if (size < HeaderLength || offset + size > buffer.Length)
            {
                break;
            }

            var record = buffer.Slice(offset, size);

            switch (relationship)
            {
                case RelationProcessorCore:
                    {
                        cores++;
                        var efficiencyClass = record[9];
                        coreClasses.Add(efficiencyClass);
                        efficiencyClasses.Add(efficiencyClass);
                        logical += CountBits(record, groupCountOffset: 30, masksOffset: 32);
                        break;
                    }

                case RelationProcessorPackage:
                    packages++;
                    break;

                case RelationCache:
                    {
                        var level = record[8];
                        var kind = record.Length > 16 ? (CacheKind)Math.Clamp(BinaryPrimitives.ReadInt32LittleEndian(record[16..]), 0, 3) : CacheKind.Unified;
                        var cacheSize = BinaryPrimitives.ReadUInt32LittleEndian(record[12..]);
                        var key = (level, kind, (long)cacheSize, MaskText(record, groupCountOffset: 38, masksOffset: 40));
                        caches[key] = caches.GetValueOrDefault(key) + 1;
                        break;
                    }
            }

            offset += size;
        }

        // The highest efficiency class is the performance core on hybrid processors.
        if (efficiencyClasses.Count > 1)
        {
            var top = efficiencyClasses.Max();
            performance = coreClasses.Count(cls => cls == top);
            efficiency = cores - performance;
        }

        var cacheInfos = caches
            .GroupBy(pair => (pair.Key.Level, pair.Key.Kind, pair.Key.Size))
            .Select(group => new CacheInfo(group.Key.Level, group.Key.Kind, group.Key.Size, group.Count()))
            .OrderBy(cache => cache.Level)
            .ThenBy(cache => cache.Kind)
            .ToList();

        return new ProcessorTopology(cores, logical, packages, performance, efficiency, cacheInfos);
    }

    private static int CountBits(ReadOnlySpan<byte> record, int groupCountOffset, int masksOffset)
    {
        var count = 0;

        foreach (var mask in Masks(record, groupCountOffset, masksOffset))
        {
            count += BitOperations.PopCount(mask);
        }

        return count;
    }

    private static string MaskText(ReadOnlySpan<byte> record, int groupCountOffset, int masksOffset)
    {
        return string.Join(',', Masks(record, groupCountOffset, masksOffset).Select(mask => mask.ToString("X", System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static List<ulong> Masks(ReadOnlySpan<byte> record, int groupCountOffset, int masksOffset)
    {
        var masks = new List<ulong>();

        if (record.Length < groupCountOffset + 2)
        {
            return masks;
        }

        // Older systems report no group count and exactly one mask.
        var groups = Math.Max(1, (int)BinaryPrimitives.ReadUInt16LittleEndian(record[groupCountOffset..]));

        for (var index = 0; index < groups; index++)
        {
            var maskOffset = masksOffset + index * GroupAffinityLength;

            if (maskOffset + 8 > record.Length)
            {
                break;
            }

            masks.Add(BinaryPrimitives.ReadUInt64LittleEndian(record[maskOffset..]));
        }

        return masks;
    }
}
