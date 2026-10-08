using System.Windows;
using System.Windows.Controls;
using WinClean.ViewModels;

namespace WinClean.Views;

public partial class HealthPage : UserControl
{
    public HealthPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HealthViewModel viewModel)
        {
            await viewModel.ActivateAsync();
        }
    }
}
