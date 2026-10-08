using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Core.Settings;
using WinClean.Native;
using WinClean.Resources;
using WinClean.Services.Monitoring;
using WinClean.ViewModels;

namespace WinClean.Services.Shell;

/// <summary>
/// The notification area icons. Identified by window and id rather than a GUID, because a GUID binds
/// an icon to the executable's path and signature and breaks portable moves. Re-registered when the
/// taskbar restarts, redrawn when the theme changes, skipped when the reading did not change.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private const uint CallbackMessage = User32.WM_APP + 1;

    private const uint CpuId = 1;

    private const uint MemoryId = 2;

    private const uint DiskId = 3;

    private const int MaximumTipLength = 127;

    private readonly SettingsStore _settings;

    private readonly MonitoringScheduler _scheduler;

    private readonly ILogger<TrayIcon> _logger;

    private readonly uint _taskbarCreated = User32.RegisterWindowMessageW("TaskbarCreated");

    private readonly Dictionary<uint, string> _shown = [];

    private HwndSource? _window;

    private SystemSample? _latest;

    private ContextMenu? _menu;

    private MenuItem? _widgetItem;

    public TrayIcon(SettingsStore settings, MonitoringScheduler scheduler, ILogger<TrayIcon> logger)
    {
        _settings = settings;
        _scheduler = scheduler;
        _logger = logger;

        _settings.Changed += OnSettingsChanged;
        _scheduler.SampleReady += OnSample;
        Apply();
    }

    /// <summary>The user clicked an icon or a menu entry; the argument is the page to open, if any.</summary>
    public event EventHandler<string?>? OpenRequested;

    public event EventHandler? ExitRequested;

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        _scheduler.SampleReady -= OnSample;
        RemoveAll();
        _window?.Dispose();
        _window = null;
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        if (_widgetItem is not null)
        {
            _widgetItem.IsChecked = settings.Widget.Enabled;
        }

        Apply();
    }

    private void OnSample(object? sender, SystemSample sample)
    {
        _latest = sample;

        if (_settings.Current.Tray.Enabled)
        {
            Refresh(force: false);
        }
    }

    private void Apply()
    {
        if (!_settings.Current.Tray.Enabled)
        {
            RemoveAll();
            return;
        }

        EnsureWindow();
        Refresh(force: true);
    }

    private void EnsureWindow()
    {
        if (_window is not null)
        {
            return;
        }

        // A real top-level window, hidden and kept out of Alt+Tab: broadcasts such as TaskbarCreated
        // never reach message-only windows.
        var parameters = new HwndSourceParameters("WinClean.Tray")
        {
            WindowStyle = 0,
            ExtendedWindowStyle = (int)User32.WS_EX_TOOLWINDOW_STYLE,
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
        };

        _window = new HwndSource(parameters);
        _window.AddHook(HandleMessage);
    }

    private void Refresh(bool force)
    {
        if (_window is null)
        {
            return;
        }

        var tray = _settings.Current.Tray;
        var dark = WindowsTheme.SystemUsesDarkTheme();
        var size = User32.GetSystemMetricsForDpi(User32.SM_CXSMICON, User32.GetDpiForSystem());
        var wanted = new HashSet<uint>();

        var cpu = _latest?.CpuPercent ?? 0;
        var memory = _latest?.Memory?.UsedPercent ?? 0;
        var volume = SystemVolume();
        var disk = volume?.UsedPercent ?? 0;

        if (tray.Style == TrayIconStyle.Bars)
        {
            var values = new List<double>();
            var tips = new List<string>();

            if (tray.ShowCpu)
            {
                values.Add(cpu);
                tips.Add(CpuTip(cpu));
            }

            if (tray.ShowMemory)
            {
                values.Add(memory);
                tips.Add(MemoryTip());
            }

            if (tray.ShowDisk && volume is not null)
            {
                values.Add(disk);
                tips.Add(DiskTip(volume));
            }

            if (values.Count > 0)
            {
                wanted.Add(CpuId);
                var inner = size - 2 * (size >= 24 ? 3 : 2);
                var key = $"bars:{size}:{dark}:" + string.Join(',', values.Select(value => TrayIconRenderer.BarHeight(value, inner)));
                Show(CpuId, key, force, () => TrayIconRenderer.RenderBars(values, size, dark), string.Join('\n', tips));
            }
        }
        else
        {
            if (tray.ShowCpu)
            {
                wanted.Add(CpuId);
                var figure = (int)Math.Round(cpu);
                Show(CpuId, $"number:{size}:{dark}:{figure}", force, () => TrayIconRenderer.RenderNumber(figure, size, dark), CpuTip(cpu));
            }

            if (tray.ShowMemory)
            {
                wanted.Add(MemoryId);
                var figure = (int)Math.Round(memory);
                Show(MemoryId, $"number:{size}:{dark}:{figure}", force, () => TrayIconRenderer.RenderNumber(figure, size, dark), MemoryTip());
            }

            if (tray.ShowDisk && volume is not null)
            {
                wanted.Add(DiskId);
                var figure = (int)Math.Round(disk);
                Show(DiskId, $"number:{size}:{dark}:{figure}", force, () => TrayIconRenderer.RenderNumber(figure, size, dark), DiskTip(volume));
            }
        }

        foreach (var id in _shown.Keys.Where(id => !wanted.Contains(id)).ToList())
        {
            Remove(id);
        }
    }

    private void Show(uint id, string key, bool force, Func<byte[]> render, string tip)
    {
        var exists = _shown.TryGetValue(id, out var shownKey);
        var sameImage = exists && string.Equals(shownKey, key, StringComparison.Ordinal);
        var fullKey = key + "|" + tip;

        if (!force && exists && string.Equals(shownKey, fullKey, StringComparison.Ordinal))
        {
            return;
        }

        var icon = sameImage && !force ? 0 : CreateIcon(render());

        try
        {
            var data = new Shell32.NOTIFYICONDATAW
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.NOTIFYICONDATAW>(),
                hWnd = _window!.Handle,
                uID = id,
                uFlags = Shell32.NIF_MESSAGE | Shell32.NIF_TIP | Shell32.NIF_SHOWTIP | (icon != 0 ? Shell32.NIF_ICON : 0),
                uCallbackMessage = CallbackMessage,
                hIcon = icon,
            };

            WriteTip(ref data, tip);

            if (!Shell32.Shell_NotifyIconW(exists ? Shell32.NIM_MODIFY : Shell32.NIM_ADD, ref data))
            {
                _logger.LogWarning("The notification area icon {Id} could not be shown: {Error}", id, Win32Reason.LastError());
                return;
            }

            if (!exists)
            {
                data.uVersion = Shell32.NOTIFYICON_VERSION_4;
                Shell32.Shell_NotifyIconW(Shell32.NIM_SETVERSION, ref data);
            }

            _shown[id] = fullKey;
        }
        finally
        {
            if (icon != 0)
            {
                // The shell keeps its own copy.
                User32.DestroyIcon(icon);
            }
        }
    }

    private nint CreateIcon(byte[] png)
    {
        var icon = User32.CreateIconFromResourceEx(png, (uint)png.Length, fIcon: true, 0x00030000, 0, 0, 0);

        if (icon == 0)
        {
            _logger.LogWarning("The notification area icon could not be created: {Error}", Win32Reason.LastError());
        }

        return icon;
    }

    private static unsafe void WriteTip(ref Shell32.NOTIFYICONDATAW data, string tip)
    {
        var length = Math.Min(tip.Length, MaximumTipLength);

        fixed (char* buffer = data.szTip)
        {
            tip.AsSpan(0, length).CopyTo(new Span<char>(buffer, 128));
            buffer[length] = '\0';
        }
    }

    private void Remove(uint id)
    {
        if (_window is null || !_shown.Remove(id))
        {
            return;
        }

        var data = new Shell32.NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<Shell32.NOTIFYICONDATAW>(),
            hWnd = _window.Handle,
            uID = id,
        };

        Shell32.Shell_NotifyIconW(Shell32.NIM_DELETE, ref data);
    }

    private void RemoveAll()
    {
        foreach (var id in _shown.Keys.ToList())
        {
            Remove(id);
        }
    }

    private nint HandleMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == CallbackMessage)
        {
            switch ((int)(lParam & 0xFFFF))
            {
                case Shell32.NIN_SELECT:
                case Shell32.NIN_KEYSELECT:
                case User32.WM_LBUTTONUP:
                    OpenRequested?.Invoke(this, null);
                    break;

                case User32.WM_CONTEXTMENU:
                    ShowMenu();
                    break;
            }

            handled = true;
        }
        else if ((uint)message == _taskbarCreated)
        {
            // Explorer restarted and forgot every icon.
            _shown.Clear();
            Apply();
        }
        else if (message == User32.WM_SETTINGCHANGE)
        {
            Refresh(force: true);
        }

        return 0;
    }

    private void ShowMenu()
    {
        if (_window is null)
        {
            return;
        }

        _menu ??= BuildMenu();

        // Without the foreground switch the menu stays open after the user clicks elsewhere.
        User32.SetForegroundWindow(_window.Handle);
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }

    private ContextMenu BuildMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(Item(Strings.Tray_Open, () => OpenRequested?.Invoke(this, null)));
        menu.Items.Add(Item(Strings.Nav_Processes, () => OpenRequested?.Invoke(this, PageKeys.Processes)));
        menu.Items.Add(Item(Strings.Nav_Cleanup, () => OpenRequested?.Invoke(this, PageKeys.Cleanup)));
        menu.Items.Add(new Separator());
        _widgetItem = new MenuItem { Header = Strings.Tray_ShowWidget, IsCheckable = true, IsChecked = _settings.Current.Widget.Enabled };
        _widgetItem.Click += (_, _) => _settings.Update(current => current with { Widget = current.Widget with { Enabled = !current.Widget.Enabled } });
        menu.Items.Add(_widgetItem);
        menu.Items.Add(Item(Strings.Nav_Settings, () => OpenRequested?.Invoke(this, PageKeys.Settings)));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(Strings.Tray_Exit, () => ExitRequested?.Invoke(this, EventArgs.Empty)));
        return menu;
    }

    private static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private VolumeSample? SystemVolume()
    {
        var volumes = _latest?.Volumes;

        if (volumes is null)
        {
            return null;
        }

        return volumes.FirstOrDefault(volume => volume.IsSystemVolume) ?? volumes.FirstOrDefault(volume => volume.Kind == VolumeKind.Fixed);
    }

    private static string CpuTip(double cpu) => string.Format(CultureInfo.CurrentCulture, Strings.Tray_CpuTip, Percent.Format(cpu, 0));

    private string MemoryTip()
    {
        if (_latest?.Memory is not { } memory)
        {
            return Strings.Overview_Memory;
        }

        var unit = ByteSize.UnitFor(memory.Total);
        return string.Format(CultureInfo.CurrentCulture, Strings.Tray_MemoryTip, ByteSize.Format(memory.Used, unit), ByteSize.Format(memory.Total, unit), Percent.Format(memory.UsedPercent, 0));
    }

    private static string DiskTip(VolumeSample volume)
    {
        return string.Format(CultureInfo.CurrentCulture, Strings.Tray_DiskTip, volume.Letter, ByteSize.Format(volume.Free), ByteSize.Format(volume.Total));
    }
}
