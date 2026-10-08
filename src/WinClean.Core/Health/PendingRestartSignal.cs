namespace WinClean.Core.Health;

/// <summary>Each place Windows records that a restart is still owed, reported by name rather than summed up.</summary>
public enum PendingRestartSignal
{
    WindowsUpdate,
    ComponentServicing,
    FileRenames,
    ComputerRename,
    UpdateInProgress,
}
