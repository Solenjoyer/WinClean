namespace WinClean.Services.Processes;

public enum ActionOutcome
{
    Succeeded,

    AccessDenied,

    /// <summary>The process had already exited, or its id now belongs to another process.</summary>
    Gone,

    Failed,
}
