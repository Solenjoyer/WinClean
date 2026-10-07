namespace WinClean.Dialogs;

/// <summary>What the confirmation before ending a process says. A warning adds a checkbox the user must tick.</summary>
public sealed record EndProcessPrompt(string Question, string Message, string? Warning, string ConfirmLabel)
{
    public bool RequiresAcknowledgement => Warning is not null;
}
