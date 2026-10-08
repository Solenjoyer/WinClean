using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinClean.Core.Applications;
using WinClean.Core.Monitoring;
using WinClean.Dialogs;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Monitoring;
using WinClean.Services.Processes;
using WinClean.Services.Shell;

namespace WinClean.ViewModels;

/// <summary>
/// The process table. Every tick is turned into the list of rows that should be on screen, existing
/// rows are updated in place and only the order changes; while the mouse is down or a menu is open
/// the order is frozen so nothing moves under the pointer.
/// </summary>
public sealed partial class ProcessesViewModel : ObservableObject
{
    private const int SearchDelayMilliseconds = 150;

    private readonly MonitoringScheduler _scheduler;

    private readonly ProcessDetailsCache _details;

    private readonly ProcessActions _actions;

    private readonly IDialogService _dialogs;

    private readonly IClipboard _clipboard;

    private readonly Dictionary<string, ProcessRow> _rows = new(StringComparer.Ordinal);

    private readonly HashSet<string> _expanded = new(StringComparer.Ordinal);

    private readonly string _windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";

    private readonly int _ownPid = Environment.ProcessId;

    private ProcessSnapshot? _snapshot;

    private CancellationTokenSource? _searchDelay;

    private bool _holdOrder;

    public ProcessesViewModel(
        MonitoringScheduler scheduler,
        ProcessDetailsCache details,
        ProcessActions actions,
        IDialogService dialogs,
        IClipboard clipboard,
        SettingsStore settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _scheduler = scheduler;
        _details = details;
        _actions = actions;
        _dialogs = dialogs;
        _clipboard = clipboard;

        SearchText = string.Empty;
        Summary = string.Empty;
        SuspendLabel = Strings.Processes_Suspend;
        GroupByApplication = settings.Current.GroupByApplication;
        ShowSystemProcesses = settings.Current.ShowSystemProcesses;
        SortColumn = ProcessColumn.Memory;
        SortDescending = true;

        scheduler.SampleReady += OnSample;
    }

    public ObservableCollection<ProcessRow> Rows { get; } = [];

    public ProcessDetailsViewModel Details { get; } = new();

    public string PauseLabel => IsPaused ? Strings.Processes_ResumeUpdates : Strings.Processes_Pause;

    [ObservableProperty]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial bool GroupByApplication { get; set; }

