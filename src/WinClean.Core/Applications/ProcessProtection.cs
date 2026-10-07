namespace WinClean.Core.Applications;

/// <summary>
/// Decides which process actions are safe. Kernel-adjacent processes are never touched; Windows
/// components and anything running as a system account need an explicit confirmation; suspending is
/// only offered for ordinary applications.
/// </summary>
public static class ProcessProtection
{
    private static readonly string[] PseudoProcesses = ["System", "Registry", "Memory Compression", "Secure System", "Idle"];

    private static readonly string[] VirtualMachineMemory = ["vmmem", "vmmem.exe", "vmmemWSL"];

    private static readonly string[] KernelAdjacent =
        ["smss.exe", "csrss.exe", "wininit.exe", "winlogon.exe", "services.exe", "lsass.exe", "LsaIso.exe", "fontdrvhost.exe"];

    private static readonly string[] AlwaysProtected = ["MsMpEng.exe"];

    private static readonly string[] Components =
    [
        "svchost.exe", "dwm.exe", "explorer.exe", "sihost.exe", "taskhostw.exe", "ctfmon.exe", "audiodg.exe", "conhost.exe",
        "spoolsv.exe", "SearchHost.exe", "StartMenuExperienceHost.exe", "ShellExperienceHost.exe", "RuntimeBroker.exe", "TextInputHost.exe",
    ];

    /// <summary>
    /// The verdict for one action. When the Windows directory is given, name-based rules only apply to
    /// executables under it; a file with the same name elsewhere is just a program.
    /// </summary>
    public static ProtectionVerdict Evaluate(ProcessFacts facts, ProcessAction action, int ownPid, string? windowsDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (facts.Pid == ownPid)
        {
            return new ProtectionVerdict(ProtectionLevel.Blocked, ProtectionReason.Self);
        }

        if (facts.Pid is 0 or 4 || IsNamed(facts, PseudoProcesses))
        {
            return new ProtectionVerdict(ProtectionLevel.Blocked, ProtectionReason.KernelProcess);
        }

        if (IsNamed(facts, VirtualMachineMemory))
        {
            return new ProtectionVerdict(ProtectionLevel.Blocked, ProtectionReason.VirtualMachineMemory);
        }

        if (facts.IsCritical)
        {
            return new ProtectionVerdict(ProtectionLevel.Blocked, ProtectionReason.Critical);
        }

        var trustedLocation = facts.Path is null
            || windowsDirectory is null
            || facts.Path.StartsWith(windowsDirectory, StringComparison.OrdinalIgnoreCase);

        if (IsNamed(facts, AlwaysProtected) || (trustedLocation && IsNamed(facts, KernelAdjacent)))
        {
            return new ProtectionVerdict(ProtectionLevel.Blocked, ProtectionReason.KernelProcess);
        }

        var component = trustedLocation && IsNamed(facts, Components);
        var pausing = action is ProcessAction.Suspend or ProcessAction.Resume;

        if (component)
        {
            return new ProtectionVerdict(pausing ? ProtectionLevel.Blocked : ProtectionLevel.Confirm, ProtectionReason.WindowsComponent);
        }

        if (facts.IsSystemAccount)
        {
            return new ProtectionVerdict(pausing ? ProtectionLevel.Blocked : ProtectionLevel.Confirm, ProtectionReason.SystemAccount);
        }

        return ProtectionVerdict.Allowed;
    }

    private static bool IsNamed(ProcessFacts facts, string[] names)
    {
        foreach (var name in names)
        {
            if (string.Equals(facts.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
