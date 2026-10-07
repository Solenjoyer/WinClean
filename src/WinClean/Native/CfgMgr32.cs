using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

[SupportedOSPlatform("windows")]
internal static partial class CfgMgr32
{
    internal const int CR_SUCCESS = 0;

    internal const int CR_BUFFER_SMALL = 0x1A;

    internal const uint CM_GETIDLIST_FILTER_PRESENT = 0x00000100;

    internal const uint CM_LOCATE_DEVNODE_NORMAL = 0;

    internal const uint DEVPROP_TYPE_UINT32 = 0x00000007;
    internal const uint DEVPROP_TYPE_GUID = 0x0000000D;
    internal const uint DEVPROP_TYPE_FILETIME = 0x00000010;
    internal const uint DEVPROP_TYPE_STRING = 0x00000012;
    internal const uint DEVPROP_TYPE_STRING_LIST = 0x00002012;

    internal static readonly DEVPROPKEY DEVPKEY_NAME = new(new Guid(0xb725f130, 0x47ef, 0x101a, 0xa5, 0xf2, 0x02, 0x60, 0x8c, 0x9e, 0xeb, 0xac), 10);
    internal static readonly DEVPROPKEY DEVPKEY_Device_HardwareIds = new(new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 3);
    internal static readonly DEVPROPKEY DEVPKEY_Device_Class = new(new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 9);
    internal static readonly DEVPROPKEY DEVPKEY_Device_Manufacturer = new(new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 13);
    internal static readonly DEVPROPKEY DEVPKEY_Device_DriverDate = new(new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0x0c, 0x75, 0xd6), 2);
    internal static readonly DEVPROPKEY DEVPKEY_Device_DriverVersion = new(new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0x0c, 0x75, 0xd6), 3);
    internal static readonly DEVPROPKEY DEVPKEY_Device_DriverDesc = new(new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0x0c, 0x75, 0xd6), 4);
    internal static readonly DEVPROPKEY DEVPKEY_Device_DriverInfPath = new(new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0x0c, 0x75, 0xd6), 5);
    internal static readonly DEVPROPKEY DEVPKEY_Device_DriverProvider = new(new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0x0c, 0x75, 0xd6), 9);
    internal static readonly DEVPROPKEY DEVPKEY_Device_ProblemCode = new(new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7), 3);

    [LibraryImport("cfgmgr32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int CM_Get_Device_ID_List_SizeW(out uint pulLen, string? pszFilter, uint ulFlags);

    [LibraryImport("cfgmgr32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int CM_Get_Device_ID_ListW(string? pszFilter, Span<char> Buffer, uint BufferLen, uint ulFlags);

    [LibraryImport("cfgmgr32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

    [LibraryImport("cfgmgr32.dll")]
    internal static partial int CM_Get_DevNode_PropertyW(uint dnDevInst, in DEVPROPKEY PropertyKey, out uint PropertyType, Span<byte> PropertyBuffer, ref uint PropertyBufferSize, uint ulFlags);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DEVPROPKEY(Guid fmtid, uint pid)
    {
        public readonly Guid fmtid = fmtid;
        public readonly uint pid = pid;
    }
}
