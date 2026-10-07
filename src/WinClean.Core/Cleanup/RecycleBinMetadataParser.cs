using System.Buffers.Binary;
using System.Text;

namespace WinClean.Core.Cleanup;

/// <summary>
/// Reads the $I metadata files in $Recycle.Bin. Version 1 (Vista to 8.1) stores a fixed 520-byte
/// path; version 2 (Windows 10 and later) stores a length-prefixed one. The matching $R file holds
/// the content.
/// </summary>
public static class RecycleBinMetadataParser
{
    public const string MetadataPrefix = "$I";

    public const string ContentPrefix = "$R";

    private const int HeaderLength = 24;

    private const int FixedPathLength = 520;

    private const int MaximumPathCharacters = 32768;

    public static bool TryParse(ReadOnlySpan<byte> data, out RecycleBinEntry? entry)
    {
        entry = null;

        if (data.Length < HeaderLength)
        {
            return false;
        }

        var version = BinaryPrimitives.ReadInt64LittleEndian(data);
        var size = BinaryPrimitives.ReadInt64LittleEndian(data[8..]);
        var fileTime = BinaryPrimitives.ReadInt64LittleEndian(data[16..]);

        if (size < 0 || fileTime < 0)
        {
            return false;
        }

        DateTimeOffset deletedAt;

        try
        {
            deletedAt = DateTimeOffset.FromFileTime(fileTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        string path;

        switch (version)
        {
            case 1:
                if (data.Length < HeaderLength + FixedPathLength)
                {
                    return false;
                }

                path = ReadNullTerminated(data.Slice(HeaderLength, FixedPathLength));
                break;

            case 2:
                if (data.Length < HeaderLength + 4)
                {
                    return false;
                }

                var characters = BinaryPrimitives.ReadInt32LittleEndian(data[HeaderLength..]);

                if (characters <= 0 || characters > MaximumPathCharacters || data.Length < HeaderLength + 4 + characters * 2)
                {
                    return false;
                }

                path = ReadNullTerminated(data.Slice(HeaderLength + 4, characters * 2));
                break;

            default:
                return false;
        }

        if (path.Length == 0)
        {
            return false;
        }

        entry = new RecycleBinEntry(path, size, deletedAt, (int)version);
        return true;
    }

    /// <summary>"$IABC123.txt" describes "$RABC123.txt".</summary>
    public static string? ContentFileName(string metadataFileName)
    {
        ArgumentNullException.ThrowIfNull(metadataFileName);

        return metadataFileName.StartsWith(MetadataPrefix, StringComparison.Ordinal)
            ? ContentPrefix + metadataFileName[MetadataPrefix.Length..]
            : null;
    }

    private static string ReadNullTerminated(ReadOnlySpan<byte> utf16)
    {
        var text = Encoding.Unicode.GetString(utf16);
        var terminator = text.IndexOf('\0', StringComparison.Ordinal);
        return terminator < 0 ? text : text[..terminator];
    }
}
