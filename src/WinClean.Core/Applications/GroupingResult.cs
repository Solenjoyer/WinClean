namespace WinClean.Core.Applications;

public sealed record GroupingResult(IReadOnlyList<ProcessGroup> Groups, IReadOnlyDictionary<int, string> GroupKeyByPid);
