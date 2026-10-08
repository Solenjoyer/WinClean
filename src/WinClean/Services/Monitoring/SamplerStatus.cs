namespace WinClean.Services.Monitoring;

/// <summary>
/// Whether a reader works on this machine and, if not, why. Shown on the Settings page and in
/// --self-check. A reader the user switched off is not available either, but that is a choice,
/// not a failure, so the self-check does not count it as degraded.
/// </summary>
public sealed record SamplerStatus(string Name, bool Available, string? Reason, bool Disabled = false);
