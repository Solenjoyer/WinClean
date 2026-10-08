using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using WinClean.Core.Settings;
using WinClean.Native;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Monitoring;
using WinClean.ViewModels;

namespace WinClean;

/// <summary>
/// The desktop widget: a borderless window that never takes focus, stays out of Alt+Tab and the
/// taskbar, and sits where the placement setting says. It has no owner on purpose: an owned window
/// can never go below its owner, which would defeat the "behind other windows" placement.
/// </summary>
public partial class WidgetWindow : Window
{
    private const double EdgeMargin = 16;

    private const double SnapDistance = 8;

    private static readonly WidgetPlacement[] Placements = [WidgetPlacement.AlwaysOnTop, WidgetPlacement.BehindWindows, WidgetPlacement.Normal];

    private readonly SettingsStore _settings;

    private readonly MonitoringCoordinator _monitoring;

    private readonly MenuItem[] _placementItems;

    private readonly MenuItem _lockItem;

    private bool _behind;

    private nint _previousForeground;

    public WidgetWindow(WidgetViewModel viewModel, SettingsStore settings, MonitoringCoordinator monitoring)
    {
        InitializeComponent();

        _settings = settings;
        _monitoring = monitoring;
        DataContext = viewModel;

        // WPF hands the first window it creates to Application.MainWindow; dialogs and crash reports must not be owned by the widget.
        if (ReferenceEquals(Application.Current.MainWindow, this))
        {
            Application.Current.MainWindow = null;
        }

        _placementItems = new MenuItem[Placements.Length];
        _lockItem = new MenuItem { Header = Strings.Widget_LockPosition, IsCheckable = true, IsChecked = settings.Current.Widget.Locked };
        ContextMenu = BuildMenu();

        RestorePosition(settings.Current.Widget);
        _settings.Changed += OnSettingsChanged;
        IsVisibleChanged += OnVisibilityChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(HandleWindowMessage);

        // Out of Alt+Tab and the taskbar, and never activated: the widget is looked at, not worked in.
        var style = User32.GetWindowLongPtrW(handle, User32.GWL_EXSTYLE);
        style = (style | (nint)User32.WS_EX_TOOLWINDOW | (nint)User32.WS_EX_NOACTIVATE) & ~(nint)User32.WS_EX_APPWINDOW;

        if (_settings.Current.Widget.ClickThrough)
        {
            style |= (nint)User32.WS_EX_TRANSPARENT;
        }

        User32.SetWindowLongPtrW(handle, User32.GWL_EXSTYLE, style);
        ApplyPlacement(_settings.Current.Widget.Placement);
    }

    protected override void OnClosed(EventArgs e)
    {
        _settings.Changed -= OnSettingsChanged;
        base.OnClosed(e);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ClickCount == 2)
        {
            OpenMainWindow(null);
            return;
        }

        var widget = _settings.Current.Widget;

        if (widget.Placement == WidgetPlacement.Normal)
        {
            // A window that never activates does not rise on its own.
            Pin(User32.HWND_TOP);
        }

