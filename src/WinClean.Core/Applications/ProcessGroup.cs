namespace WinClean.Core.Applications;

/// <summary>Processes that belong together on screen: an application and everything it spawned.</summary>
public sealed record ProcessGroup(
    string Key,
    string DisplayName,
    ApplicationCategory Category,
    KnownApplication? Application,
    IReadOnlyList<int> MemberPids)
{
    public string? Hint => Application?.Hint;
}
