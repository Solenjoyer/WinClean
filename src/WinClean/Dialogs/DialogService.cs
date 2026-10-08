using System.Windows;

namespace WinClean.Dialogs;

public sealed class DialogService : IDialogService
{
    public bool ConfirmEndProcess(EndProcessPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return Show(new ConfirmEndProcessDialog(prompt)) == true;
    }

    public bool ConfirmCleanup(CleanupPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return Show(new ConfirmCleanupDialog(prompt)) == true;
    }

    public bool ConfirmFolderDelete(string folderName, string path)
    {
        return Show(new ConfirmFolderDeleteDialog(folderName, path)) == true;
    }

    private static bool? Show(DialogWindow dialog)
    {
        var owner = Application.Current.MainWindow;

        if (owner is { IsLoaded: true })
        {
            dialog.Owner = owner;
        }

        var host = owner as IDialogHost;
        host?.SetDialogOpen(true);

        try
        {
            return dialog.ShowDialog();
        }
        finally
        {
            host?.SetDialogOpen(false);
        }
    }
}
