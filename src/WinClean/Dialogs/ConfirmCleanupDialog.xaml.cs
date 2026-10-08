using System.Windows;

namespace WinClean.Dialogs;

public partial class ConfirmCleanupDialog : DialogWindow
{
    public ConfirmCleanupDialog(CleanupPrompt prompt)
    {
        InitializeComponent();
        DataContext = prompt;
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
