using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinClean.Core.Cleanup;
using WinClean.Core.Formatting;
using WinClean.Core.Storage;
using WinClean.Dialogs;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Cleanup;
using WinClean.Services.Shell;

namespace WinClean.ViewModels;

/// <summary>
/// Analyse, choose, preview, confirm, clean, report. The list the user confirms is the list that is
/// executed; nothing is rediscovered in between.
/// </summary>
public sealed partial class CleanupViewModel : ObservableObject, IDisposable
{
    private const int ConfirmationLines = 5;

    private readonly CleanupDiscovery _discovery;

    private readonly CleanupExecutor _executor;

    private readonly SettingsStore _settings;

    private readonly IDialogService _dialogs;

    private readonly ShellLinks _links;

    private readonly Elevation _elevation;

    private readonly ILogger<CleanupViewModel> _logger;

    private readonly List<CleanupCategoryItem> _categories = [];

    private CancellationTokenSource? _work;

    public CleanupViewModel(
        CleanupDiscovery discovery,
        CleanupExecutor executor,
        SettingsStore settings,
        IDialogService dialogs,
        ShellLinks links,
        Elevation elevation,
        ILogger<CleanupViewModel> logger)
    {
        _discovery = discovery;
        _executor = executor;
        _settings = settings;
        _dialogs = dialogs;
        _links = links;
        _elevation = elevation;
        _logger = logger;

        Status = string.Empty;
        Summary = Strings.Cleanup_NothingSelected;
        PreviewFilter = string.Empty;
        PreviewTotal = string.Empty;
        FinishedSummary = string.Empty;
        PreviewLabel = Strings.Cleanup_Preview;
    }

    public ObservableCollection<CleanupGroupItem> Groups { get; } = [];

    public ObservableCollection<CleanupPreviewRow> PreviewRows { get; } = [];

    public ObservableCollection<CleanupResultRow> NotRemoved { get; } = [];

