using System.Runtime.InteropServices;
using System.Windows;
using WinClean.ViewModels;

namespace WinClean.Dialogs;

public partial class ErrorReportDialog : DialogWindow
{
    public ErrorReportDialog(ErrorReportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCopyDetails(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(DetailsBox.Text);
        }
        catch (COMException)
        {
            // Another application holds the clipboard; the text stays selectable in the box.
        }
    }
}
