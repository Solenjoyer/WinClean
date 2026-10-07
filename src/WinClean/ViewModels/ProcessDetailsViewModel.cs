using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Applications;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Resources;
using WinClean.Services.Processes;

namespace WinClean.ViewModels;

/// <summary>The pane next to the process table: everything known about the selected process, refreshed every tick.</summary>
public sealed partial class ProcessDetailsViewModel : ObservableObject
{
    private readonly FactItem _pid = new(Strings.Details_Pid);

    private readonly FactItem _parent = new(Strings.Details_Parent);

    private readonly FactItem _user = new(Strings.Details_User);

    private readonly FactItem _started = new(Strings.Details_Started);

    private readonly FactItem _architecture = new(Strings.Details_Architecture);

    private readonly FactItem _elevated = new(Strings.Details_Elevated);

    private readonly FactItem _session = new(Strings.Details_Session);

    private readonly FactItem _threads = new(Strings.Details_Threads);

    private readonly FactItem _handles = new(Strings.Details_Handles);

    private readonly FactItem _cpuTime = new(Strings.Details_CpuTime);

    private readonly FactItem _privateWorkingSet = new(Strings.Details_PrivateWorkingSet);

    private readonly FactItem _workingSet = new(Strings.Details_WorkingSet);

    private readonly FactItem _commit = new(Strings.Details_Commit);

    private readonly FactItem _gpuMemory = new(Strings.Details_GpuMemory);

    private readonly FactItem _io = new(Strings.Details_Io);

    public ProcessDetailsViewModel()
    {
        Facts =
        [
            _pid, _parent, _user, _started, _architecture, _elevated, _session, _threads, _handles,
            _cpuTime, _privateWorkingSet, _workingSet, _commit, _gpuMemory, _io,
        ];
        Name = string.Empty;
        Subtitle = string.Empty;
        SuspendLabel = Strings.Processes_Suspend;
    }

    public ObservableCollection<FactItem> Facts { get; }

    public ObservableCollection<string> WindowTitles { get; } = [];

    [ObservableProperty]
    public partial bool HasSelection { get; private set; }

    [ObservableProperty]
    public partial string Name { get; private set; }

    [ObservableProperty]
    public partial string Subtitle { get; private set; }

    [ObservableProperty]
    public partial ImageSource? Icon { get; private set; }

    [ObservableProperty]
    public partial string? Protection { get; private set; }

    [ObservableProperty]
    public partial string? CommandLine { get; private set; }

    [ObservableProperty]
    public partial string? Path { get; private set; }

    [ObservableProperty]
    public partial bool HasWindows { get; private set; }

    [ObservableProperty]
    public partial bool CanEnd { get; private set; }

    [ObservableProperty]
    public partial bool CanSuspend { get; private set; }

    [ObservableProperty]
    public partial string SuspendLabel { get; private set; }

    public void Clear()
    {
        HasSelection = false;
        Name = string.Empty;
        Subtitle = string.Empty;
        Icon = null;
        Protection = null;
        CommandLine = null;
        Path = null;
        WindowTitles.Clear();
        HasWindows = false;
        CanEnd = false;
        CanSuspend = false;
    }

    public void Show(ProcessRow row, ProcessSnapshot snapshot, ProcessDetails? details, ProtectionVerdict endVerdict, ProtectionVerdict suspendVerdict, string? protection)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(endVerdict);
        ArgumentNullException.ThrowIfNull(suspendVerdict);

        var culture = CultureInfo.CurrentCulture;
        HasSelection = true;
        Icon = row.Icon;
        Protection = protection;
        CanEnd = !endVerdict.IsBlocked;
        CanSuspend = !suspendVerdict.IsBlocked;

        if (row.Sample is not { } sample)
        {
            ShowGroup(row, culture);
            return;
        }

        var facts = sample.Facts;
        Name = facts.Description ?? facts.Name;
        Subtitle = facts.Description is null ? string.Empty : facts.Name;
        SuspendLabel = sample.IsSuspended ? Strings.Processes_Resume : Strings.Processes_Suspend;
        CommandLine = details is null || details.AccessDenied ? null : details.CommandLine;
        Path = details?.AccessDenied == true ? Strings.Details_AccessDenied : facts.Path;

