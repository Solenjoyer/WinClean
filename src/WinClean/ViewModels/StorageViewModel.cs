using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Core.Storage;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Monitoring;
using WinClean.Services.Shell;
using WinClean.Services.Storage;

namespace WinClean.ViewModels;

/// <summary>Drives, one scan at a time, and the views over its result. Everything here is read-only.</summary>
public sealed partial class StorageViewModel : ObservableObject, IDisposable
{
    private readonly StorageScanner _scanner;

    private readonly KnownLocationSizer _sizer;

    private readonly ShellLinks _links;

    private readonly IClipboard _clipboard;

    private readonly SettingsStore _settings;

    private readonly ILogger<StorageViewModel> _logger;

    private CancellationTokenSource? _scan;

    private CancellationTokenSource? _measure;

    private ScanResult? _result;

    private bool _applicationsLoaded;

    public StorageViewModel(
        MonitoringScheduler scheduler,
        StorageScanner scanner,
        KnownLocationSizer sizer,
        ShellLinks links,
        IClipboard clipboard,
        SettingsStore settings,
        ILogger<StorageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(scheduler);

        _scanner = scanner;
        _sizer = sizer;
        _links = links;
        _clipboard = clipboard;
        _settings = settings;
        _logger = logger;

        ScanStatus = string.Empty;
        CurrentPath = string.Empty;
        FilesHere = string.Empty;
        MeasureStatus = string.Empty;

        scheduler.SampleReady += OnSample;
    }

    public ObservableCollection<DriveItem> Drives { get; } = [];

    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = [];

    public ObservableCollection<FolderItem> Folders { get; } = [];

    public ObservableCollection<FileItem> LargestFiles { get; } = [];

    public ObservableCollection<CategoryItem> Categories { get; } = [];

    public ObservableCollection<ApplicationItem> Applications { get; } = [];

    public ObservableCollection<ArtifactItem> Artifacts { get; } = [];

    public ObservableCollection<LocationItem> Locations { get; } = [];

    [ObservableProperty]
    public partial bool IsScanning { get; private set; }

    [ObservableProperty]
    public partial string ScanStatus { get; private set; }

    [ObservableProperty]
    public partial string CurrentPath { get; private set; }

    [ObservableProperty]
    public partial bool HasResult { get; private set; }

    [ObservableProperty]
    public partial string? ResultNote { get; private set; }

    [ObservableProperty]
    public partial string FilesHere { get; private set; }

    [ObservableProperty]
    public partial bool CanGoUp { get; private set; }

    [ObservableProperty]
    public partial bool IsMeasuring { get; private set; }

    [ObservableProperty]
    public partial string MeasureStatus { get; private set; }

    [ObservableProperty]
    public partial bool HasApplications { get; private set; }

    [ObservableProperty]
    public partial bool HasArtifacts { get; private set; }

    /// <summary>Called by the page when it is shown; the installed applications are read once.</summary>
    public async Task ActivateAsync()
    {
        if (_applicationsLoaded)
        {
            return;
        }

        _applicationsLoaded = true;

        try
        {
            var applications = await Task.Run(InstalledApplicationsReader.Read);
            PresentApplications(applications);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger.LogWarning(exception, "The installed applications could not be read.");
        }
    }

    public Task ScanPathAsync(string path, bool fastMedia) => ScanAsync(path, fastMedia);

    public void Dispose()
    {
        _scan?.Cancel();
        _scan?.Dispose();
        _measure?.Cancel();
        _measure?.Dispose();
    }

    [RelayCommand]
    private Task ScanDrive(DriveItem? drive) => drive is null ? Task.CompletedTask : ScanAsync(drive.Root, drive.IsFast);

    [RelayCommand]
    private void CancelScan() => _scan?.Cancel();

    [RelayCommand]
    private void Enter(FolderItem? folder)
    {
        if (folder is { CanEnter: true } && _result is not null)
        {
            ShowFolder(folder.Node);
        }
    }

    [RelayCommand]
    private void GoUp()
    {
        if (_result is not null && Breadcrumbs.Count > 1)
        {
            ShowFolder(Breadcrumbs[^2].Node);
        }
    }

    [RelayCommand]
    private void NavigateTo(BreadcrumbItem? crumb)
    {
        if (crumb is not null && _result is not null)
        {
            ShowFolder(crumb.Node);
        }
    }

