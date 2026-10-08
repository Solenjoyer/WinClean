using System.IO;
using WinClean.Core.Monitoring;
using WinClean.Native;

namespace WinClean.Services.Monitoring;

/// <summary>Lists mounted volumes with their free space. Runs on the slow tier: a sleeping USB disk can take seconds to answer.</summary>
internal sealed class VolumeSampler
{
    private static readonly string SystemRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";

    public string? Reason { get; private set; }

    public IReadOnlyList<VolumeSample> Sample()
    {
        var mask = Kernel32.GetLogicalDrives();

        if (mask == 0)
        {
            Reason = Win32Reason.LastError();
            return [];
        }

        Reason = null;
        var volumes = new List<VolumeSample>();

        // Without this, a missing disk in a card reader pops up a system dialog.
        Kernel32.SetThreadErrorMode(Kernel32.SEM_FAILCRITICALERRORS, out var previousMode);

        try
        {
            for (var bit = 0; bit < 26; bit++)
            {
                if ((mask & (1u << bit)) == 0)
                {
                    continue;
                }

                var root = (char)('A' + bit) + @":\";
                var kind = Kernel32.GetDriveTypeW(root) switch
                {
                    Kernel32.DRIVE_FIXED => VolumeKind.Fixed,
                    Kernel32.DRIVE_REMOVABLE => VolumeKind.Removable,
                    Kernel32.DRIVE_REMOTE => VolumeKind.Network,
                    Kernel32.DRIVE_CDROM => VolumeKind.Optical,
                    Kernel32.DRIVE_RAMDISK => VolumeKind.RamDisk,
                    _ => VolumeKind.Unknown,
                };

                if (kind == VolumeKind.Unknown || !Kernel32.GetDiskFreeSpaceExW(root, out _, out var total, out var free))
                {
                    continue;
                }

                var (label, fileSystem) = ReadVolumeInformation(root);
                volumes.Add(new VolumeSample(root, label, fileSystem, kind, (long)total, (long)free, string.Equals(root, SystemRoot, StringComparison.OrdinalIgnoreCase)));
            }
        }
        finally
        {
            Kernel32.SetThreadErrorMode(previousMode, out _);
        }

        return volumes;
    }

    private static (string Label, string FileSystem) ReadVolumeInformation(string root)
    {
        Span<char> label = stackalloc char[261];
        Span<char> fileSystem = stackalloc char[261];

        if (!Kernel32.GetVolumeInformationW(root, label, (uint)label.Length, out _, out _, out _, fileSystem, (uint)fileSystem.Length))
        {
            return (string.Empty, string.Empty);
        }

        return (Terminated(label), Terminated(fileSystem));
    }

    private static string Terminated(ReadOnlySpan<char> buffer)
    {
        var end = buffer.IndexOf('\0');
        return new string(end < 0 ? buffer : buffer[..end]);
    }
}
