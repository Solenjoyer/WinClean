namespace WinClean.Core.Health;

public sealed record UpdateHistoryEntry(string Title, DateTimeOffset Date, int ResultCode)
{
    /// <summary>Operation result codes of the Windows Update Agent: 2 succeeded, 3 succeeded with errors, 4 failed, 5 aborted.</summary>
    public bool Succeeded => ResultCode is 2 or 3;
}
