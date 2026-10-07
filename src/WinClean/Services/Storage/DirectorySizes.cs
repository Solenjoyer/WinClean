using System.IO;
using System.IO.Enumeration;
using WinClean.Core.Applications;

namespace WinClean.Services.Storage;

/// <summary>Sums one folder without building a tree, for the known locations and cleanup previews.</summary>
internal static class DirectorySizes
{
    private static readonly EnumerationOptions Recursive = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = 0,
        IgnoreInaccessible = true,
        ReturnSpecialDirectories = false,
        BufferSize = 64 * 1024,
    };

    private static readonly EnumerationOptions TopLevel = new()
    {
        RecurseSubdirectories = false,
        AttributesToSkip = 0,
        IgnoreInaccessible = true,
        ReturnSpecialDirectories = false,
        BufferSize = 64 * 1024,
    };

    /// <summary>Bytes and files under a path; a file path counts itself. Patterns restrict the count to matching top-level files.</summary>
    public static (long Bytes, int Files) Measure(string path, IReadOnlyList<string>? patterns, CancellationToken cancellationToken)
    {
        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            return patterns is null || patterns.Any(pattern => Wildcard.IsMatch(info.Name, pattern)) ? (info.Length, 1) : (0, 0);
        }

        if (!Directory.Exists(path))
        {
            return (0, 0);
        }

        long bytes = 0;
        var files = 0;

        var enumerable = new FileSystemEnumerable<long>(path, (ref FileSystemEntry entry) => entry.Length, patterns is null ? Recursive : TopLevel)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && (patterns is null || Matches(entry.FileName, patterns)),
            ShouldRecursePredicate = (ref FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0,
        };

        try
        {
            foreach (var length in enumerable)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bytes += length;
                files++;
            }
        }
        catch (IOException)
        {
            // The root vanished or is not readable; what was counted stays.
        }
        catch (UnauthorizedAccessException)
        {
        }

        return (bytes, files);
    }

    private static bool Matches(ReadOnlySpan<char> name, IReadOnlyList<string> patterns)
    {
        var text = name.ToString();
        return patterns.Any(pattern => Wildcard.IsMatch(text, pattern));
    }
}
