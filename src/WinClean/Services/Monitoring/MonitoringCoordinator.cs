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

    public MonitoringCoordinator(MonitoringScheduler scheduler, ShellViewModel shell, SettingsStore settings)
    {
        _scheduler = scheduler;
        _shell = shell;
        _settings = settings;

        _shell.PropertyChanged += OnShellChanged;
        _settings.Changed += OnSettingsChanged;
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

    public void Dispose()
    {
        _shell.PropertyChanged -= OnShellChanged;
        _settings.Changed -= OnSettingsChanged;
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
        var interval = _settings.Current.RefreshIntervalSeconds;

        if (!_windowVisible)
        {
            return _settings.Current.Tray.Enabled ? new MonitoringDemand(true, false, false, Math.Max(interval, 2)) : MonitoringDemand.None;
        }

        return _shell.SelectedItem.Key switch
        {
            PageKeys.Overview => new MonitoringDemand(true, true, false, interval),
            PageKeys.Processes => new MonitoringDemand(true, true, true, interval),
            _ => new MonitoringDemand(true, false, false, interval),
        };
    }
}
