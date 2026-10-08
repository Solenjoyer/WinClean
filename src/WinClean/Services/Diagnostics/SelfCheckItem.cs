namespace WinClean.Services.Diagnostics;

internal sealed record SelfCheckItem(string Name, Func<CheckResult> Run);
