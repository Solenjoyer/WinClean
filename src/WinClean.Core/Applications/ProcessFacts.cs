using WinClean.Core.Monitoring;

namespace WinClean.Core.Applications;

/// <summary>What the grouper and the protection policy need to know about one process.</summary>
public sealed record ProcessFacts(
    int Pid,
    long CreateTime,
    int ParentPid,
    string Name,
    string? Path,
    string? CommandLine,
    string? Description,
    int SessionId,
    bool IsSystemAccount,
    bool IsCritical = false)
{
    public ProcessIdentity Identity => new(Pid, CreateTime);

    /// <summary>"Code.exe" becomes "Code"; pseudo-processes such as "System" are unchanged.</summary>
    public string Stem => Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? Name[..^4] : Name;
}
