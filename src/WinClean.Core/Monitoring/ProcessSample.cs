using WinClean.Core.Applications;

namespace WinClean.Core.Monitoring;

/// <summary>One process in one tick: the facts that identify it and the numbers that change.</summary>
public sealed record ProcessSample(
    ProcessFacts Facts,
    double CpuPercent,
    long CpuTime,
    long PrivateWorkingSet,
    long WorkingSet,
    long Commit,
    int Threads,
    int Handles,
    double IoBytesPerSecond,
    double GpuPercent,
    long GpuMemory,
    bool IsSuspended,
    bool HasWindow,
    bool IsNotResponding)
{
    public int Pid => Facts.Pid;

    public ProcessIdentity Identity => Facts.Identity;

    public string Name => Facts.Name;
}
