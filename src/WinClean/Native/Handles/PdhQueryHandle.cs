using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native.Handles;

/// <summary>A PDH query; closing it also frees every counter added to it.</summary>
[SupportedOSPlatform("windows")]
internal sealed class PdhQueryHandle : SafeHandle
{
    public PdhQueryHandle()
        : base(0, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == 0;

    protected override bool ReleaseHandle() => Pdh.PdhCloseQuery(handle) == Pdh.ERROR_SUCCESS;
}
