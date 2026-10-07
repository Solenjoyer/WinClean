namespace WinClean.Core.Monitoring;

/// <summary>A process id together with its start time: ids are reused, the pair is not.</summary>
public readonly record struct ProcessIdentity(int Pid, long CreateTime);
