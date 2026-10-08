using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using WinClean.ViewModels;

namespace WinClean.Views;

public partial class StoragePage : UserControl
{
    public StoragePage()
    {
        InitializeComponent();
    }

    private StorageViewModel? ViewModel => DataContext as StorageViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            await viewModel.ActivateAsync();
        }
    }

    private async void OnScanFolder(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var dialog = new OpenFolderDialog { Multiselect = false };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true && dialog.FolderName is { Length: > 0 } folder)
        {
            await viewModel.ScanPathAsync(folder, fastMedia: true);
        }
    }

    private void OnFolderDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FolderList.SelectedItem is FolderItem folder)
        {
            ViewModel?.EnterCommand.Execute(folder);
        }
    }

    private void OnFolderKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Return or Key.Right && FolderList.SelectedItem is FolderItem folder)
        {
            ViewModel?.EnterCommand.Execute(folder);
            e.Handled = true;
        }
    }
}
