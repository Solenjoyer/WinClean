using System.Runtime.InteropServices;
using WinClean.Native;

namespace WinClean.App.Tests.Native;

/// <summary>
/// The interop structs are written by hand against the Windows SDK headers; their sizes are the
/// cheapest check that a field was not left out or misaligned.
/// </summary>
public class StructLayoutTests
{
    [Fact]
    public void Kernel32Structs_HaveTheirSdkSizes()
    {
        Assert.Equal(64, Marshal.SizeOf<Kernel32.MEMORYSTATUSEX>());
        Assert.Equal(104, Marshal.SizeOf<Kernel32.PERFORMANCE_INFORMATION>());
        Assert.Equal(12, Marshal.SizeOf<Kernel32.SYSTEM_POWER_STATUS>());
        Assert.Equal(592, Marshal.SizeOf<Kernel32.WIN32_FIND_DATAW>());
    }

    [Fact]
    public void ShellStructs_HaveTheirSdkSizes()
    {
        Assert.Equal(976, Marshal.SizeOf<Shell32.NOTIFYICONDATAW>());
        Assert.Equal(112, Marshal.SizeOf<Shell32.SHELLEXECUTEINFOW>());
        Assert.Equal(696, Marshal.SizeOf<Shell32.SHFILEINFOW>());
        Assert.Equal(24, Marshal.SizeOf<Shell32.SHQUERYRBINFO>());
    }

    [Fact]
    public void OtherStructs_HaveTheirSdkSizes()
    {
        Assert.Equal(284, Marshal.SizeOf<NtDll.OSVERSIONINFOEXW>());
        Assert.Equal(840, Marshal.SizeOf<User32.DISPLAY_DEVICEW>());
        Assert.Equal(220, Marshal.SizeOf<User32.DEVMODEW>());
        Assert.Equal(16, Marshal.SizeOf<Tbs.TPM_DEVICE_INFO>());
        Assert.Equal(20, Marshal.SizeOf<CfgMgr32.DEVPROPKEY>());
        Assert.Equal(16, Marshal.SizeOf<Pdh.PDH_FMT_COUNTERVALUE>());
    }
}
