namespace WinClean.Core.Health;

/// <summary>Registry facts the application reads; the evaluator turns them into signals.</summary>
public sealed record PendingRestartFacts(
    bool WindowsUpdateRebootRequired,
    bool ComponentServicingRebootPending,
    bool PendingFileRenames,
    bool ComputerNameChanged,
    bool UpdateExeVolatile);
