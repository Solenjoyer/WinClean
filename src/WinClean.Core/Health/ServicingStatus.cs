namespace WinClean.Core.Health;

public sealed record ServicingStatus(
    ServicingState State,
    WindowsRelease? Release,
    DateOnly? EndOfServicing,
    bool UsesEnterpriseDates);
