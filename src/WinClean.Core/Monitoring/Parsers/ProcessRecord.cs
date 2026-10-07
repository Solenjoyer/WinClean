namespace WinClean.Core.Monitoring.Parsers;

/// <summary>
/// One SYSTEM_PROCESS_INFORMATION entry. Times are in 100-nanosecond units, sizes in bytes; the image
/// name is left in the buffer and read on demand because most processes are already known by the time
/// the next snapshot arrives.
/// </summary>
public readonly record struct ProcessRecord(
    int Pid,
    int ParentPid,
    long CreateTime,
    long KernelTime,
    long UserTime,
    long PrivateWorkingSet,
    long WorkingSet,
    long Commit,
    int Threads,
    int SuspendedThreads,
    int Handles,
    int SessionId,
    int BasePriority,
    long ReadTransfer,
    long WriteTransfer,
    long OtherTransfer,
    int NameOffset,
    int NameLength)
{
    public long CpuTime => KernelTime + UserTime;

    public long IoTransfer => ReadTransfer + WriteTransfer + OtherTransfer;

    /// <summary>Every thread is parked in a suspend wait, which is what a suspended process looks like from outside.</summary>
    public bool IsSuspended => Threads > 0 && SuspendedThreads == Threads;
}
