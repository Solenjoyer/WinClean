namespace WinClean.Core.Storage;

/// <summary>Maps file extensions to the handful of categories the storage page shows.</summary>
public static class FileTypeCategories
{
    private static readonly Dictionary<string, FileCategory> ByExtension = Build();

    public static FileCategory Categorize(ReadOnlySpan<char> fileName)
    {
        var dot = fileName.LastIndexOf('.');

        if (dot < 0 || dot == fileName.Length - 1)
        {
            return FileCategory.Other;
        }

        var extension = fileName[(dot + 1)..];

        if (extension.Length > 12)
        {
            return FileCategory.Other;
        }

        Span<char> lower = stackalloc char[extension.Length];
        extension.ToLowerInvariant(lower);

        return ByExtension.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(lower, out var category) ? category : FileCategory.Other;
    }

    private static Dictionary<string, FileCategory> Build()
    {
        var map = new Dictionary<string, FileCategory>(StringComparer.Ordinal);

        Add(map, FileCategory.Video, "mp4", "mkv", "avi", "mov", "wmv", "webm", "m4v", "mpg", "mpeg", "flv", "ts", "m2ts", "3gp");
        Add(map, FileCategory.Image, "jpg", "jpeg", "png", "gif", "bmp", "tif", "tiff", "webp", "heic", "heif", "raw", "cr2", "cr3", "nef", "arw", "dng", "psd", "ai", "svg", "ico", "avif");
        Add(map, FileCategory.Audio, "mp3", "wav", "flac", "aac", "m4a", "ogg", "wma", "opus", "aiff", "aif", "alac");
        Add(map, FileCategory.Document, "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "rtf", "odt", "ods", "odp", "md", "epub", "csv", "one", "pages", "numbers", "key", "log");
        Add(map, FileCategory.Archive, "zip", "7z", "rar", "tar", "gz", "tgz", "bz2", "xz", "zst", "cab", "lz4", "nupkg", "jar", "whl");
        Add(map, FileCategory.Installer, "msi", "msix", "msixbundle", "appx", "appxbundle", "msp", "msu");
        Add(map, FileCategory.Program, "exe", "dll", "sys", "ocx", "drv", "cpl", "scr", "com", "node", "pyd", "so", "dylib", "winmd");
        Add(map, FileCategory.Code, "cs", "vb", "fs", "js", "mjs", "cjs", "ts", "tsx", "jsx", "py", "java", "kt", "kts", "scala", "c", "cpp", "cc", "h", "hpp", "go", "rs", "rb", "php", "swift",
            "json", "xml", "xaml", "yaml", "yml", "toml", "html", "htm", "css", "scss", "less", "sql", "sh", "ps1", "psm1", "bat", "cmd", "csproj", "sln", "slnx", "props", "targets", "lua", "r", "dart", "vue", "svelte");
        Add(map, FileCategory.BuildOutput, "obj", "pdb", "lib", "a", "o", "class", "pyc", "map", "ilk", "exp", "idb", "tlog", "res", "nupkg.metadata", "cache");
        Add(map, FileCategory.DiskImage, "vhdx", "vhd", "iso", "vmdk", "wim", "esd", "img", "qcow2", "vdi", "dmg", "swm");
        Add(map, FileCategory.Database, "db", "sqlite", "sqlite3", "db-wal", "db-shm", "mdf", "ldf", "ndf", "accdb", "mdb", "bak", "ibd", "frm", "rdb", "ldb");

        return map;
    }

    private static void Add(Dictionary<string, FileCategory> map, FileCategory category, params string[] extensions)
    {
        foreach (var extension in extensions)
        {
            map[extension] = category;
        }
    }
}
