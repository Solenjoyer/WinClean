using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using WinClean.Core.Cleanup;
using WinClean.Core.Storage;
using WinClean.Native;
using SearchOption = System.IO.SearchOption;

namespace WinClean.Services.Cleanup;

/// <summary>
/// Deletes exactly the previewed items, one file at a time, re-checking each against the policy
/// first. Nothing is ever deleted recursively except a detected developer folder, and the roots of
/// the categories are never removed.
/// </summary>
public sealed class CleanupExecutor
{
    private const int ErrorSharingViolation = unchecked((int)0x80070020);

    private const int ErrorLockViolation = unchecked((int)0x80070021);

    private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(30);

    private readonly CleanupDiscovery _discovery;

    private readonly DockerCli _docker;

    private readonly CleanupLog _log;

    private readonly ILogger<CleanupExecutor> _logger;

    public CleanupExecutor(CleanupDiscovery discovery, DockerCli docker, CleanupLog log, ILogger<CleanupExecutor> logger)
    {
        _discovery = discovery;
        _docker = docker;
        _log = log;
        _logger = logger;
    }

    public Task<CleanupRun> ExecuteAsync(IReadOnlyList<CleanupItem> items, IReadOnlySet<string> recycleCategories, IProgress<CleanupProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(recycleCategories);
        return Task.Run(() => Execute(items, recycleCategories, progress, cancellationToken), CancellationToken.None);
    }

    private CleanupRun Execute(IReadOnlyList<CleanupItem> items, IReadOnlySet<string> recycleCategories, IProgress<CleanupProgress>? progress, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var results = new List<CleanupItemResult>(items.Count);
        var emptiedBins = new Dictionary<string, CleanupItemResult?>(StringComparer.OrdinalIgnoreCase);
        var touchedDirectories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < items.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(new CleanupItemResult(items[index], CleanupOutcome.Skipped, "cancelled"));
                continue;
            }

            var item = items[index];
            progress?.Report(new CleanupProgress(index, items.Count, item.Path));

            var result = item.Kind switch
            {
                CleanupItemKind.File => DeleteFile(item, recycleCategories.Contains(item.CategoryId), touchedDirectories),
                CleanupItemKind.RecycleBinEntry => EmptyRecycleBin(item, emptiedBins),
                CleanupItemKind.DeveloperFolder => DeleteDeveloperFolder(item),
                CleanupItemKind.Command => RunCommand(item),
                _ => new CleanupItemResult(item, CleanupOutcome.Skipped, "unknown kind"),
            };

