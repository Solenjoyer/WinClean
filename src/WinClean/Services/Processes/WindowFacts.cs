namespace WinClean.Services.Processes;

/// <summary>The top-level windows one process owns, as far as the shell shows them.</summary>
public sealed class WindowFacts
{
    public bool IsHung { get; set; }

    public List<string> Titles { get; } = [];
}
