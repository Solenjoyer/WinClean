using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WinClean.Resources;

namespace WinClean.Dialogs;

public partial class ConfirmFolderDeleteDialog : DialogWindow
{
    private readonly string _folderName;

    public ConfirmFolderDeleteDialog(string folderName, string path)
    {
        InitializeComponent();
        _folderName = folderName;
        Question.Text = string.Format(CultureInfo.CurrentCulture, Strings.Cleanup_FolderConfirmTitle, folderName);
        Message.Text = string.Format(CultureInfo.CurrentCulture, Strings.Cleanup_FolderConfirmMessage, path);
        Loaded += (_, _) => Input.Focus();
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ConfirmButton.IsEnabled = string.Equals(Input.Text.Trim(), _folderName, StringComparison.Ordinal);
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
