namespace WinClean.Core.Health;

/// <summary>One row of Microsoft's release-health data: a Windows version and when its servicing ends.</summary>
public sealed record WindowsRelease(
    string Product,
    string Version,
    int Build,
    bool LongTermServicing,
    DateOnly Released,
    DateOnly? ConsumerEnd,
    DateOnly? EnterpriseEnd,
    string? Note = null);
