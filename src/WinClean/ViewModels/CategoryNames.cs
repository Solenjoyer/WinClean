using WinClean.Core.Applications;
using WinClean.Resources;

namespace WinClean.ViewModels;

internal static class CategoryNames
{
    /// <summary>The badge text for a category; nothing for applications WinClean does not know.</summary>
    public static string For(ApplicationCategory category) => category switch
    {
        ApplicationCategory.AiAgent => Strings.Category_AiAgent,
        ApplicationCategory.Editor => Strings.Category_Editor,
        ApplicationCategory.Ide => Strings.Category_Ide,
        ApplicationCategory.Container => Strings.Category_Container,
        ApplicationCategory.VirtualMachine => Strings.Category_VirtualMachine,
        ApplicationCategory.Runtime => Strings.Category_Runtime,
        ApplicationCategory.PackageManager => Strings.Category_PackageManager,
        ApplicationCategory.VersionControl => Strings.Category_VersionControl,
        ApplicationCategory.Shell => Strings.Category_Shell,
        ApplicationCategory.Terminal => Strings.Category_Terminal,
        ApplicationCategory.Browser => Strings.Category_Browser,
        ApplicationCategory.Windows => Strings.Category_Windows,
        _ => string.Empty,
    };
}
