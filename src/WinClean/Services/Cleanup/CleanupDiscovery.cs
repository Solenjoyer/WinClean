using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using Microsoft.Extensions.Logging;
using WinClean.Core.Applications;
using WinClean.Core.Cleanup;
using WinClean.Core.Settings;
using WinClean.Core.Storage;
using WinClean.Services.Storage;

namespace WinClean.Services.Cleanup;

/// <summary>
/// Finds what each category would delete, file by file, applying the safety policy to every candidate.
/// Discovery is read-only; the executor later works from exactly this list.
/// </summary>
public sealed class CleanupDiscovery
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

    private readonly KnownLocationSizer _sizer;

    private readonly DockerCli _docker;

    private readonly ScanResults _scans;

    private readonly ILogger<CleanupDiscovery> _logger;

    private readonly CleanupPolicyOptions _policy = CleanupPolicyFactory.Create();

    public CleanupDiscovery(KnownLocationSizer sizer, DockerCli docker, ScanResults scans, ILogger<CleanupDiscovery> logger)
    {
        _sizer = sizer;
        _docker = docker;
        _scans = scans;
        _logger = logger;
    }

    public CleanupPolicyOptions Policy => _policy;

    public Task<IReadOnlyList<CategoryDiscovery>> DiscoverAsync(AppSettings settings, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Task.Run<IReadOnlyList<CategoryDiscovery>>(() => Discover(settings, progress, cancellationToken), cancellationToken);
    }

    private List<CategoryDiscovery> Discover(AppSettings settings, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var running = RunningProcessNames();
        var results = new List<CategoryDiscovery>();
        var now = DateTime.UtcNow;

        foreach (var location in KnownLocations.All.Where(location => location.IsCleanable))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(location.DisplayName);
            results.Add(DiscoverLocation(location, settings, running, now, cancellationToken));
        }

        progress?.Report(Resources.Strings.Cleanup_RecycleBin);
        results.Add(DiscoverRecycleBin());
        progress?.Report(Resources.Strings.Cleanup_Docker);
        results.Add(DiscoverDocker());
        results.Add(DiscoverDeveloperFolders(settings));
        return results;
    }

    private CategoryDiscovery DiscoverLocation(KnownLocation location, AppSettings settings, HashSet<string> running, DateTime now, CancellationToken cancellationToken)
    {
        var minimumAge = location.UsesTemporaryFileAge ? TimeSpan.FromHours(settings.Cleanup.TemporaryFileMinimumAgeHours)
            : string.Equals(location.Id, "old-installers", StringComparison.Ordinal) ? TimeSpan.FromDays(settings.Cleanup.OldDownloadDays)
            : TimeSpan.Zero;
        var items = new List<CleanupItem>();
        var unavailable = false;

        foreach (var root in _sizer.Expand(location))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(root))
            {
                continue;
            }

            try
            {
                foreach (var candidate in Candidates(root, location.FilePatterns))
                {
                    var decision = CleanupSafetyPolicy.Evaluate(candidate.Path, root, candidate.Attributes, candidate.LastWriteUtc, now, minimumAge, _policy);

                    if (decision.Allowed)
                    {
                        items.Add(new CleanupItem(location.Id, candidate.Path, candidate.Length, new DateTimeOffset(candidate.LastWriteUtc, TimeSpan.Zero), CleanupItemKind.File, root));
                    }
                    else if (decision.Rejection is PolicyRejection.ProtectedLocation or PolicyRejection.OutsideRoot or PolicyRejection.UnsupportedPath)
                    {
                        _logger.LogDebug("{Path} skipped: {Rejection}", candidate.Path, decision.Rejection);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                unavailable = true;
            }
            catch (IOException exception)
            {
                _logger.LogDebug(exception, "{Root} could not be listed.", root);
            }
        }

        var conflicts = (location.ConflictingProcesses ?? [])
            .Where(process => running.Contains(Path.GetFileNameWithoutExtension(process)))
            .ToList();

        return new CategoryDiscovery(
            location.Id,
            location.Group,
            location.DisplayName,
            location.Description,
            location.Risk,
            location.RequiresElevation,
            location.DefaultSelected,
            AllOrNothing: false,
            items,
            items.Sum(item => item.Size),
            conflicts,
            location.Note,
            Unavailable: unavailable && items.Count == 0 && location.RequiresElevation && !ProcessContext.IsElevated);
    }

    private static FileSystemEnumerable<Candidate> Candidates(string root, IReadOnlyList<string>? patterns)
    {
        var enumerable = new FileSystemEnumerable<Candidate>(
            root,
            (ref FileSystemEntry entry) => new Candidate(entry.ToFullPath(), entry.Attributes, entry.Length, entry.LastWriteTimeUtc.UtcDateTime),
            patterns is null ? Recursive : TopLevel)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && (patterns is null || Matches(entry.FileName, patterns)),
            ShouldRecursePredicate = (ref FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0,
        };

        return enumerable;
    }

    private static bool Matches(ReadOnlySpan<char> name, IReadOnlyList<string> patterns)
    {
        var text = name.ToString();
        return patterns.Any(pattern => Wildcard.IsMatch(text, pattern));
    }

    private static CategoryDiscovery DiscoverRecycleBin()
    {
        var items = new List<CleanupItem>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType == DriveType.Fixed && drive.IsReady)
            {
                items.AddRange(RecycleBinContents.Read(drive.RootDirectory.FullName));
            }
        }

        return new CategoryDiscovery(
            CleanupCategoryIds.RecycleBin,
            LocationGroup.Temporary,
            Resources.Strings.Cleanup_RecycleBin,
            Resources.Strings.Cleanup_RecycleBinDescription,
            CleanupRisk.Caution,
            RequiresElevation: false,
            DefaultSelected: false,
            AllOrNothing: true,
            items,
            items.Sum(item => item.Size),
            [],
            null);
    }

    private CategoryDiscovery DiscoverDocker()
    {
        var docker = DockerCli.Locate();
        var items = new List<CleanupItem>();
        string? note = null;

        if (docker is null)
        {
            note = Resources.Strings.Cleanup_DockerNotInstalled;
        }
        else if (_docker.Usage(docker) is not { } usage)
        {
            note = Resources.Strings.Cleanup_DockerNotRunning;
        }
        else
        {
            foreach (var (type, reclaimable) in usage)
            {
                var command = type switch
                {
                    "Images" => "docker image prune -f",
                    "Containers" => "docker container prune -f",
                    "Build Cache" => "docker builder prune -f",
                    _ => null,
                };

                if (command is not null && reclaimable > 0)
                {
                    items.Add(new CleanupItem(CleanupCategoryIds.Docker, command, reclaimable, DateTimeOffset.Now, CleanupItemKind.Command, docker, type));
                }
            }
        }

        return new CategoryDiscovery(
            CleanupCategoryIds.Docker,
            LocationGroup.Docker,
            Resources.Strings.Cleanup_Docker,
            Resources.Strings.Cleanup_DockerDescription,
            CleanupRisk.Caution,
            RequiresElevation: false,
            DefaultSelected: false,
            AllOrNothing: false,
            items,
            items.Sum(item => item.Size),
            [],
            note,
            Unavailable: docker is null);
    }

    private CategoryDiscovery DiscoverDeveloperFolders(AppSettings settings)
    {
        var items = new List<CleanupItem>();
        var staleAfter = TimeSpan.FromDays(settings.Cleanup.StaleArtifactDays);
        var now = DateTimeOffset.UtcNow;

        if (_scans.Latest is { } scan)
        {
            foreach (var artifact in scan.Artifacts)
            {
                if (!artifact.Rule.IsCandidateForCleanup || artifact.ProjectNewestWriteTicks <= 0)
                {
                    continue;
                }

                var changed = new DateTimeOffset(artifact.ProjectNewestWriteTicks, TimeSpan.Zero);

                if (now - changed > staleAfter)
                {
                    items.Add(new CleanupItem(CleanupCategoryIds.DeveloperFolders, scan.Tree.Path(artifact.Node), scan.Tree.TotalBytes(artifact.Node), changed, CleanupItemKind.DeveloperFolder, null, artifact.Rule.DisplayName));
                }
            }
        }

        return new CategoryDiscovery(
            CleanupCategoryIds.DeveloperFolders,
            LocationGroup.Ides,
            Resources.Strings.Cleanup_DeveloperFolders,
            Resources.Strings.Cleanup_DeveloperFoldersDescription,
            CleanupRisk.Advanced,
            RequiresElevation: false,
            DefaultSelected: false,
            AllOrNothing: false,
            items,
            items.Sum(item => item.Size),
            [],
            _scans.Latest is null ? Resources.Strings.Cleanup_DeveloperFoldersNeedScan : null);
    }

    private static HashSet<string> RunningProcessNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                names.Add(process.ProcessName);
            }
        }

        return names;
    }

    private readonly record struct Candidate(string Path, FileAttributes Attributes, long Length, DateTime LastWriteUtc);
}