        if (widget.Locked)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The button was released before the move loop could start.
            return;
        }

        Snap();
        SavePosition();
    }

    protected override void OnContextMenuOpening(ContextMenuEventArgs e)
    {
        // A background window cannot capture the mouse, and the menu needs the capture to close on an outside click.
        _previousForeground = User32.GetForegroundWindow();
        User32.SetForegroundWindow(new WindowInteropHelper(this).Handle);
        base.OnContextMenuOpening(e);
    }

    private static void Update(SettingsStore settings, Func<WidgetSettings, WidgetSettings> change)
    {
        settings.Update(current => current with { Widget = change(current.Widget) });
    }

    private static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private static string PlacementLabel(WidgetPlacement placement) => placement switch
    {
        WidgetPlacement.AlwaysOnTop => Strings.Settings_WidgetAlwaysOnTop,
        WidgetPlacement.BehindWindows => Strings.Settings_WidgetBehindWindows,
        _ => Strings.Settings_WidgetNormal,
    };

    private static void OpenMainWindow(string? page) => ((App)Application.Current).ShowMainWindow(page);

    private unsafe nint HandleWindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == User32.WM_WINDOWPOSCHANGING && _behind && lParam != 0)
        {
            // Refusing every z-order change is what keeps the widget behind the other windows once it is there.
            ((User32.WINDOWPOS*)lParam)->flags |= User32.SWP_NOZORDER;
        }

        return 0;
    }

    private ContextMenu BuildMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(Item(Strings.Widget_Open, () => OpenMainWindow(null)));
        menu.Items.Add(Item(Strings.Nav_Processes, () => OpenMainWindow(PageKeys.Processes)));
        menu.Items.Add(Item(Strings.Nav_Cleanup, () => OpenMainWindow(PageKeys.Cleanup)));
        menu.Items.Add(new Separator());

        for (var index = 0; index < Placements.Length; index++)
        {
            var placement = Placements[index];
            var item = Item(PlacementLabel(placement), () => Update(_settings, widget => widget with { Placement = placement }));
            item.IsChecked = _settings.Current.Widget.Placement == placement;
            _placementItems[index] = item;
            menu.Items.Add(item);
        }

        menu.Items.Add(new Separator());
        _lockItem.Click += (_, _) => Update(_settings, widget => widget with { Locked = !widget.Locked });
        menu.Items.Add(_lockItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(Strings.Widget_Hide, () => Update(_settings, widget => widget with { Enabled = false })));
        menu.Items.Add(Item(Strings.Widget_Exit, () => ((App)Application.Current).ExitApplication()));
        menu.Closed += OnMenuClosed;
        return menu;
    }

    private void OnMenuClosed(object sender, RoutedEventArgs e)
    {
        if (_previousForeground != 0 && _previousForeground != new WindowInteropHelper(this).Handle)
        {
            User32.SetForegroundWindow(_previousForeground);
        }
    }

    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _monitoring.WidgetVisible = IsVisible;

        if (IsVisible && _behind)
        {
            // Showing a window puts it on top; send it back where the placement wants it.
            Pin(User32.HWND_BOTTOM);
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnSettingsChanged(sender, settings));
            return;
        }

        var widget = settings.Widget;
        ApplyPlacement(widget.Placement);
        ApplyClickThrough(widget.ClickThrough);
        _lockItem.IsChecked = widget.Locked;

        if (widget.Left is null && widget.Top is null)
        {
            MoveToDefault();
        }
    }

    private void ApplyPlacement(WidgetPlacement placement)
    {
        _behind = placement == WidgetPlacement.BehindWindows;
        Topmost = placement == WidgetPlacement.AlwaysOnTop;

        for (var index = 0; index < Placements.Length; index++)
        {
            _placementItems[index].IsChecked = Placements[index] == placement;
        }

        if (_behind)
        {
            Pin(User32.HWND_BOTTOM);
        }
        else if (placement == WidgetPlacement.Normal)
        {
            Pin(User32.HWND_TOP);
        }
    }

    private void ApplyClickThrough(bool enabled)
    {
        var handle = new WindowInteropHelper(this).Handle;

        if (handle == 0)
        {
            return;
        }

        var style = User32.GetWindowLongPtrW(handle, User32.GWL_EXSTYLE);
        var updated = enabled ? style | (nint)User32.WS_EX_TRANSPARENT : style & ~(nint)User32.WS_EX_TRANSPARENT;

        if (updated != style)
        {
            User32.SetWindowLongPtrW(handle, User32.GWL_EXSTYLE, updated);
        }
    }

    private void Pin(nint insertAfter)
    {
        var handle = new WindowInteropHelper(this).Handle;

        if (handle != 0)
        {
            User32.SetWindowPos(handle, insertAfter, 0, 0, 0, 0, User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE | User32.SWP_NOSENDCHANGING);
        }
    }

    private void RestorePosition(WidgetSettings widget)
    {
        WindowStartupLocation = WindowStartupLocation.Manual;

        if (widget.Left is { } left && widget.Top is { } top && IsOnScreen(left, top))
        {
            Left = left;
            Top = top;
        }
        else
        {
            MoveToDefault();
        }
    }

    private void MoveToDefault()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - EdgeMargin;
        Top = area.Top + EdgeMargin;
    }

    private bool IsOnScreen(double left, double top)
    {
        var screens = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        var visible = Rect.Intersect(new Rect(left, top, Width, Math.Max(ActualHeight, 120)), screens);

        return !visible.IsEmpty && visible.Width >= 100 && visible.Height >= 60;
    }

    private void Snap()
    {
        var area = SystemParameters.WorkArea;

        if (Math.Abs(Left - area.Left) <= SnapDistance)
        {
            Left = area.Left;
        }
        else if (Math.Abs(Left + ActualWidth - area.Right) <= SnapDistance)
        {
            Left = area.Right - ActualWidth;
        }

        if (Math.Abs(Top - area.Top) <= SnapDistance)
        {
            Top = area.Top;
        }
        else if (Math.Abs(Top + ActualHeight - area.Bottom) <= SnapDistance)
        {
            Top = area.Bottom - ActualHeight;
        }
    }

    private void SavePosition()
    {
        var left = Left;
        var top = Top;
        Update(_settings, widget => widget with { Left = left, Top = top });
    }
}
