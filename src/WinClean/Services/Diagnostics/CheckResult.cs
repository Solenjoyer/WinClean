namespace WinClean.Services.Diagnostics;

internal readonly record struct CheckResult(CheckStatus Status, string Message)
{
    public static CheckResult Ok(string message) => new(CheckStatus.Ok, message);

    public static CheckResult Degraded(string message) => new(CheckStatus.Degraded, message);

    public static CheckResult Failed(string message) => new(CheckStatus.Failed, message);
}
