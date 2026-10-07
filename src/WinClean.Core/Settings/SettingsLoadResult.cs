namespace WinClean.Core.Settings;

/// <summary>The settings that will be used, and the reason defaults were substituted when the file could not be read.</summary>
public sealed record SettingsLoadResult(AppSettings Settings, string? Error);