    [ObservableProperty]
    public partial bool IsAnalyzed { get; private set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string Status { get; private set; }

    [ObservableProperty]
    public partial string Summary { get; private set; }

    [ObservableProperty]
    public partial bool CanClean { get; private set; }

    [ObservableProperty]
    public partial bool NeedsAdministrator { get; private set; }

    [ObservableProperty]
    public partial bool IsPreviewOpen { get; set; }

    [ObservableProperty]
    public partial string PreviewLabel { get; private set; }

    [ObservableProperty]
    public partial string PreviewFilter { get; set; }

    [ObservableProperty]
    public partial string PreviewTotal { get; private set; }

    [ObservableProperty]
    public partial bool IsFinished { get; private set; }

    [ObservableProperty]
    public partial string FinishedSummary { get; private set; }

    [ObservableProperty]
    public partial string? LogPath { get; private set; }

    public void Dispose()
    {
        _work?.Cancel();
        _work?.Dispose();
    }

    partial void OnIsPreviewOpenChanged(bool value)
    {
        PreviewLabel = value ? Strings.Cleanup_HidePreview : Strings.Cleanup_Preview;

        if (value)
        {
            RebuildPreview();
        }
    }

    partial void OnPreviewFilterChanged(string value)
    {
        if (IsPreviewOpen)
        {
            RebuildPreview();
        }
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        _work?.Cancel();
        _work = new CancellationTokenSource();
        IsBusy = true;
        IsFinished = false;
        var progress = new Progress<string>(name => Status = string.Format(CultureInfo.CurrentCulture, Strings.Cleanup_Analyzing, name));

        try
        {
            var discoveries = await _discovery.DiscoverAsync(_settings.Current, progress, _work.Token);
            Present(discoveries);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _logger.LogError(exception, "Cleanup analysis failed.");
            Status = exception.Message;
        }
        finally
        {
            IsBusy = false;
            Status = string.Empty;
        }
    }

    [RelayCommand]
    private void TogglePreview() => IsPreviewOpen = !IsPreviewOpen;

    [RelayCommand]
    private void RestartAsAdministrator() => _elevation.RestartElevated(PageKeys.Cleanup);

    [RelayCommand]
    private void OpenLog()
    {
        if (LogPath is not null)
        {
            _links.Reveal(LogPath);
        }
    }

    [RelayCommand]
    private Task DoneAsync() => AnalyzeAsync();

    [RelayCommand]
    private async Task CleanAsync()
    {
        var items = SelectedItems();

        if (items.Count == 0)
        {
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        var folders = items.Where(item => item.Kind == CleanupItemKind.DeveloperFolder).ToList();

        foreach (var folder in folders)
        {
            if (!_dialogs.ConfirmFolderDelete(System.IO.Path.GetFileName(folder.Path), folder.Path))
            {
                return;
            }
        }

        var files = items.Count(item => item.Kind == CleanupItemKind.File);
        var onlyFiles = files == items.Count;
        var count = items.Count.ToString("N0", culture);
        var bytes = ByteSize.Format(items.Sum(item => item.Size));
        var byCategory = _categories
            .Where(category => category.IsSelected)
            .Select(category => (category.Name, Bytes: items.Where(item => item.CategoryId == category.Id).Sum(item => item.Size)))
            .Where(pair => pair.Bytes > 0)
            .OrderByDescending(pair => pair.Bytes)
            .ToList();
        var lines = byCategory.Take(ConfirmationLines).Select(pair => $"{pair.Name}: {ByteSize.Format(pair.Bytes)}").ToList();

        if (byCategory.Count > ConfirmationLines)
        {
            lines.Add(string.Format(culture, Strings.Cleanup_ConfirmAndMore, byCategory.Count - ConfirmationLines));
        }

        var notices = _categories
            .Where(category => category.IsSelected && category.Discovery.RunningConflicts.Count > 0)
            .Select(category => string.Format(culture, Strings.Cleanup_Running, string.Join(", ", category.Discovery.RunningConflicts)))
            .ToList();

        if (items.Any(item => RecycleCategories.Contains(item.CategoryId)))
        {
            notices.Add(Strings.Cleanup_ConfirmRecycleNote);
        }

        if (folders.Count > 0)
        {
            notices.Add(string.Format(culture, Strings.Cleanup_ConfirmDeveloperNote, folders.Count));
        }

        var prompt = new CleanupPrompt(
            string.Format(culture, onlyFiles ? Strings.Cleanup_ConfirmTitle : Strings.Cleanup_ConfirmTitleItems, count),
            string.Format(culture, Strings.Cleanup_ConfirmMessage, bytes),
            lines,
            notices,
            string.Format(culture, onlyFiles ? Strings.Cleanup_ConfirmDelete : Strings.Cleanup_ConfirmRemove, count));

        if (!_dialogs.ConfirmCleanup(prompt))
        {
            return;
        }

        _work?.Cancel();
        _work = new CancellationTokenSource();
        IsBusy = true;
        IsPreviewOpen = false;
        var progress = new Progress<CleanupProgress>(p => Status = string.Format(culture, Strings.Cleanup_Deleting, p.Done.ToString("N0", culture), p.Total.ToString("N0", culture)));

        try
        {
            var run = await _executor.ExecuteAsync(items, RecycleCategories, progress, _work.Token);
            PresentRun(run);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _logger.LogError(exception, "Cleanup failed.");
            Status = exception.Message;
        }
        finally
        {
            IsBusy = false;
            Status = string.Empty;
        }
    }

    private static HashSet<string> RecycleCategories { get; } = KnownLocations.All
        .Where(location => location.RecycleInsteadOfDelete)
        .Select(location => location.Id)
        .ToHashSet(StringComparer.Ordinal);

    private void Present(IReadOnlyList<CategoryDiscovery> discoveries)
    {
        foreach (var category in _categories)
        {
            category.PropertyChanged -= OnCategoryChanged;
        }

        _categories.Clear();
        Groups.Clear();
        var elevated = Elevation.IsElevated;

        foreach (var group in discoveries.GroupBy(discovery => discovery.Group).OrderBy(group => group.Key))
        {
            var categories = group.Select(discovery => new CleanupCategoryItem(discovery, elevated)).ToList();

            foreach (var category in categories)
            {
                category.PropertyChanged += OnCategoryChanged;
            }

            _categories.AddRange(categories);
            Groups.Add(new CleanupGroupItem(LocationGroupNames.For(group.Key), categories));
        }

        NeedsAdministrator = !elevated && _categories.Any(category => category.RequiresElevation && (category.Discovery.Count > 0 || category.Discovery.Unavailable));
        IsAnalyzed = true;
        UpdateSummary();

        if (IsPreviewOpen)
        {
            RebuildPreview();
        }
    }

    private void OnCategoryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanupCategoryItem.IsSelected))
        {
            UpdateSummary();

            if (IsPreviewOpen)
            {
                RebuildPreview();
            }
        }
    }

    private void UpdateSummary()
    {
        var selected = _categories.Where(category => category.IsSelected).ToList();
        var items = SelectedItems();
        var culture = CultureInfo.CurrentCulture;

        Summary = items.Count == 0
            ? Strings.Cleanup_NothingSelected
            : string.Format(culture, Strings.Cleanup_Summary, selected.Count, items.Count.ToString("N0", culture), ByteSize.Format(items.Sum(item => item.Size)));
        CanClean = items.Count > 0 && !IsBusy;
    }

    private List<CleanupItem> SelectedItems()
    {
        var excluded = PreviewRows.Where(row => !row.IsIncluded).Select(row => row.Item).ToHashSet();

        return _categories
            .Where(category => category.IsSelected)
            .SelectMany(category => category.Discovery.Items)
            .Where(item => !excluded.Contains(item))
            .ToList();
    }

    private void RebuildPreview()
    {
        var excluded = PreviewRows.Where(row => !row.IsIncluded).Select(row => row.Item).ToHashSet();
        var filter = PreviewFilter.Trim();
        PreviewRows.Clear();

        foreach (var category in _categories.Where(category => category.IsSelected))
        {
            foreach (var item in category.Discovery.Items.OrderByDescending(item => item.Size))
            {
                if (filter.Length > 0 && !item.Path.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var row = new CleanupPreviewRow(item, category.Name, !category.Discovery.AllOrNothing) { IsIncluded = !excluded.Contains(item) };
                row.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(CleanupPreviewRow.IsIncluded))
                    {
                        UpdateSummary();
                        UpdatePreviewTotal();
                    }
                };
                PreviewRows.Add(row);
            }
        }

        UpdatePreviewTotal();
    }

    private void UpdatePreviewTotal()
    {
        var included = PreviewRows.Where(row => row.IsIncluded).ToList();
        PreviewTotal = string.Format(CultureInfo.CurrentCulture, Strings.Cleanup_PreviewTotal, included.Count.ToString("N0", CultureInfo.CurrentCulture), ByteSize.Format(included.Sum(row => row.Item.Size)));
    }

    private void PresentRun(CleanupRun run)
    {
        var culture = CultureInfo.CurrentCulture;
        FinishedSummary = string.Format(
            culture,
            Strings.Cleanup_FinishedSummary,
            ByteSize.Format(run.FreedBytes),
            run.Removed.ToString("N0", culture),
            run.InUse.ToString("N0", culture),
            run.AccessDenied.ToString("N0", culture));
        LogPath = run.LogPath;
        NotRemoved.Clear();

        foreach (var result in run.Results.Where(result => !result.Removed))
        {
            var reason = result.Outcome switch
            {
                CleanupOutcome.InUse => Strings.Cleanup_ReasonInUse,
                CleanupOutcome.AccessDenied => Strings.Cleanup_ReasonAccessDenied,
                CleanupOutcome.Gone => Strings.Cleanup_ReasonGone,
                CleanupOutcome.Skipped => Strings.Cleanup_ReasonSkipped,
                _ => Strings.Cleanup_ReasonFailed,
            };

            if (result.Detail is not null)
            {
                reason += " (" + result.Detail + ")";
            }

            NotRemoved.Add(new CleanupResultRow(System.IO.Path.GetFileName(result.Item.Path), result.Item.Path, ByteSize.Format(result.Item.Size), reason));
        }

        IsFinished = true;
        IsAnalyzed = false;
    }
}