            results.Add(result);
        }

        RetryWithUpdateServiceStopped(results, touchedDirectories);
        RemoveEmptyDirectories(touchedDirectories);
        progress?.Report(new CleanupProgress(items.Count, items.Count, string.Empty));

        var duration = Stopwatch.GetElapsedTime(started);
        string? logPath = null;

        try
        {
            logPath = _log.Write(results, duration);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "The cleanup log could not be written.");
        }

        _logger.LogInformation("Cleanup removed {Removed} of {Total} items, {Bytes} bytes.", results.Count(result => result.Removed), results.Count, results.Where(result => result.Removed).Sum(result => result.Item.Size));
        return new CleanupRun(results, logPath, duration);
    }

    private CleanupItemResult DeleteFile(CleanupItem item, bool recycle, Dictionary<string, HashSet<string>> touchedDirectories)
    {
        if (item.Root is null)
        {
            return new CleanupItemResult(item, CleanupOutcome.Skipped, "no root");
        }

        FileAttributes attributes;
        DateTime lastWrite;

        try
        {
            attributes = File.GetAttributes(item.Path);
            lastWrite = File.GetLastWriteTimeUtc(item.Path);
        }
        catch (FileNotFoundException)
        {
            return new CleanupItemResult(item, CleanupOutcome.Gone);
        }
        catch (DirectoryNotFoundException)
        {
            return new CleanupItemResult(item, CleanupOutcome.Gone);
        }
        catch (UnauthorizedAccessException)
        {
            return new CleanupItemResult(item, CleanupOutcome.AccessDenied);
        }
        catch (IOException exception)
        {
            return new CleanupItemResult(item, CleanupOutcome.Failed, exception.Message);
        }

        // Attributes are re-read right before the delete: a file that became read-only or a link since the preview stays.
        var decision = CleanupSafetyPolicy.Evaluate(item.Path, item.Root, attributes, lastWrite, DateTime.UtcNow, TimeSpan.Zero, _discovery.Policy);

        if (!decision.Allowed)
        {
            return new CleanupItemResult(item, CleanupOutcome.Skipped, decision.Rejection.ToString());
        }

        try
        {
            if (recycle)
            {
                FileSystem.DeleteFile(item.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
            }
            else
            {
                File.Delete(item.Path);
            }
        }
        catch (FileNotFoundException)
        {
            return new CleanupItemResult(item, CleanupOutcome.Gone);
        }
        catch (UnauthorizedAccessException)
        {
            return new CleanupItemResult(item, CleanupOutcome.AccessDenied);
        }
        catch (IOException exception) when (exception.HResult is ErrorSharingViolation or ErrorLockViolation)
        {
            return new CleanupItemResult(item, CleanupOutcome.InUse);
        }
        catch (IOException exception)
        {
            return new CleanupItemResult(item, CleanupOutcome.Failed, exception.Message);
        }
        catch (OperationCanceledException)
        {
            return new CleanupItemResult(item, CleanupOutcome.Skipped, "cancelled by the shell");
        }

        var directory = Path.GetDirectoryName(item.Path);

        if (directory is not null)
        {
            if (!touchedDirectories.TryGetValue(item.Root, out var directories))
            {
                directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                touchedDirectories[item.Root] = directories;
            }

            directories.Add(directory);
        }

        return new CleanupItemResult(item, recycle ? CleanupOutcome.Recycled : CleanupOutcome.Deleted);
    }

    private static CleanupItemResult EmptyRecycleBin(CleanupItem item, Dictionary<string, CleanupItemResult?> emptied)
    {
        var root = item.Root ?? string.Empty;

        if (!emptied.TryGetValue(root, out var outcome))
        {
            var status = Shell32.SHEmptyRecycleBinW(0, root, Shell32.SHERB_NOCONFIRMATION | Shell32.SHERB_NOPROGRESSUI | Shell32.SHERB_NOSOUND);
            outcome = status >= 0 ? null : new CleanupItemResult(item, CleanupOutcome.Failed, $"SHEmptyRecycleBin returned 0x{status:X8}");
            emptied[root] = outcome;
        }

        return outcome is null ? new CleanupItemResult(item, CleanupOutcome.Deleted) : outcome with { Item = item };
    }

    private static CleanupItemResult DeleteDeveloperFolder(CleanupItem item)
    {
        DirectoryInfo directory;

        try
        {
            directory = new DirectoryInfo(item.Path);

            if (!directory.Exists)
            {
                return new CleanupItemResult(item, CleanupOutcome.Gone);
            }

            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return new CleanupItemResult(item, CleanupOutcome.Skipped, "link");
            }

            // The project must still be there and still mark this folder as its artifact.
            var parent = directory.Parent;

            if (parent is null)
            {
                return new CleanupItemResult(item, CleanupOutcome.Skipped, "no parent");
            }

            var siblings = parent.EnumerateFileSystemInfos().Select(entry => entry.Name).ToList();
            var contents = directory.EnumerateFileSystemInfos().Select(entry => entry.Name).ToList();

            if (DeveloperArtifactRules.Match(directory.Name, siblings, contents, inProfileRoot: false) is not { IsCandidateForCleanup: true })
            {
                return new CleanupItemResult(item, CleanupOutcome.Skipped, "no longer recognised");
            }

            directory.Delete(recursive: true);
            return new CleanupItemResult(item, CleanupOutcome.Deleted);
        }
        catch (UnauthorizedAccessException)
        {
            return new CleanupItemResult(item, CleanupOutcome.AccessDenied);
        }
        catch (IOException exception) when (exception.HResult is ErrorSharingViolation or ErrorLockViolation)
        {
            return new CleanupItemResult(item, CleanupOutcome.InUse);
        }
        catch (IOException exception)
        {
            return new CleanupItemResult(item, CleanupOutcome.Failed, exception.Message);
        }
    }

    private CleanupItemResult RunCommand(CleanupItem item)
    {
        if (item.Root is null)
        {
            return new CleanupItemResult(item, CleanupOutcome.Skipped, "no docker.exe");
        }

        // The item path is the full command line as shown to the user; the executable comes first.
        var arguments = item.Path.StartsWith("docker ", StringComparison.Ordinal) ? item.Path[7..] : item.Path;
        return _docker.Prune(item.Root, arguments, out var output)
            ? new CleanupItemResult(item, CleanupOutcome.Deleted, output.Trim().Split('\n').LastOrDefault()?.Trim())
            : new CleanupItemResult(item, CleanupOutcome.Failed, "docker reported an error");
    }

    /// <summary>Windows Update holds some of its downloads open; with administrator rights the service is paused for the retry.</summary>
    private void RetryWithUpdateServiceStopped(List<CleanupItemResult> results, Dictionary<string, HashSet<string>> touchedDirectories)
    {
        var held = results
            .Select((result, index) => (result, index))
            .Where(pair => pair.result.Outcome == CleanupOutcome.InUse && string.Equals(pair.result.Item.CategoryId, "windows-update-cache", StringComparison.Ordinal))
            .ToList();

        if (held.Count == 0 || !ProcessContext.IsElevated)
        {
            return;
        }

        try
        {
            using var service = new ServiceController("wuauserv");
            var wasRunning = service.Status == ServiceControllerStatus.Running;

            if (wasRunning)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTimeout);
            }

            try
            {
                foreach (var (result, index) in held)
                {
                    results[index] = DeleteFile(result.Item, false, touchedDirectories);
                }
            }
            finally
            {
                if (wasRunning)
                {
                    service.Start();
                }
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or System.ServiceProcess.TimeoutException)
        {
            _logger.LogWarning(exception, "The Windows Update service could not be paused for the retry.");
        }
    }

    /// <summary>Folders left empty by the deletions go too, one level at a time and never the category root.</summary>
    private static void RemoveEmptyDirectories(Dictionary<string, HashSet<string>> touchedDirectories)
    {
        foreach (var (root, directories) in touchedDirectories)
        {
            foreach (var directory in directories.OrderByDescending(path => path.Length))
            {
                var current = directory;

                while (current is not null && !string.Equals(current.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) && CleanupSafetyPolicy.IsUnder(current, root))
                {
                    try
                    {
                        if (Directory.EnumerateFileSystemEntries(current, "*", SearchOption.TopDirectoryOnly).Any())
                        {
                            break;
                        }

                        Directory.Delete(current, recursive: false);
                    }
                    catch (IOException)
                    {
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        break;
                    }

                    current = Path.GetDirectoryName(current);
                }
            }
        }
    }
}