    [ObservableProperty]
    public partial bool ShowSystemProcesses { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PauseLabel))]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    public partial bool IsDetailsOpen { get; set; }

    [ObservableProperty]
    public partial ProcessRow? SelectedRow { get; set; }

    [ObservableProperty]
    public partial ProcessColumn SortColumn { get; private set; }

    [ObservableProperty]
    public partial bool SortDescending { get; private set; }

    [ObservableProperty]
    public partial string Summary { get; private set; }

    [ObservableProperty]
    public partial string? Notice { get; private set; }

    [ObservableProperty]
    public partial bool HasGpu { get; private set; }

    [ObservableProperty]
    public partial string SuspendLabel { get; private set; }

    /// <summary>Clicking the column that already sorts the table flips the direction.</summary>
    public void SortBy(ProcessColumn column)
    {
        if (column == SortColumn)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortColumn = column;
            SortDescending = column is not ProcessColumn.Name and not ProcessColumn.Status;
        }

        Rebuild();
    }

    /// <summary>The view holds the order while the mouse is down or a menu is open.</summary>
    public void HoldOrder(bool hold)
    {
        if (_holdOrder == hold)
        {
            return;
        }

        _holdOrder = hold;

        if (!hold)
        {
            Rebuild();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDelay?.Cancel();
        _searchDelay = new CancellationTokenSource();
        _ = RebuildAfterDelayAsync(_searchDelay.Token);
    }

    partial void OnGroupByApplicationChanged(bool value) => Rebuild();

    partial void OnShowSystemProcessesChanged(bool value) => Rebuild();

    partial void OnIsPausedChanged(bool value)
    {
        if (!value)
        {
            Rebuild();
        }
    }

    partial void OnSelectedRowChanged(ProcessRow? value) => UpdateSelection();

    private async Task RebuildAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SearchDelayMilliseconds, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Rebuild();
    }

    private void OnSample(object? sender, SystemSample sample)
    {
        if (sample.Processes is null)
        {
            return;
        }

        _snapshot = sample.Processes;
        HasGpu = sample.Gpus.Count > 0;

        if (!IsPaused)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        if (_snapshot is null)
        {
            return;
        }

        var snapshot = _snapshot;
        var filter = SearchText.Trim();
        var byPid = new Dictionary<int, ProcessSample>(snapshot.Processes.Count);

        foreach (var process in snapshot.Processes)
        {
            byPid[process.Pid] = process;
        }

        var entries = new List<Entry>();

        if (GroupByApplication)
        {
            BuildGrouped(snapshot, byPid, filter, entries);
        }
        else
        {
            BuildFlat(snapshot, byPid, filter, entries);
        }

        entries.Sort((left, right) => Compare(left.Row, right.Row));
        var desired = new List<ProcessRow>(Rows.Count + 8);
        var processes = 0;

        foreach (var entry in entries)
        {
            desired.Add(entry.Row);
            processes += entry.Row.IsGroup ? entry.Row.Members.Count : 1;

            if (entry.Children is { } children)
            {
                children.Sort(Compare);
                desired.AddRange(children);
            }
        }

        if (!_holdOrder)
        {
            ApplyOrder(desired);
        }

        Summary = GroupByApplication
            ? string.Format(CultureInfo.CurrentCulture, Strings.Processes_SummaryGrouped, processes, entries.Count)
            : string.Format(CultureInfo.CurrentCulture, Strings.Processes_Summary, processes);

        UpdateSelection();
    }

    private void BuildGrouped(ProcessSnapshot snapshot, Dictionary<int, ProcessSample> byPid, string filter, List<Entry> entries)
    {
        foreach (var group in snapshot.Grouping.Groups)
        {
            var members = new List<ProcessSample>(group.MemberPids.Count);

            foreach (var pid in group.MemberPids)
            {
                if (byPid.TryGetValue(pid, out var member))
                {
                    members.Add(member);
                }
            }

            if (members.Count == 0 || (!ShowSystemProcesses && IsSystem(group, members)))
            {
                continue;
            }

            var shown = members;

            if (filter.Length > 0)
            {
                var groupMatches = group.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase);
                var matching = members.Where(member => Matches(member, filter)).ToList();

                if (matching.Count == 0 && !groupMatches)
                {
                    continue;
                }

                if (!groupMatches)
                {
                    shown = matching;
                }
            }

            var known = group.Application is not null;

            if (members.Count == 1)
            {
                var row = ProcessRowFor(members[0], group.DisplayName, known ? CategoryNames.For(group.Category) : string.Empty, isChild: false);
                entries.Add(new Entry(row, null));
                continue;
            }

            // The grouper lists the root first, so its icon stands for the group.
            var expanded = filter.Length > 0 || _expanded.Contains(group.Key);
            var groupRow = GroupRowFor(group, members, _details.TryGet(members[0].Identity), expanded);
            List<ProcessRow>? children = null;

            if (expanded)
            {
                children = new List<ProcessRow>(shown.Count);

                foreach (var member in shown)
                {
                    children.Add(ProcessRowFor(member, member.Name, string.Empty, isChild: true));
                }
            }

            entries.Add(new Entry(groupRow, children));
        }
    }

    private void BuildFlat(ProcessSnapshot snapshot, Dictionary<int, ProcessSample> byPid, string filter, List<Entry> entries)
    {
        var hidden = new HashSet<int>();
        var applicationByPid = new Dictionary<int, string>();

        foreach (var group in snapshot.Grouping.Groups)
        {
            var members = group.MemberPids.Where(byPid.ContainsKey).Select(pid => byPid[pid]).ToList();

            if (!ShowSystemProcesses && IsSystem(group, members))
            {
                hidden.UnionWith(group.MemberPids);
            }

            if (group.Application is not null)
            {
                foreach (var pid in group.MemberPids)
                {
                    applicationByPid[pid] = group.DisplayName;
                }
            }
        }

        foreach (var process in snapshot.Processes)
        {
            if (hidden.Contains(process.Pid) || (filter.Length > 0 && !Matches(process, filter)))
            {
                continue;
            }

            var row = ProcessRowFor(process, process.Name, applicationByPid.GetValueOrDefault(process.Pid, string.Empty), isChild: false);
            entries.Add(new Entry(row, null));
        }
    }

    /// <summary>Windows itself and services stay out of the way unless asked for; anything with a window is never hidden.</summary>
    private static bool IsSystem(ProcessGroup group, List<ProcessSample> members)
    {
        if (members.Any(member => member.HasWindow))
        {
            return false;
        }

        if (group.Category == ApplicationCategory.Windows)
        {
            return true;
        }

        return group.Application is null && members.All(member => member.Facts.IsSystemAccount);
    }

    private static bool Matches(ProcessSample process, string filter)
    {
        return process.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || process.Pid.ToString(CultureInfo.InvariantCulture).StartsWith(filter, StringComparison.Ordinal)
            || (process.Facts.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (process.Facts.Path?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private ProcessRow ProcessRowFor(ProcessSample process, string displayName, string badge, bool isChild)
    {
        var key = $"p:{process.Pid}:{process.Facts.CreateTime}";

        if (!_rows.TryGetValue(key, out var row))
        {
            row = new ProcessRow(key, isGroup: false);
            _rows[key] = row;
        }

        row.UpdateProcess(process, _details.TryGet(process.Identity), displayName, badge, isChild);
        return row;
    }

    private ProcessRow GroupRowFor(ProcessGroup group, List<ProcessSample> members, ProcessDetails? rootDetails, bool expanded)
    {
        var key = "g:" + group.Key;

        if (!_rows.TryGetValue(key, out var row))
        {
            row = new ProcessRow(key, isGroup: true);
            _rows[key] = row;
        }

        row.UpdateGroup(group, members, rootDetails, expanded);
        return row;
    }

    private int Compare(ProcessRow left, ProcessRow right)
    {
        var result = SortColumn switch
        {
            ProcessColumn.Name => CompareNames(left, right),
            ProcessColumn.Status => string.CompareOrdinal(left.StatusText, right.StatusText),
            ProcessColumn.Pid => left.Pid.CompareTo(right.Pid),
            ProcessColumn.Cpu => left.Cpu.CompareTo(right.Cpu),
            ProcessColumn.Memory => left.Memory.CompareTo(right.Memory),
            ProcessColumn.Io => left.Io.CompareTo(right.Io),
            ProcessColumn.Gpu => left.Gpu.CompareTo(right.Gpu),
            _ => 0,
        };

        if (SortDescending)
        {
            result = -result;
        }

        if (result == 0)
        {
            result = CompareNames(left, right);
        }

        return result != 0 ? result : left.Pid.CompareTo(right.Pid);
    }

    [SuppressMessage("Globalization", "CA1309:Use ordinal string comparison", Justification = "Names are sorted the way the user reads them.")]
    private static int CompareNames(ProcessRow left, ProcessRow right)
    {
        return string.Compare(left.SortName, right.SortName, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>Moves rows into place instead of resetting the collection, so the list never flickers and keeps its selection.</summary>
    private void ApplyOrder(List<ProcessRow> desired)
    {
        for (var index = 0; index < desired.Count; index++)
        {
            var row = desired[index];

            if (index < Rows.Count && ReferenceEquals(Rows[index], row))
            {
                continue;
            }

            var current = -1;

            for (var candidate = index + 1; candidate < Rows.Count; candidate++)
            {
                if (ReferenceEquals(Rows[candidate], row))
                {
                    current = candidate;
                    break;
                }
            }

            if (current >= 0)
            {
                Rows.Move(current, index);
            }
            else
            {
                Rows.Insert(index, row);
            }
        }

        while (Rows.Count > desired.Count)
        {
            Rows.RemoveAt(Rows.Count - 1);
        }

        if (_rows.Count > desired.Count)
        {
            var keep = new HashSet<string>(desired.Select(row => row.Key), StringComparer.Ordinal);
            var stale = _rows.Keys.Where(key => !keep.Contains(key)).ToList();

            foreach (var key in stale)
            {
                _rows.Remove(key);
            }
        }
    }

    private void UpdateSelection()
    {
        var row = SelectedRow;

        if (row is null || _snapshot is null)
        {
            Details.Clear();
            SuspendLabel = Strings.Processes_Suspend;
        }
        else if (row.Sample is { } sample)
        {
            var end = Verdict(sample.Facts, ProcessAction.Terminate);
            var suspend = Verdict(sample.Facts, sample.IsSuspended ? ProcessAction.Resume : ProcessAction.Suspend);
            Details.Show(row, _snapshot, _details.TryGet(sample.Identity), end, suspend, ProtectionText(end) ?? ProtectionText(suspend));
            SuspendLabel = Details.SuspendLabel;
        }
        else
        {
            var end = Worst(row.Members, ProcessAction.Terminate);
            var blocked = new ProtectionVerdict(ProtectionLevel.Blocked, ProtectionReason.None);
            Details.Show(row, _snapshot, null, end, blocked, ProtectionText(end));
            SuspendLabel = Strings.Processes_Suspend;
        }

        EndProcessCommand.NotifyCanExecuteChanged();
        EndProcessTreeCommand.NotifyCanExecuteChanged();
        SuspendOrResumeCommand.NotifyCanExecuteChanged();
        OpenLocationCommand.NotifyCanExecuteChanged();
        PropertiesCommand.NotifyCanExecuteChanged();
        CopyPathCommand.NotifyCanExecuteChanged();
        CopyRowCommand.NotifyCanExecuteChanged();
    }

    private ProtectionVerdict Verdict(ProcessFacts facts, ProcessAction action)
    {
        return ProcessProtection.Evaluate(facts, action, _ownPid, _windowsDirectory);
    }

    private ProtectionVerdict Worst(IReadOnlyList<ProcessSample> processes, ProcessAction action)
    {
        var worst = ProtectionVerdict.Allowed;

        foreach (var process in processes)
        {
            var verdict = Verdict(process.Facts, action);

            if (verdict.Level > worst.Level)
            {
                worst = verdict;
            }
        }

        return worst;
    }

    private static string? ProtectionText(ProtectionVerdict verdict) => verdict.Reason switch
    {
        ProtectionReason.Self => Strings.Processes_ProtectedSelf,
        ProtectionReason.KernelProcess => Strings.Processes_ProtectedKernel,
        ProtectionReason.VirtualMachineMemory => Strings.Processes_ProtectedVm,
        ProtectionReason.Critical => Strings.Processes_ProtectedCritical,
        ProtectionReason.WindowsComponent => Strings.Processes_ComponentWarning,
        ProtectionReason.SystemAccount => Strings.Processes_SystemAccountWarning,
        _ => null,
    };

    private static string? WarningFor(ProtectionVerdict verdict) => verdict.Level == ProtectionLevel.Confirm ? ProtectionText(verdict) : null;

    private bool CanEndSelected()
    {
        return SelectedRow is { } row && (row.Sample is { } sample
            ? !Verdict(sample.Facts, ProcessAction.Terminate).IsBlocked
            : row.Members.Count > 0 && !Worst(row.Members, ProcessAction.Terminate).IsBlocked);
    }

    private bool CanEndTreeSelected() => SelectedRow is { Sample: { } sample } && !Verdict(sample.Facts, ProcessAction.Terminate).IsBlocked;

    private bool CanSuspendSelected()
    {
        return SelectedRow is { Sample: { } sample } && !Verdict(sample.Facts, sample.IsSuspended ? ProcessAction.Resume : ProcessAction.Suspend).IsBlocked;
    }

    private bool HasSelectedPath() => SelectedRow?.Path is { Length: > 0 };

    private bool HasSelection() => SelectedRow is not null;

    [RelayCommand(CanExecute = nameof(CanEndSelected))]
    private void EndProcess()
    {
        if (SelectedRow is not { } row)
        {
            return;
        }

        var culture = CultureInfo.CurrentCulture;

        if (row.Sample is { } sample)
        {
            var verdict = Verdict(sample.Facts, ProcessAction.Terminate);
            var prompt = new EndProcessPrompt(
                string.Format(culture, Strings.Processes_EndTitle, sample.Name, sample.Pid),
                Strings.Processes_EndMessage,
                WarningFor(verdict),
                Strings.Processes_EndProcess);

            if (_dialogs.ConfirmEndProcess(prompt))
            {
                Report(sample.Name, _actions.Terminate(sample.Identity));
                _scheduler.Poke();
            }

            return;
        }

        var members = row.Members;
        var groupPrompt = new EndProcessPrompt(
            string.Format(culture, Strings.Processes_EndGroupTitle, row.Name, members.Count),
            Strings.Processes_EndManyMessage,
            WarningFor(Worst(members, ProcessAction.Terminate)),
            Strings.Processes_EndProcess);

        if (_dialogs.ConfirmEndProcess(groupPrompt))
        {
            var results = new List<ActionResult>(members.Count);

            foreach (var member in members)
            {
                results.Add(_actions.Terminate(member.Identity));
            }

            ReportMany(row.Name, results);
            _scheduler.Poke();
        }
    }

    [RelayCommand(CanExecute = nameof(CanEndTreeSelected))]
    private void EndProcessTree()
    {
        if (SelectedRow is not { Sample: { } sample } || _snapshot is null)
        {
            return;
        }

        var tree = ProcessActions.Descendants(sample, _snapshot.Processes)
            .Where(process => !Verdict(process.Facts, ProcessAction.Terminate).IsBlocked)
            .ToList();
        var culture = CultureInfo.CurrentCulture;
        var prompt = new EndProcessPrompt(
            tree.Count == 1
                ? string.Format(culture, Strings.Processes_EndTitle, sample.Name, sample.Pid)
                : string.Format(culture, Strings.Processes_EndTreeTitle, sample.Name, tree.Count - 1),
            tree.Count == 1 ? Strings.Processes_EndMessage : Strings.Processes_EndManyMessage,
            WarningFor(Worst(tree, ProcessAction.Terminate)),
            Strings.Processes_EndProcessTree);

        if (!_dialogs.ConfirmEndProcess(prompt))
        {
            return;
        }

        var results = _actions.TerminateTree(sample, tree);
        ReportMany(sample.Name, results.Select(result => result.Result).ToList());
        _scheduler.Poke();
    }

    [RelayCommand(CanExecute = nameof(CanSuspendSelected))]
    private void SuspendOrResume()
    {
        if (SelectedRow is not { Sample: { } sample })
        {
            return;
        }

        Report(sample.Name, sample.IsSuspended ? _actions.Resume(sample.Identity) : _actions.Suspend(sample.Identity));
        _scheduler.Poke();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPath))]
    private void OpenLocation()
    {
        if (SelectedRow?.Path is { Length: > 0 } path)
        {
            _actions.OpenLocation(path);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPath))]
    private void Properties()
    {
        if (SelectedRow?.Path is { Length: > 0 } path)
        {
            _actions.ShowProperties(path);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPath))]
    private void CopyPath()
    {
        if (SelectedRow?.Path is { Length: > 0 } path)
        {
            _clipboard.SetText(path);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void CopyRow()
    {
        if (SelectedRow is { } row)
        {
            _clipboard.SetText(string.Join('\t', row.Name, row.PidText, row.CpuText, row.MemoryText));
        }
    }

    [RelayCommand]
    private void ToggleExpand(ProcessRow? row)
    {
        if (row?.Group is not { } group)
        {
            return;
        }

        if (!_expanded.Remove(group.Key))
        {
            _expanded.Add(group.Key);
        }

        Rebuild();
    }

    [RelayCommand]
    private void ToggleDetails() => IsDetailsOpen = !IsDetailsOpen;

    [RelayCommand]
    private void TogglePause() => IsPaused = !IsPaused;

    [RelayCommand]
    private void Refresh() => _scheduler.Poke();

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private void DismissNotice() => Notice = null;

    private void Report(string name, ActionResult result)
    {
        var culture = CultureInfo.CurrentCulture;

        Notice = result.Outcome switch
        {
            ActionOutcome.Succeeded => null,
            ActionOutcome.AccessDenied => string.Format(culture, Strings.Processes_ActionAccessDenied, name),
            ActionOutcome.Gone => string.Format(culture, Strings.Processes_ActionGone, name),
            _ => string.Format(culture, Strings.Processes_ActionFailed, name, result.Error ?? string.Empty),
        };
    }

    private void ReportMany(string name, List<ActionResult> results)
    {
        var failures = results.Where(result => !result.Ok && result.Outcome != ActionOutcome.Gone).ToList();

        if (failures.Count == 0)
        {
            Notice = null;
        }
        else if (failures.Count == results.Count)
        {
            Report(name, failures[0]);
        }
        else
        {
            Notice = string.Format(CultureInfo.CurrentCulture, Strings.Processes_ActionPartial, failures.Count, results.Count);
        }
    }

    private sealed record Entry(ProcessRow Row, List<ProcessRow>? Children);
}
