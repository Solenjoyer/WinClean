using System.Windows;
using WinClean.ViewModels;

namespace WinClean;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel shell)
    {
        InitializeComponent();
        DataContext = shell;
    }
}
