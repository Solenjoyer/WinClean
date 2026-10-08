using System.Windows;
using System.Windows.Controls;
using WinClean.ViewModels;

namespace WinClean.Views;

public partial class HardwarePage : UserControl
{
    public HardwarePage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HardwareViewModel viewModel)
        {
            await viewModel.ActivateAsync();
        }
    }
}
