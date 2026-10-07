namespace WinClean.Core.Health;

public static class PendingRestartEvaluator
{
    public static IReadOnlyList<PendingRestartSignal> Evaluate(PendingRestartFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var signals = new List<PendingRestartSignal>();

        if (facts.WindowsUpdateRebootRequired)
        {
            signals.Add(PendingRestartSignal.WindowsUpdate);
        }

        if (facts.ComponentServicingRebootPending)
        {
            signals.Add(PendingRestartSignal.ComponentServicing);
        }

        if (facts.PendingFileRenames)
        {
            signals.Add(PendingRestartSignal.FileRenames);
        }

        if (facts.ComputerNameChanged)
        {
            signals.Add(PendingRestartSignal.ComputerRename);
        }

        if (facts.UpdateExeVolatile)
        {
            signals.Add(PendingRestartSignal.UpdateInProgress);
        }

        return signals;
    }

    /// <summary>File renames alone are a weak signal: many installers leave one behind without needing a restart.</summary>
    public static bool IsRestartPending(IReadOnlyList<PendingRestartSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);
        return signals.Any(signal => signal != PendingRestartSignal.FileRenames);
    }
}
