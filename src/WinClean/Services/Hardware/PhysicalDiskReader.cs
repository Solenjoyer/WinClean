using System.Buffers.Binary;
using Microsoft.Win32.SafeHandles;
using WinClean.Core.Hardware.Parsers;
using WinClean.Native;

namespace WinClean.Services.Hardware;

/// <summary>Physical disks and the volumes on them, through the storage IOCTLs that need no rights beyond opening the device.</summary>
internal static class PhysicalDiskReader
{
    private const int MaximumDisks = 32;

    public static IReadOnlyList<PhysicalDisk> Read(IEnumerable<string> volumeRoots)
    {
        var volumesByDisk = MapVolumes(volumeRoots);
        var disks = new List<PhysicalDisk>();

        for (var number = 0; number < MaximumDisks; number++)
        {
            using var handle = Open($@"\\.\PhysicalDrive{number}");

            if (handle is null)
            {
                continue;
            }

            if (!QueryProperty(handle, Kernel32.StorageDeviceProperty, out var descriptorBytes) || !StorageDeviceDescriptorParser.TryParse(descriptorBytes, out var descriptor))
            {
                continue;
            }

            disks.Add(new PhysicalDisk(
                number,
                descriptor.Model.Length == 0 ? $"Disk {number}" : descriptor.Model,
                descriptor.BusName,
                descriptor.SerialNumber,
                ReadCapacity(handle),
                QueryProperty(handle, Kernel32.StorageDeviceSeekPenaltyProperty, out var seek) && seek.Length > 8 ? seek[8] == 0 : null,
                QueryProperty(handle, Kernel32.StorageDeviceTrimProperty, out var trim) && trim.Length > 8 ? trim[8] != 0 : null,
                descriptor.RemovableMedia,
                volumesByDisk.TryGetValue(number, out var volumes) ? volumes : []));
        }

        return disks;
    }

    private static SafeFileHandle? Open(string path)
    {
        var handle = Kernel32.CreateFileW(path, 0, Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE, 0, Kernel32.OPEN_EXISTING, 0, 0);

        if (handle.IsInvalid)
        {
            handle.Dispose();
            return null;
        }

        return handle;
    }

    private static bool QueryProperty(SafeFileHandle handle, int propertyId, out byte[] output)
    {
        Span<byte> query = stackalloc byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(query, propertyId);
        output = new byte[1024];

        if (Kernel32.DeviceIoControl(handle, Kernel32.IOCTL_STORAGE_QUERY_PROPERTY, query, (uint)query.Length, output, (uint)output.Length, out var returned, 0))
        {
            Array.Resize(ref output, (int)returned);
            return true;
        }

        return false;
    }

    private static long ReadCapacity(SafeFileHandle handle)
    {
        Span<byte> geometry = stackalloc byte[32];
        return Kernel32.DeviceIoControl(handle, Kernel32.IOCTL_DISK_GET_DRIVE_GEOMETRY_EX, [], 0, geometry, (uint)geometry.Length, out var returned, 0) && returned >= 32
            ? BinaryPrimitives.ReadInt64LittleEndian(geometry[24..])
            : 0;
    }

    private static Dictionary<int, List<string>> MapVolumes(IEnumerable<string> volumeRoots)
    {
        var map = new Dictionary<int, List<string>>();
        Span<byte> extents = stackalloc byte[8 + 24 * 8];

        foreach (var root in volumeRoots)
        {
            var letter = root.TrimEnd('\\');
            using var handle = Open(@"\\.\" + letter);

            if (handle is null)
            {
                continue;
            }

            extents.Clear();

            if (!Kernel32.DeviceIoControl(handle, Kernel32.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, [], 0, extents, (uint)extents.Length, out var returned, 0) && returned < 32)
            {
                continue;
            }

            var count = Math.Min((int)BinaryPrimitives.ReadUInt32LittleEndian(extents), 8);

            for (var index = 0; index < count; index++)
            {
                var disk = (int)BinaryPrimitives.ReadUInt32LittleEndian(extents[(8 + index * 24)..]);

                if (!map.TryGetValue(disk, out var list))
                {
                    list = [];
                    map[disk] = list;
                }

                list.Add(letter);
            }
        }

        return map;
    }
}
