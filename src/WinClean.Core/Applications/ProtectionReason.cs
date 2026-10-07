namespace WinClean.Core.Applications;

public enum ProtectionReason
{
    None,
    Self,
    KernelProcess,
    VirtualMachineMemory,
    Critical,
    WindowsComponent,
    SystemAccount,
}
