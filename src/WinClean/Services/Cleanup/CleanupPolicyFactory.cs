using System.IO;
using WinClean.Core.Cleanup;

namespace WinClean.Services.Cleanup;

/// <summary>The protected places on this machine, resolved once from the known folders.</summary>
internal static class CleanupPolicyFactory
{
    public static CleanupPolicyOptions Create()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new CleanupPolicyOptions(
            windows,
            [
                Path.Combine(windows, "Temp"),
                Path.Combine(windows, "SoftwareDistribution", "Download"),
                Path.Combine(windows, "Minidump"),
                Path.Combine(windows, "Logs", "CBS"),
            ],
            [
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                profile,
                Path.GetDirectoryName(profile) ?? profile,
            ],
            [
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            ]);
    }
}
