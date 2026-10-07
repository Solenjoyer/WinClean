using System.IO;
using System.Security.Principal;
using WinClean.Core.Cleanup;

namespace WinClean.Services.Cleanup;

/// <summary>The current account's Recycle Bin entries on one drive, read from their $I metadata files.</summary>
internal static class RecycleBinContents
{
    public static IReadOnlyList<CleanupItem> Read(string root)
    {
        var items = new List<CleanupItem>();
        using var identity = WindowsIdentity.GetCurrent();

        if (identity.User is null)
        {
            return items;
        }

        var folder = Path.Combine(root, "$Recycle.Bin", identity.User.Value);

        if (!Directory.Exists(folder))
        {
            return items;
        }

        try
        {
            foreach (var metadata in Directory.EnumerateFiles(folder, RecycleBinMetadataParser.MetadataPrefix + "*"))
            {
                try
                {
                    if (RecycleBinMetadataParser.TryParse(File.ReadAllBytes(metadata), out var entry) && entry is not null)
                    {
                        items.Add(new CleanupItem(CleanupCategoryIds.RecycleBin, entry.OriginalPath, entry.Size, entry.DeletedAt, CleanupItemKind.RecycleBinEntry, root));
                    }
                }
                catch (IOException)
                {
                    // Being emptied or restored right now.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return items;
    }
}