        _pid.Value = sample.Pid.ToString(culture);
        _parent.Value = ParentText(facts, snapshot, culture);
        _user.Value = details?.UserName ?? Strings.NotAvailable;
        _started.Value = Durations.FormatRelative(DateTimeOffset.FromFileTime(facts.CreateTime), DateTimeOffset.Now);
        _architecture.Value = details?.Architecture ?? Strings.NotAvailable;
        _elevated.Value = details?.IsElevated switch { true => Strings.Details_Yes, false => Strings.Details_No, null => Strings.NotAvailable };
        _session.Value = facts.SessionId.ToString(culture);
        _threads.Value = sample.Threads.ToString("N0", culture);
        _handles.Value = sample.Handles.ToString("N0", culture);
        _cpuTime.Value = TimeSpan.FromTicks(sample.CpuTime).ToString(@"hh\:mm\:ss", culture);
        _privateWorkingSet.Value = ByteSize.Format(sample.PrivateWorkingSet);
        _workingSet.Value = ByteSize.Format(sample.WorkingSet);
        _commit.Value = ByteSize.Format(sample.Commit);
        _gpuMemory.Value = sample.GpuMemory > 0 ? ByteSize.Format(sample.GpuMemory) : "0 B";
        _io.Value = Rate.Format(sample.IoBytesPerSecond);

        foreach (var fact in Facts)
        {
            fact.IsVisible = true;
        }

        SyncTitles(snapshot.WindowTitles.TryGetValue(sample.Pid, out var titles) ? titles : []);
    }

    private void ShowGroup(ProcessRow row, CultureInfo culture)
    {
        Name = row.Name;
        Subtitle = row.Badge;
        SuspendLabel = Strings.Processes_Suspend;
        CommandLine = null;
        Path = row.Path;
        CanSuspend = false;

        var threads = 0;
        var handles = 0;
        var cpuTime = 0L;
        var workingSet = 0L;
        var commit = 0L;
        var gpuMemory = 0L;

        foreach (var member in row.Members)
        {
            threads += member.Threads;
            handles += member.Handles;
            cpuTime += member.CpuTime;
            workingSet += member.WorkingSet;
            commit += member.Commit;
            gpuMemory += member.GpuMemory;
        }

        _threads.Value = threads.ToString("N0", culture);
        _handles.Value = handles.ToString("N0", culture);
        _cpuTime.Value = TimeSpan.FromTicks(cpuTime).ToString(@"hh\:mm\:ss", culture);
        _privateWorkingSet.Value = ByteSize.Format(row.Memory);
        _workingSet.Value = ByteSize.Format(workingSet);
        _commit.Value = ByteSize.Format(commit);
        _gpuMemory.Value = gpuMemory > 0 ? ByteSize.Format(gpuMemory) : "0 B";
        _io.Value = Rate.Format(row.Io);

        foreach (var fact in Facts)
        {
            fact.IsVisible = fact != _pid && fact != _parent && fact != _user && fact != _started && fact != _architecture && fact != _elevated && fact != _session;
        }

        SyncTitles([]);
    }

    private static string ParentText(ProcessFacts facts, ProcessSnapshot snapshot, CultureInfo culture)
    {
        foreach (var process in snapshot.Processes)
        {
            if (process.Pid == facts.ParentPid && process.Facts.CreateTime <= facts.CreateTime)
            {
                return $"{process.Name} ({process.Pid.ToString(culture)})";
            }
        }

        return facts.ParentPid == 0 ? Strings.NotAvailable : facts.ParentPid.ToString(culture);
    }

    private void SyncTitles(IReadOnlyList<string> titles)
    {
        if (!WindowTitles.SequenceEqual(titles, StringComparer.Ordinal))
        {
            WindowTitles.Clear();

            foreach (var title in titles)
            {
                WindowTitles.Add(title);
            }
        }

        HasWindows = WindowTitles.Count > 0;
    }
}
