using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using Microsoft.Extensions.Logging;
using WinClean.Core.Storage;
using WinClean.Native;

namespace WinClean.Services.Storage;

/// <summary>
/// Walks a drive or folder with a few threads and no file opens: one work item per directory, files
/// aggregated into their folder as they are listed. Developer folders are recognised on the way, with
/// the parent's listing already in hand, and the walk never follows links or mount points.
/// </summary>
public sealed class StorageScanner
{
    private const int LargestFileCount = 2000;

    private const int MaximumWorkers = 8;

    private const int CloudOnlyAttributes = 0x00400000 | 0x00040000 | (int)FileAttributes.Offline;

    private static readonly EnumerationOptions ListingOptions = new()
    {
        RecurseSubdirectories = false,
        AttributesToSkip = 0,
        IgnoreInaccessible = false,
        ReturnSpecialDirectories = false,
        BufferSize = 64 * 1024,
    };

    private readonly ILogger<StorageScanner> _logger;

    public StorageScanner(ILogger<StorageScanner> logger)
    {
        _logger = logger;
    }

    public static int WorkersFor(bool fastMedia) => fastMedia ? Math.Min(Environment.ProcessorCount, MaximumWorkers) : 2;

    public Task<ScanResult> ScanAsync(string rootPath, int workers, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);
        return Task.Run(() => Scan(rootPath, Math.Max(1, workers), progress, cancellationToken), CancellationToken.None);
    }

    private ScanResult Scan(string rootPath, int workers, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var state = new ScanState(rootPath);
        var root = state.Tree.AddRoot(rootPath);
        state.Enqueue(new WorkItem(root, rootPath, null, 0, false, false));

        var threads = new List<Thread>(workers);

        for (var index = 0; index < workers; index++)
        {
            var thread = new Thread(() => Work(state, cancellationToken)) { Name = "WinClean.Scan", IsBackground = true };
            threads.Add(thread);
            thread.Start();
        }

        // The token stops the workers; this loop only reports until they are done.
        while (!state.Finished.Wait(100, CancellationToken.None))
        {
            progress?.Report(state.Progress());
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        progress?.Report(state.Progress());
        state.Tree.Aggregate();

        var result = new ScanResult(
            rootPath,
            state.Tree,
            state.Largest.ToList(),
            state.Categories.ToList(),
            state.Artifacts.OrderByDescending(artifact => state.Tree.TotalBytes(artifact.Node)).ToList(),
            Stopwatch.GetElapsedTime(started),
            cancellationToken.IsCancellationRequested);

        _logger.LogInformation(
            "Scanned {Path}: {Folders} folders, {Files} files, {Bytes} bytes in {Seconds:F1} s{Cancelled}",
            rootPath,
            state.Tree.Count,
            result.TotalFiles,
            result.TotalBytes,
            result.Duration.TotalSeconds,
            result.Cancelled ? " (cancelled)" : string.Empty);

        return result;
    }

    private void Work(ScanState state, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var item in state.Queue.GetConsumingEnumerable(cancellationToken))
            {
                try
                {
                    ListDirectory(state, item);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    _logger.LogWarning(exception, "Listing {Path} failed.", item.Path);
                }

                state.Done();
            }
        }
        catch (OperationCanceledException)
        {
            state.Stop();
        }
    }

    private static void ListDirectory(ScanState state, WorkItem item)
    {
        state.CurrentPath = item.Path;
        var entries = new List<RawEntry>();

        try
        {
            foreach (var entry in new FileSystemEnumerable<RawEntry>(item.Path, Transform, ListingOptions))
            {
                entries.Add(entry);
            }
        }
        catch (UnauthorizedAccessException)
        {
            state.Tree.Mark(item.Node, FolderState.AccessDenied);
            return;
        }
        catch (DirectoryNotFoundException)
        {
            // Gone since it was listed by its parent.
            return;
        }
        catch (IOException)
        {
            state.Tree.Mark(item.Node, FolderState.AccessDenied);
            return;
        }

        var names = new string[entries.Count];

        for (var index = 0; index < entries.Count; index++)
        {
            names[index] = entries[index].Name;
        }

        var insideArtifact = item.InsideArtifact;

        if (!insideArtifact && item.Siblings is not null)
        {
            var rule = DeveloperArtifactRules.Match(state.Tree.Name(item.Node), item.Siblings, names, item.InProfileRoot);

            if (rule is not null)
            {
                state.Tree.Mark(item.Node, FolderState.DeveloperArtifact);
                state.Artifacts.Add(new DeveloperArtifact(item.Node, rule, item.ProjectNewestWriteTicks));
                insideArtifact = true;
            }
        }

        // The two most recent entries: a child's project is "the parent without that child".
        var newest = 0L;
        var newestIndex = -1;
        var secondNewest = 0L;

        for (var index = 0; index < entries.Count; index++)
        {
            var ticks = entries[index].LastWriteTicks;

            if (ticks > newest)
            {
                secondNewest = newest;
                newest = ticks;
                newestIndex = index;
            }
            else if (ticks > secondNewest)
            {
                secondNewest = ticks;
            }
        }

        var childrenInProfileRoot = string.Equals(item.Path.TrimEnd('\\'), state.ProfileRoot, StringComparison.OrdinalIgnoreCase);

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];

            if (entry.IsDirectory)
            {
                var child = state.Tree.AddChild(item.Node, entry.Name);
                var childPath = Path.Join(item.Path, entry.Name);
                state.Folders++;

                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    if (!ReparsePoints.TryReadTag(childPath, out var tag) || !ReparsePoints.IsCloudFolder(tag))
                    {
                        state.Tree.Mark(child, FolderState.Link);
                        continue;
                    }

                    state.Tree.Mark(child, FolderState.CloudFolder);
                }

                var projectNewest = index == newestIndex ? secondNewest : newest;
                state.Enqueue(new WorkItem(child, childPath, names, projectNewest, insideArtifact, childrenInProfileRoot));
                continue;
            }

            var cloudOnly = ((int)entry.Attributes & CloudOnlyAttributes) != 0;
            var sizeOnDisk = cloudOnly ? 0 : SizeOnDisk(item.Path, entry);
            state.Tree.AddFile(item.Node, entry.Length, sizeOnDisk, cloudOnly, entry.LastWriteTicks);
            state.Categories.Add(FileTypeCategories.Categorize(entry.Name), entry.Length);
            state.CountFile(entry.Length);

            if (state.Largest.Accepts(entry.Length))
            {
                state.Largest.Offer(new FileEntry(Path.Join(item.Path, entry.Name), entry.Length, new DateTime(entry.LastWriteTicks, DateTimeKind.Utc)));
            }
        }
    }

    private static long SizeOnDisk(string directory, in RawEntry entry)
    {
        if ((entry.Attributes & (FileAttributes.Compressed | FileAttributes.SparseFile)) == 0)
        {
            return entry.Length;
        }

        var low = Kernel32.GetCompressedFileSizeW(Path.Join(directory, entry.Name), out var high);

        if (low == Kernel32.INVALID_FILE_SIZE && Kernel32.GetCompressedFileSizeW(Path.Join(directory, entry.Name), out _) == Kernel32.INVALID_FILE_SIZE)
        {
            return entry.Length;
        }

        return ((long)high << 32) | low;
    }

    private static RawEntry Transform(ref FileSystemEntry entry)
    {
        var isDirectory = entry.IsDirectory;
        return new RawEntry(entry.FileName.ToString(), entry.Attributes, isDirectory ? 0 : entry.Length, entry.LastWriteTimeUtc.UtcTicks, isDirectory);
    }

    private readonly record struct RawEntry(string Name, FileAttributes Attributes, long Length, long LastWriteTicks, bool IsDirectory);

    private sealed record WorkItem(int Node, string Path, string[]? Siblings, long ProjectNewestWriteTicks, bool InsideArtifact, bool InProfileRoot);

    private sealed class ScanState
    {
        private int _pending;

        private long _files;

        private long _bytes;

        public ScanState(string rootPath)
        {
            RootPath = rootPath;
            ProfileRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\');
            CurrentPath = rootPath;
        }

        public string RootPath { get; }

        public string ProfileRoot { get; }

        public ScanTree Tree { get; } = new();

        public LargestFiles Largest { get; } = new(LargestFileCount);

        public CategoryTotals Categories { get; } = new();

        public ConcurrentBag<DeveloperArtifact> Artifacts { get; } = [];

        public BlockingCollection<WorkItem> Queue { get; } = [];

        public ManualResetEventSlim Finished { get; } = new(false);

        public int Folders;

        public volatile string CurrentPath;

        public void Enqueue(WorkItem item)
        {
            Interlocked.Increment(ref _pending);

            if (Queue.IsAddingCompleted)
            {
                return;
            }

            try
            {
                Queue.Add(item);
            }
            catch (InvalidOperationException)
            {
                // Adding completed while cancelling.
            }
        }

        public void Done()
        {
            if (Interlocked.Decrement(ref _pending) == 0)
            {
                Stop();
            }
        }

        public void Stop()
        {
            if (!Queue.IsAddingCompleted)
            {
                try
                {
                    Queue.CompleteAdding();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            Finished.Set();
        }

        public void CountFile(long bytes)
        {
            Interlocked.Increment(ref _files);
            Interlocked.Add(ref _bytes, bytes);
        }

        public ScanProgress Progress() => new(Volatile.Read(ref Folders), Volatile.Read(ref _files), Volatile.Read(ref _bytes), CurrentPath);
    }
}
