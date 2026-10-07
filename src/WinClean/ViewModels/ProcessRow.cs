using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Applications;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Resources;
using WinClean.Services.Processes;

namespace WinClean.ViewModels;

/// <summary>
/// One line of the process table, either an application group or a process. Rows live as long as what
/// they show and are updated in place, so the table never rebuilds and selection survives every tick.
/// </summary>
public sealed partial class ProcessRow : ObservableObject
{
    private const double RateFloor = 1024;

    public ProcessRow(string key, bool isGroup)
    {
        Key = key;
        IsGroup = isGroup;
        Name = string.Empty;
        Badge = string.Empty;
        StatusText = string.Empty;
        PidText = string.Empty;
        CpuText = string.Empty;
        MemoryText = string.Empty;
        IoText = string.Empty;
        GpuText = string.Empty;
        AutomationName = string.Empty;
    }

    public string Key { get; }

    public bool IsGroup { get; }

    public ProcessGroup? Group { get; private set; }

    public ProcessSample? Sample { get; private set; }

    public IReadOnlyList<ProcessSample> Members { get; private set; } = [];

    public int Pid { get; private set; }

    public double Cpu { get; private set; }

    public long Memory { get; private set; }

    public double Io { get; private set; }

    public double Gpu { get; private set; }

    public string SortName => Name;

    [ObservableProperty]
    public partial string Name { get; private set; }

    [ObservableProperty]
    public partial ImageSource? Icon { get; private set; }

    [ObservableProperty]
    public partial string Badge { get; private set; }

    [ObservableProperty]
    public partial bool IsChild { get; private set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; private set; }

    [ObservableProperty]
    public partial RowStatus Status { get; private set; }

    [ObservableProperty]
    public partial string StatusText { get; private set; }

    [ObservableProperty]
    public partial string PidText { get; private set; }

    [ObservableProperty]
    public partial string CpuText { get; private set; }

    [ObservableProperty]
    public partial string MemoryText { get; private set; }

    [ObservableProperty]
    public partial string IoText { get; private set; }

    [ObservableProperty]
    public partial string GpuText { get; private set; }

    [ObservableProperty]
    public partial string? Path { get; private set; }

    [ObservableProperty]
    public partial bool IsSuspended { get; private set; }

    [ObservableProperty]
    public partial string AutomationName { get; private set; }

    public void UpdateProcess(ProcessSample sample, ProcessDetails? details, string displayName, string badge, bool isChild)
    {
        ArgumentNullException.ThrowIfNull(sample);

        Sample = sample;
        Group = null;
        Members = [];
        Pid = sample.Pid;
        Cpu = sample.CpuPercent;
        Memory = sample.PrivateWorkingSet;
        Io = sample.IoBytesPerSecond;
        Gpu = sample.GpuPercent;

        Name = displayName;
        Icon = details?.Icon;
        Badge = badge;
        IsChild = isChild;
        Path = details?.Path;
        IsSuspended = sample.IsSuspended;
        Status = sample.IsSuspended ? RowStatus.Suspended : sample.IsNotResponding ? RowStatus.NotResponding : RowStatus.None;
        StatusText = Status switch
        {
            RowStatus.Suspended => Strings.Processes_StatusSuspended,
            RowStatus.NotResponding => Strings.Processes_StatusNotResponding,
            _ => string.Empty,
        };
        PidText = sample.Pid.ToString(CultureInfo.CurrentCulture);
        UpdateNumbers();
    }

    public void UpdateGroup(ProcessGroup group, IReadOnlyList<ProcessSample> members, ProcessDetails? rootDetails, bool isExpanded)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(members);

        Group = group;
        Members = members;
        Sample = null;
        Pid = 0;
        Cpu = 0;
        Memory = 0;
        Io = 0;
        Gpu = 0;

        foreach (var member in members)
        {
            Cpu += member.CpuPercent;
            Memory += member.PrivateWorkingSet;
            Io += member.IoBytesPerSecond;
            Gpu = Math.Max(Gpu, member.GpuPercent);
        }

        Cpu = Math.Min(Cpu, 100);
        Name = group.DisplayName;
        Icon = rootDetails?.Icon;
        Badge = string.Format(CultureInfo.CurrentCulture, Strings.Processes_MemberCount, members.Count);
        IsChild = false;
        IsExpanded = isExpanded;
        Path = rootDetails?.Path;
        IsSuspended = false;
        Status = RowStatus.None;
        StatusText = string.Empty;
        PidText = string.Empty;
        UpdateNumbers();
    }

    private void UpdateNumbers()
    {
        CpuText = Percent.Format(Cpu, 1);
        MemoryText = ByteSize.Format(Memory);
        IoText = Io >= RateFloor ? Rate.Format(Io) : string.Empty;
        GpuText = Gpu >= 0.5 ? Percent.Format(Gpu, 0) : string.Empty;
        AutomationName = string.Format(CultureInfo.CurrentCulture, Strings.Processes_RowAutomation, Name, MemoryText, CpuText);
    }
}
