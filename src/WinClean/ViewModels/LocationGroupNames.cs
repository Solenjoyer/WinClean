using WinClean.Core.Storage;
using WinClean.Resources;

namespace WinClean.ViewModels;

internal static class LocationGroupNames
{
    public static string For(LocationGroup group) => group switch
    {
        LocationGroup.Temporary => Strings.LocationGroup_Temporary,
        LocationGroup.Diagnostics => Strings.LocationGroup_Diagnostics,
        LocationGroup.Caches => Strings.LocationGroup_Caches,
        LocationGroup.Browsers => Strings.LocationGroup_Browsers,
        LocationGroup.PackageManagers => Strings.LocationGroup_PackageManagers,
        LocationGroup.PackageStores => Strings.LocationGroup_PackageStores,
        LocationGroup.Ides => Strings.LocationGroup_Ides,
        LocationGroup.AiAgents => Strings.LocationGroup_AiAgents,
        LocationGroup.Docker => Strings.LocationGroup_Docker,
        LocationGroup.Wsl => Strings.LocationGroup_Wsl,
        LocationGroup.WindowsUpdate => Strings.LocationGroup_WindowsUpdate,
        LocationGroup.Downloads => Strings.LocationGroup_Downloads,
        _ => string.Empty,
    };

    public static string ForCategory(FileCategory category) => category switch
    {
        FileCategory.Video => Strings.Category_Video,
        FileCategory.Image => Strings.Category_Image,
        FileCategory.Audio => Strings.Category_Audio,
        FileCategory.Document => Strings.Category_Document,
        FileCategory.Archive => Strings.Category_Archive,
        FileCategory.Installer => Strings.Category_Installer,
        FileCategory.Program => Strings.Category_Program,
        FileCategory.Code => Strings.Category_Code,
        FileCategory.BuildOutput => Strings.Category_BuildOutput,
        FileCategory.DiskImage => Strings.Category_DiskImage,
        FileCategory.Database => Strings.Category_Database,
        _ => Strings.Category_OtherFiles,
    };
}
