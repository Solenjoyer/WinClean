using System.Buffers.Binary;
using System.Text;
using WinClean.Core.Health;
using WinClean.Native;

namespace WinClean.Services.Health;

/// <summary>Every present device with its driver, read through the Configuration Manager like Device Manager does.</summary>
internal static class DriverInventory
{
    public static IReadOnlyList<DriverInfo> Read()
    {
        var drivers = new List<DriverInfo>();

        foreach (var deviceId in PresentDeviceIds())
        {
            if (CfgMgr32.CM_Locate_DevNodeW(out var node, deviceId, CfgMgr32.CM_LOCATE_DEVNODE_NORMAL) != CfgMgr32.CR_SUCCESS)
            {
                continue;
            }

            var provider = ReadString(node, CfgMgr32.DEVPKEY_Device_DriverProvider);
            var className = ReadString(node, CfgMgr32.DEVPKEY_Device_Class);
            var name = ReadString(node, CfgMgr32.DEVPKEY_NAME) ?? ReadString(node, CfgMgr32.DEVPKEY_Device_DriverDesc);

            if (name is null)
            {
                continue;
            }

            drivers.Add(new DriverInfo(
                name,
                className,
                DeviceClasses.Classify(className, provider),
                provider,
                ReadString(node, CfgMgr32.DEVPKEY_Device_DriverVersion),
                ReadDate(node, CfgMgr32.DEVPKEY_Device_DriverDate),
                ReadString(node, CfgMgr32.DEVPKEY_Device_DriverInfPath),
                ReadString(node, CfgMgr32.DEVPKEY_Device_Manufacturer),
                ReadStringList(node, CfgMgr32.DEVPKEY_Device_HardwareIds),
                ReadUInt32(node, CfgMgr32.DEVPKEY_Device_ProblemCode)));
        }

        return drivers;
    }

    private static List<string> PresentDeviceIds()
    {
        var ids = new List<string>();

        if (CfgMgr32.CM_Get_Device_ID_List_SizeW(out var length, null, CfgMgr32.CM_GETIDLIST_FILTER_PRESENT) != CfgMgr32.CR_SUCCESS || length == 0)
        {
            return ids;
        }

        var buffer = new char[length];

        if (CfgMgr32.CM_Get_Device_ID_ListW(null, buffer, length, CfgMgr32.CM_GETIDLIST_FILTER_PRESENT) != CfgMgr32.CR_SUCCESS)
        {
            return ids;
        }

        // A multi-string: entries end with one NUL, the list with a second.
        var start = 0;

        for (var index = 0; index < buffer.Length; index++)
        {
            if (buffer[index] != '\0')
            {
                continue;
            }

            if (index == start)
            {
                break;
            }

            ids.Add(new string(buffer, start, index - start));
            start = index + 1;
        }

        return ids;
    }

    private static byte[]? ReadProperty(uint node, in CfgMgr32.DEVPROPKEY key, uint expectedType)
    {
        var size = 0u;
        var status = CfgMgr32.CM_Get_DevNode_PropertyW(node, key, out var type, [], ref size, 0);

        if (status != CfgMgr32.CR_BUFFER_SMALL || size == 0 || type != expectedType)
        {
            return null;
        }

        var buffer = new byte[size];
        status = CfgMgr32.CM_Get_DevNode_PropertyW(node, key, out type, buffer, ref size, 0);
        return status == CfgMgr32.CR_SUCCESS && type == expectedType ? buffer : null;
    }

    private static string? ReadString(uint node, in CfgMgr32.DEVPROPKEY key)
    {
        var bytes = ReadProperty(node, key, CfgMgr32.DEVPROP_TYPE_STRING);
        return bytes is null ? null : Terminated(Encoding.Unicode.GetString(bytes));
    }

    private static List<string> ReadStringList(uint node, in CfgMgr32.DEVPROPKEY key)
    {
        var bytes = ReadProperty(node, key, CfgMgr32.DEVPROP_TYPE_STRING_LIST);
        return bytes is null ? [] : Encoding.Unicode.GetString(bytes).Split('\0', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static DateOnly? ReadDate(uint node, in CfgMgr32.DEVPROPKEY key)
    {
        var bytes = ReadProperty(node, key, CfgMgr32.DEVPROP_TYPE_FILETIME);

        if (bytes is null || bytes.Length < 8)
        {
            return null;
        }

        var fileTime = BinaryPrimitives.ReadInt64LittleEndian(bytes);
        return fileTime <= 0 ? null : DateOnly.FromDateTime(DateTime.FromFileTimeUtc(fileTime));
    }

    private static int ReadUInt32(uint node, in CfgMgr32.DEVPROPKEY key)
    {
        var bytes = ReadProperty(node, key, CfgMgr32.DEVPROP_TYPE_UINT32);
        return bytes is null || bytes.Length < 4 ? 0 : (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    private static string Terminated(string text)
    {
        var end = text.IndexOf('\0', StringComparison.Ordinal);
        return (end < 0 ? text : text[..end]).Trim();
    }
}
