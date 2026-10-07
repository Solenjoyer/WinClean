using WinClean.Core.Applications;

namespace WinClean.Core.Tests.Applications;

/// <summary>Builds process trees for grouping tests; start times increase in the order processes are added.</summary>
internal sealed class SnapshotBuilder
{
    private readonly List<ProcessFacts> _processes = [];

    private long _clock = 1000;

    public SnapshotBuilder Add(
        int pid,
        int parentPid,
        string name,
        string? path = null,
        string? commandLine = null,
        string? description = null,
        int sessionId = 1,
        bool systemAccount = false,
        long? createTime = null)
    {
        _processes.Add(new ProcessFacts(pid, createTime ?? _clock++, parentPid, name, path, commandLine, description, sessionId, systemAccount));
        return this;
    }

    public IReadOnlyList<ProcessFacts> Build() => _processes;

    public GroupingResult Group() => ApplicationGrouper.Group(_processes);
}
