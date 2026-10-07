namespace WinClean.Services.Processes;

public sealed record ActionResult(ActionOutcome Outcome, string? Error = null)
{
    public static ActionResult Succeeded { get; } = new(ActionOutcome.Succeeded);

    public static ActionResult Gone { get; } = new(ActionOutcome.Gone);

    public bool Ok => Outcome == ActionOutcome.Succeeded;
}
