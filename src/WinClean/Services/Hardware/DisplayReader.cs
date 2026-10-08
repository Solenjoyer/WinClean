using System.Buffers.Binary;
using WinClean.Native;

namespace WinClean.Services.Hardware;

/// <summary>Active displays with the mode they run at, from the legacy display API that every adapter supports.</summary>
internal static class DisplayReader
{
    public static unsafe IReadOnlyList<DisplayFacts> Read()
    {
        var displays = new List<DisplayFacts>();

        for (var index = 0u; ; index++)
        {
            var adapter = new User32.DISPLAY_DEVICEW { cb = (uint)sizeof(User32.DISPLAY_DEVICEW) };

            if (!User32.EnumDisplayDevicesW(null, index, ref adapter, 0))
            {
                break;
            }

            if ((adapter.StateFlags & User32.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0 || (adapter.StateFlags & User32.DISPLAY_DEVICE_MIRRORING_DRIVER) != 0)
            {
                continue;
            }

            var adapterName = new string(adapter.DeviceName);
            var adapterDescription = new string(adapter.DeviceString);
            var monitor = new User32.DISPLAY_DEVICEW { cb = (uint)sizeof(User32.DISPLAY_DEVICEW) };
            var monitorName = User32.EnumDisplayDevicesW(adapterName, 0, ref monitor, 0) ? new string(monitor.DeviceString) : string.Empty;

            var mode = new User32.DEVMODEW();
            BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(mode.Data, User32.DEVMODEW.Size)[68..], User32.DEVMODEW.Size);

            if (!User32.EnumDisplaySettingsW(adapterName, User32.ENUM_CURRENT_SETTINGS, ref mode))
            {
                continue;
            }

            var data = new ReadOnlySpan<byte>(mode.Data, User32.DEVMODEW.Size);
            displays.Add(new DisplayFacts(
                monitorName.Length == 0 ? adapterName : monitorName,
                adapterDescription,
                (int)BinaryPrimitives.ReadUInt32LittleEndian(data[172..]),
                (int)BinaryPrimitives.ReadUInt32LittleEndian(data[176..]),
                (int)BinaryPrimitives.ReadUInt32LittleEndian(data[184..]),
                (adapter.StateFlags & User32.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0));
        }

        return displays;
    }
}
