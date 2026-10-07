using System.Windows;

namespace WinClean.Dialogs;

public partial class ConfirmEndProcessDialog : DialogWindow
{
    public ConfirmEndProcessDialog(EndProcessPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        InitializeComponent();
        DataContext = prompt;
        ConfirmButton.IsEnabled = !prompt.RequiresAcknowledgement;
    }

    private void OnAcknowledgeChanged(object sender, RoutedEventArgs e)
    {
        ConfirmButton.IsEnabled = Acknowledge.IsChecked == true;
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
