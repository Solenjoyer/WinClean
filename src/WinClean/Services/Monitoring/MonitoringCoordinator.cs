using System.ComponentModel;
using WinClean.Core.Settings;
using WinClean.ViewModels;

namespace WinClean.Services.Monitoring;

/// <summary>Translates what the user is looking at into what the sampler thread should read.</summary>
public sealed class MonitoringCoordinator : IDisposable
{
    private readonly MonitoringScheduler _scheduler;

    private readonly ShellViewModel _shell;

    private readonly SettingsStore _settings;

    private bool _windowVisible;

    private bool _widgetVisible;

    public MonitoringCoordinator(MonitoringScheduler scheduler, ShellViewModel shell, SettingsStore settings)
    {
        _scheduler = scheduler;
        _shell = shell;
        _settings = settings;

        _shell.PropertyChanged += OnShellChanged;
        _settings.Changed += OnSettingsChanged;

        // A start minimised to the notification area opens no window, so nothing else would start the sampler.
        Apply();
    }

    /// <summary>True while the main window is shown and not minimised.</summary>
    public bool WindowVisible
    {
        get => _windowVisible;
        set
        {
            if (_windowVisible != value)
            {
                _windowVisible = value;
                Apply();
            }
        }
    }

    /// <summary>True while the desktop widget is shown; it needs metrics, and processes when it lists the running tools.</summary>
    public bool WidgetVisible
    {
        get => _widgetVisible;
        set
        {
            if (_widgetVisible != value)
            {
                _widgetVisible = value;
                Apply();
            }
        }
    }

    public void Dispose()
    {
        _shell.PropertyChanged -= OnShellChanged;
        _settings.Changed -= OnSettingsChanged;
    }

    private static MonitoringDemand Merge(MonitoringDemand first, MonitoringDemand second)
    {
        if (!first.Any)
        {
            return second;
        }

        if (!second.Any)
        {
            return first;
        }

        return new MonitoringDemand(
            first.Metrics || second.Metrics,
            first.Detailed || second.Detailed,
            first.Processes || second.Processes,
            Math.Min(first.IntervalSeconds, second.IntervalSeconds));
    }

    private void OnShellChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.SelectedItem))
        {
            Apply();
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings settings) => Apply();

    private void Apply()
    {
        var demand = Demand();

        if (demand.Any)
        {
            _scheduler.Start();
        }

        _scheduler.Demand = demand;
    }

    private MonitoringDemand Demand()
    {
        var settings = _settings.Current;
        var interval = settings.RefreshIntervalSeconds;
        var background = Math.Max(interval, 2);
        var widget = _widgetVisible
            ? new MonitoringDemand(true, settings.Widget.Layout == WidgetLayout.Full, settings.Widget.ShowsTools, background)
            : MonitoringDemand.None;

        if (!_windowVisible)
        {
            var tray = settings.Tray.Enabled ? new MonitoringDemand(true, false, false, background) : MonitoringDemand.None;
            return Merge(tray, widget);
        }

        var page = _shell.SelectedItem.Key switch
        {
            PageKeys.Overview => new MonitoringDemand(true, true, true, interval),
            PageKeys.Processes => new MonitoringDemand(true, true, true, interval),
            _ => new MonitoringDemand(true, false, false, interval),
        };

        return Merge(page, widget);
    }
}
