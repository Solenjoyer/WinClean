using WinClean.Core.Applications;

namespace WinClean.Core.Tests.Applications;

public class ProcessProtectionTests
{
    private const int OwnPid = 777;

    private const string WindowsDirectory = @"C:\Windows";

    [Theory]
    [InlineData(4, "System", null, ProtectionReason.KernelProcess)]
    [InlineData(0, "Idle", null, ProtectionReason.KernelProcess)]
    [InlineData(120, "Registry", null, ProtectionReason.KernelProcess)]
    [InlineData(600, "csrss.exe", null, ProtectionReason.KernelProcess)]
    [InlineData(600, "csrss.exe", @"C:\Windows\System32\csrss.exe", ProtectionReason.KernelProcess)]
    [InlineData(610, "lsass.exe", @"C:\Windows\System32\lsass.exe", ProtectionReason.KernelProcess)]
    [InlineData(2000, "vmmemWSL", null, ProtectionReason.VirtualMachineMemory)]
    [InlineData(2100, "MsMpEng.exe", @"C:\ProgramData\Microsoft\Windows Defender\Platform\4.18\MsMpEng.exe", ProtectionReason.KernelProcess)]
    [InlineData(OwnPid, "WinClean.exe", @"C:\Tools\WinClean.exe", ProtectionReason.Self)]
    public void Evaluate_BlocksProcessesWindowsDependsOn(int pid, string name, string? path, ProtectionReason reason)
    {
        var facts = new ProcessFacts(pid, 1, 0, name, path, null, null, 0, true);

        foreach (var action in Enum.GetValues<ProcessAction>())
        {
            var verdict = ProcessProtection.Evaluate(facts, action, OwnPid, WindowsDirectory);

            Assert.Equal(ProtectionLevel.Blocked, verdict.Level);
            Assert.Equal(reason, verdict.Reason);
        }
    }

    [Fact]
    public void Evaluate_CriticalFlag_BlocksRegardlessOfName()
    {
        var facts = new ProcessFacts(50, 1, 0, "custom-service.exe", @"C:\Tools\custom-service.exe", null, null, 0, false, IsCritical: true);

        var verdict = ProcessProtection.Evaluate(facts, ProcessAction.Terminate, OwnPid, WindowsDirectory);

        Assert.Equal(ProtectionLevel.Blocked, verdict.Level);
        Assert.Equal(ProtectionReason.Critical, verdict.Reason);
    }

    [Theory]
    [InlineData("svchost.exe", @"C:\Windows\System32\svchost.exe")]
    [InlineData("explorer.exe", @"C:\Windows\explorer.exe")]
    [InlineData("dwm.exe", null)]
    [InlineData("RuntimeBroker.exe", @"C:\Windows\System32\RuntimeBroker.exe")]
    public void Evaluate_WindowsComponents_NeedConfirmationAndCannotBePaused(string name, string? path)
    {
        var facts = new ProcessFacts(900, 1, 0, name, path, null, null, 1, false);

        var terminate = ProcessProtection.Evaluate(facts, ProcessAction.Terminate, OwnPid, WindowsDirectory);
        var suspend = ProcessProtection.Evaluate(facts, ProcessAction.Suspend, OwnPid, WindowsDirectory);

        Assert.Equal(ProtectionLevel.Confirm, terminate.Level);
        Assert.Equal(ProtectionReason.WindowsComponent, terminate.Reason);
        Assert.Equal(ProtectionLevel.Blocked, suspend.Level);
    }

    [Fact]
    public void Evaluate_SystemAccountProcesses_NeedConfirmation()
    {
        var facts = new ProcessFacts(1200, 1, 0, "SomeService.exe", @"C:\Program Files\Vendor\SomeService.exe", null, null, 0, true);

        var verdict = ProcessProtection.Evaluate(facts, ProcessAction.TerminateTree, OwnPid, WindowsDirectory);

        Assert.Equal(ProtectionLevel.Confirm, verdict.Level);
        Assert.Equal(ProtectionReason.SystemAccount, verdict.Reason);
        Assert.Equal(ProtectionLevel.Blocked, ProcessProtection.Evaluate(facts, ProcessAction.Resume, OwnPid, WindowsDirectory).Level);
    }

    [Theory]
    [InlineData("Code.exe", @"C:\Users\dev\AppData\Local\Programs\Microsoft VS Code\Code.exe")]
    [InlineData("chrome.exe", @"C:\Program Files\Google\Chrome\Application\chrome.exe")]
    [InlineData("csrss.exe", @"D:\Tools\csrss.exe")]
    public void Evaluate_OrdinaryApplications_AreAllowed(string name, string path)
    {
        var facts = new ProcessFacts(3000, 1, 0, name, path, null, null, 1, false);

        foreach (var action in Enum.GetValues<ProcessAction>())
        {
            Assert.Equal(ProtectionVerdict.Allowed, ProcessProtection.Evaluate(facts, action, OwnPid, WindowsDirectory));
        }
    }

    [Fact]
    public void Evaluate_WithoutAKnownWindowsDirectory_TrustsNamesAlone()
    {
        var facts = new ProcessFacts(3000, 1, 0, "csrss.exe", @"D:\Tools\csrss.exe", null, null, 1, false);

        Assert.Equal(ProtectionLevel.Blocked, ProcessProtection.Evaluate(facts, ProcessAction.Terminate, OwnPid).Level);
    }
}
