using System.Windows;

namespace WinClean.Dialogs;

public sealed class DialogService : IDialogService
{
    public bool ConfirmEndProcess(EndProcessPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return Show(new ConfirmEndProcessDialog(prompt)) == true;
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
