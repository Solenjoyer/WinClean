namespace WinClean.Services.Monitoring;

/// <summary>Whether a reader works on this machine and, if not, why. Shown on the Settings page and in --self-check.</summary>
public sealed record SamplerStatus(string Name, bool Available, string? Reason);
