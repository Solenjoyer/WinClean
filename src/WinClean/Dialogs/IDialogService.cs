namespace WinClean.Dialogs;

/// <summary>Modal questions, behind an interface so view models stay free of windows.</summary>
public interface IDialogService
{
    bool ConfirmEndProcess(EndProcessPrompt prompt);

    bool ConfirmCleanup(CleanupPrompt prompt);

    /// <summary>The one recursive deletion WinClean does; the user types the folder name to confirm.</summary>
    bool ConfirmFolderDelete(string folderName, string path);
}
