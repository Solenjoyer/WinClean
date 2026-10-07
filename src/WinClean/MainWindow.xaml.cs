using System.ComponentModel;
using System.Windows;
using WinClean.Core.Settings;
using WinClean.Services;
using WinClean.ViewModels;

namespace WinClean;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;

    private readonly SettingsStore _settings;

    public MainWindow(ShellViewModel shell, SettingsStore settings)
    {
        InitializeComponent();

        _shell = shell;
        _settings = settings;
        DataContext = shell;

        RestorePlacement(settings.Current.Window);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (e.Cancel)
        {
            return;
        }

        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;

        _settings.Update(current => current with
        {
            Window = new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, WindowState == WindowState.Maximized),
            CompactNavigation = _shell.IsCompact,
        });
    }

    /// <summary>Puts the window back where it was, unless that spot is no longer on a connected screen.</summary>
    private void RestorePlacement(WindowPlacement? placement)
    {
        if (placement is null || placement.Width < MinWidth || placement.Height < MinHeight)
        {
            return;
        }

        var screens = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        var visible = Rect.Intersect(new Rect(placement.Left, placement.Top, placement.Width, placement.Height), screens);

        if (visible.IsEmpty || visible.Width < 200 || visible.Height < 120)
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = placement.Left;
        Top = placement.Top;
        Width = placement.Width;
        Height = placement.Height;

        if (placement.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
    }
}
