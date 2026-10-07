using System.Windows;
using WinClean.Services;

namespace WinClean;

public partial class App : Application
{
    private readonly StartupOptions _options;

    public App(StartupOptions options)
    {
        _options = options;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow();

        if (_options.StartMinimized)
        {
            window.WindowState = WindowState.Minimized;
        }

        window.Show();
    }
}
