using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using WinClean.Core.Settings;
using WinClean.Dialogs;
using WinClean.Native;
using WinClean.Services;
using WinClean.Services.Monitoring;
using WinClean.Services.Shell;
using WinClean.ViewModels;

namespace WinClean;

public partial class MainWindow : Window, IDialogHost
{
    private readonly ShellViewModel _shell;

    private readonly SettingsStore _settings;

    private readonly MonitoringCoordinator _monitoring;

    public MainWindow(ShellViewModel shell, SettingsStore settings, MonitoringCoordinator monitoring)
    {
        InitializeComponent();

        _shell = shell;
        _settings = settings;
        _monitoring = monitoring;
        DataContext = shell;

        RestorePlacement(settings.Current.Window);
        IsVisibleChanged += (_, _) => UpdateVisibility();
        StateChanged += (_, _) => UpdateVisibility();
    }

    /// <summary>Restores, shows and activates the window, for the tray and for a second launch.</summary>
    public void BringToFront()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
        User32.SetForegroundWindow(new WindowInteropHelper(this).Handle);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(HandleWindowMessage);

        if (source is not null && ProcessContext.IsElevated)
        {
            // An elevated window ignores messages from ordinary processes unless told otherwise.
            User32.ChangeWindowMessageFilterEx(source.Handle, SingleInstance.ActivationMessage, User32.MSGFLT_ALLOW, 0);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (e.Cancel)
        {
            return;
        }

        SavePlacement();

        var current = _settings.Current;
        var keepsRunning = (current.Tray.Enabled && current.Tray.CloseToTray) || current.Widget.Enabled;

        if (keepsRunning && !((App)Application.Current).IsExiting)
        {
            e.Cancel = true;
            Hide();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        ((App)Application.Current).ExitApplication();
    }

    private void SavePlacement()
    {
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;

        _settings.Update(current => current with
        {
            Window = new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, WindowState == WindowState.Maximized),
            CompactNavigation = _shell.IsCompact,
        });
    }

    public void SetDialogOpen(bool open) => Smoke.Visibility = open ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateVisibility() => _monitoring.WindowVisible = IsVisible && WindowState != WindowState.Minimized;

    private nint HandleWindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if ((uint)message == SingleInstance.ActivationMessage)
        {
            BringToFront();
            _shell.NavigateTo(PageKeys.FromIndex((int)wParam));
            handled = true;
        }

        return 0;
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
