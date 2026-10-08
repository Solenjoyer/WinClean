namespace WinClean.Core.Cleanup;

/// <summary>
/// The last check before a file is deleted. Every candidate must be a plain file, written as a clean
/// absolute path, inside the approved root of its category, outside the protected locations, and old
/// enough not to belong to something still running.
/// </summary>
public static class CleanupSafetyPolicy
{
    private static readonly string[] DeviceNames =
        ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

    public static PolicyDecision Evaluate(
        string candidatePath,
        string rootPath,
        FileAttributes attributes,
        DateTime lastWriteUtc,
        DateTime nowUtc,
        TimeSpan minimumAge,
        CleanupPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var candidate = Normalize(candidatePath);
        var root = Normalize(rootPath);

        if (candidate is null || root is null)
        {
            return PolicyDecision.No(IsRooted(candidatePath) && IsRooted(rootPath) ? PolicyRejection.UnsupportedPath : PolicyRejection.NotAbsolute);
        }

        if (string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
        {
            return PolicyDecision.No(PolicyRejection.IsRoot);
        }

        if (!IsUnder(candidate, root))
        {
            return PolicyDecision.No(PolicyRejection.OutsideRoot);
        }

        if (IsProtectedRoot(root, options))
        {
            return PolicyDecision.No(PolicyRejection.ProtectedLocation);
        }

        if (candidate.Count(character => character == '\\') > options.MaximumDepth)
        {
            return PolicyDecision.No(PolicyRejection.TooDeep);
        }

        if ((attributes & FileAttributes.Directory) != 0)
        {
            return PolicyDecision.No(PolicyRejection.Directory);
        }

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            return PolicyDecision.No(PolicyRejection.ReparsePoint);
        }

        if ((attributes & FileAttributes.System) != 0)
        {
            return PolicyDecision.No(PolicyRejection.SystemFile);
        }

        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            return PolicyDecision.No(PolicyRejection.ReadOnlyFile);
        }

        if (minimumAge > TimeSpan.Zero && nowUtc - lastWriteUtc < minimumAge)
        {
            return PolicyDecision.No(PolicyRejection.TooNew);
        }

        return PolicyDecision.Yes;
    }

    /// <summary>
    /// A clean form of a local path: drive letter, backslashes, no relative segments, no trailing dots or
    /// spaces, no device names, no alternate data streams. Null when the path is not a plain local one.
    /// </summary>
    public static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var text = path.Replace('/', '\\');

        if (text.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            text = text[4..];
        }

        if (text.StartsWith(@"\\", StringComparison.Ordinal) || text.Length < 3 || !char.IsAsciiLetter(text[0]) || text[1] != ':' || text[2] != '\\')
        {
            return null;
        }

        var segments = text[3..].Split('\\');
        var cleaned = new List<string>(segments.Length);

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];

            if (segment.Length == 0)
            {
                if (index == segments.Length - 1)
                {
                    continue;
                }

                return null;
            }

            if (segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ') || segment.Contains(':', StringComparison.Ordinal))
            {
                return null;
            }

            var stem = segment.Split('.', 2)[0];

            if (DeviceNames.Any(device => string.Equals(device, stem, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            cleaned.Add(segment);
        }

        var drive = char.ToUpperInvariant(text[0]) + @":\";
        return cleaned.Count == 0 ? drive : drive + string.Join('\\', cleaned);
    }

    public static bool IsUnder(string path, string directory)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(directory);

        var prefix = directory.EndsWith('\\') ? directory : directory + '\\';
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && path.Length > prefix.Length;
    }

    private static bool IsProtectedRoot(string root, CleanupPolicyOptions options)
    {
        var windows = Normalize(options.WindowsDirectory);

        if (windows is not null && (string.Equals(root, windows, StringComparison.OrdinalIgnoreCase) || IsUnder(root, windows)))
        {
            var allowed = options.AllowedWindowsSubfolders
                .Select(Normalize)
                .Any(folder => folder is not null && (string.Equals(root, folder, StringComparison.OrdinalIgnoreCase) || IsUnder(root, folder)));

            if (!allowed)
            {
                return true;
            }
        }

        foreach (var protectedRoot in options.ProtectedRoots.Select(Normalize))
        {
            if (protectedRoot is not null && (string.Equals(root, protectedRoot, StringComparison.OrdinalIgnoreCase) || IsUnder(protectedRoot, root)))
            {
                return true;
            }
        }

        foreach (var tree in options.ProtectedTrees.Select(Normalize))
        {
            if (tree is not null && (string.Equals(root, tree, StringComparison.OrdinalIgnoreCase) || IsUnder(root, tree) || IsUnder(tree, root)))
            {
                return true;
            }
        }

        // A drive root as a cleanup root would make every file on the drive fair game.
        return root.Length <= 3;
    }

    private static bool IsRooted(string? path)
    {
        return path is { Length: >= 3 } && ((path[1] == ':' && path[2] is '\\' or '/') || path.StartsWith(@"\\", StringComparison.Ordinal));
    }
}