    [RelayCommand]
    private void OpenLocation(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            _links.Reveal(path);
        }
    }

    [RelayCommand]
    private void CopyPath(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            _clipboard.SetText(path);
        }
    }

    [RelayCommand]
    private void OpenInstalledApps() => _links.OpenUrl(new Uri("ms-settings:appsfeatures"));

    [RelayCommand]
    private async Task MeasureLocationsAsync()
    {
        _measure?.Cancel();
        _measure = new CancellationTokenSource();
        IsMeasuring = true;
        var progress = new Progress<string>(name => MeasureStatus = string.Format(CultureInfo.CurrentCulture, Strings.Storage_Measuring, name));

        try
        {
            var measurements = await _sizer.MeasureAllAsync(progress, _measure.Token);
            PresentLocations(measurements);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsMeasuring = false;
            MeasureStatus = string.Empty;
        }
    }

    private async Task ScanAsync(string path, bool fastMedia)
    {
        _scan?.Cancel();
        _scan = new CancellationTokenSource();
        var token = _scan.Token;

        IsScanning = true;
        HasResult = false;
        ResultNote = null;
        ScanStatus = string.Format(CultureInfo.CurrentCulture, Strings.Storage_Scanning, path);
        CurrentPath = path;
        var progress = new Progress<ScanProgress>(ReportProgress);

        try
        {
            var result = await _scanner.ScanAsync(path, StorageScanner.WorkersFor(fastMedia), progress, token);

            if (!token.IsCancellationRequested || result.Tree.Count > 1)
            {
                Present(result);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Scanning {Path} failed.", path);
            ResultNote = string.Format(CultureInfo.CurrentCulture, Strings.Storage_ScanFailed, exception.Message);
        }
        finally
        {
            if (_scan.Token == token)
            {
                IsScanning = false;
            }
        }
    }

    private void ReportProgress(ScanProgress progress)
    {
        var culture = CultureInfo.CurrentCulture;
        ScanStatus = string.Format(culture, Strings.Storage_ScanCounts, progress.Folders.ToString("N0", culture), progress.Files.ToString("N0", culture), ByteSize.Format(progress.Bytes));
        CurrentPath = progress.CurrentPath;
    }

    private void Present(ScanResult result)
    {
        _result = result;
        var culture = CultureInfo.CurrentCulture;

        ResultNote = result.Cancelled
            ? string.Format(culture, Strings.Storage_ScanCancelled, result.TotalFiles.ToString("N0", culture))
            : result.DeniedFolders > 0
                ? string.Format(culture, Strings.Storage_ScanDenied, result.DeniedFolders.ToString("N0", culture))
                : null;

        ShowFolder(ScanResult.Root);

        LargestFiles.Clear();

        foreach (var file in result.LargestFiles)
        {
            LargestFiles.Add(new FileItem(
                Path.GetFileName(file.Path),
                file.Path,
                Path.GetDirectoryName(file.Path) ?? string.Empty,
                ByteSize.Format(file.Size),
                Durations.FormatRelative(new DateTimeOffset(file.LastWriteUtc, TimeSpan.Zero), DateTimeOffset.UtcNow)));
        }

        Categories.Clear();
        var total = Math.Max(1, result.TotalBytes);

        foreach (var category in result.Categories)
        {
            Categories.Add(new CategoryItem(
                LocationGroupNames.ForCategory(category.Category),
                ByteSize.Format(category.Bytes),
                string.Format(culture, Strings.Storage_FileCount, category.Files.ToString("N0", culture)),
                100.0 * category.Bytes / total));
        }

        Artifacts.Clear();
        var staleAfter = TimeSpan.FromDays(_settings.Current.Cleanup.StaleArtifactDays);
        var now = DateTimeOffset.UtcNow;

        foreach (var artifact in result.Artifacts)
        {
            var path = result.Tree.Path(artifact.Node);
            var changed = artifact.ProjectNewestWriteTicks > 0 ? new DateTimeOffset(artifact.ProjectNewestWriteTicks, TimeSpan.Zero) : (DateTimeOffset?)null;

            Artifacts.Add(new ArtifactItem(
                path,
                result.Tree.Name(artifact.Node),
                artifact.Rule.DisplayName,
                artifact.Rule.Description,
                ByteSize.Format(result.Tree.TotalBytes(artifact.Node)),
                changed is null ? Strings.NotAvailable : Durations.FormatRelative(changed.Value, now),
                changed is not null && now - changed.Value > staleAfter && artifact.Rule.IsCandidateForCleanup));
        }

        HasArtifacts = Artifacts.Count > 0;
        HasResult = true;

        if (_applicationsLoaded)
        {
            RefreshApplicationSizes();
        }
    }

    private void ShowFolder(int node)
    {
        if (_result is null)
        {
            return;
        }

        var tree = _result.Tree;
        var culture = CultureInfo.CurrentCulture;
        var crumbs = new Stack<BreadcrumbItem>();

        for (var current = node; current >= 0; current = tree.Parent(current))
        {
            crumbs.Push(new BreadcrumbItem(current, tree.Name(current)));
        }

        Breadcrumbs.Clear();

        foreach (var crumb in crumbs)
        {
            Breadcrumbs.Add(crumb);
        }

        CanGoUp = Breadcrumbs.Count > 1;
        FilesHere = string.Format(culture, Strings.Storage_FilesHere, tree.OwnFiles(node).ToString("N0", culture), ByteSize.Format(tree.OwnBytes(node)));

        var total = Math.Max(1, tree.TotalBytes(node));
        Folders.Clear();

        foreach (var child in tree.ChildrenBySize(node))
        {
            var state = tree.State(child);
            var caption = (state & FolderState.AccessDenied) != 0 ? Strings.Storage_FolderDenied
                : (state & FolderState.Link) != 0 ? Strings.Storage_FolderLink
                : string.Format(culture, Strings.Storage_FolderCounts, tree.TotalFiles(child).ToString("N0", culture));

            if ((state & FolderState.DeveloperArtifact) != 0)
            {
                caption += ", " + Strings.Storage_FolderArtifact;
            }
            else if ((state & FolderState.CloudFolder) != 0)
            {
                caption += ", " + Strings.Storage_FolderCloud;
            }

            var bytes = tree.TotalBytes(child);
            Folders.Add(new FolderItem(child, tree.Name(child), tree.Path(child), bytes, ByteSize.Format(bytes), 100.0 * bytes / total, caption, (state & FolderState.Link) == 0));
        }
    }

    private void PresentApplications(IReadOnlyList<InstalledApplication> applications)
    {
        Applications.Clear();

        foreach (var application in applications)
        {
            Applications.Add(new ApplicationItem(
                application.Name,
                application.Publisher ?? string.Empty,
                application.Version ?? string.Empty,
                SizeText(application),
                application.InstallDate?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty,
                application.InstallLocation));
        }

        HasApplications = Applications.Count > 0;
    }

    private void RefreshApplicationSizes()
    {
        for (var index = 0; index < Applications.Count; index++)
        {
            var item = Applications[index];

            if (item.Location is not null && _result is { } result && result.Tree.FindByPath(item.Location) is var node && node >= 0)
            {
                Applications[index] = item with { SizeText = ByteSize.Format(result.Tree.TotalBytes(node)) };
            }
        }
    }

    private string SizeText(InstalledApplication application)
    {
        if (application.InstallLocation is not null && _result is { } result && result.Tree.FindByPath(application.InstallLocation) is var node && node >= 0)
        {
            return ByteSize.Format(result.Tree.TotalBytes(node));
        }

        return application.EstimatedBytes is { } estimated
            ? string.Format(CultureInfo.CurrentCulture, Strings.Storage_SizeEstimated, ByteSize.Format(estimated))
            : Strings.NotAvailable;
    }

    private void PresentLocations(IReadOnlyList<LocationMeasurement> measurements)
    {
        Locations.Clear();
        var culture = CultureInfo.CurrentCulture;

        foreach (var measurement in measurements.OrderBy(m => m.Location.Group).ThenByDescending(m => m.Bytes))
        {
            Locations.Add(new LocationItem(
                measurement.Location.DisplayName,
                LocationGroupNames.For(measurement.Location.Group),
                measurement.Location.Description,
                measurement.Location.Note,
                measurement.Exists ? ByteSize.Format(measurement.Bytes) : Strings.Storage_LocationMissing,
                measurement.Exists ? string.Format(culture, Strings.Storage_FileCount, measurement.Files.ToString("N0", culture)) : string.Empty,
                measurement.Exists,
                measurement.Paths));
        }
    }

    private void OnSample(object? sender, SystemSample sample)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var volume in sample.Volumes)
        {
            if (volume.Kind == VolumeKind.Unknown)
            {
                continue;
            }

            seen.Add(volume.Root);
            var existing = Drives.FirstOrDefault(drive => string.Equals(drive.Root, volume.Root, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                Drives.Add(new DriveItem(volume));
            }
            else
            {
                existing.Update(volume);
            }
        }

        for (var index = Drives.Count - 1; index >= 0; index--)
        {
            if (!seen.Contains(Drives[index].Root))
            {
                Drives.RemoveAt(index);
            }
        }
    }
}
