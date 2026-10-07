using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Core.Settings;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Monitoring;
using WinClean.Services.Shell;

namespace WinClean.ViewModels;

/// <summary>Every option, saved the moment it changes. Index properties back the combo boxes.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private static readonly int[] RefreshIntervals = [1, 2, 5];

    private static readonly int[] TemporaryAges = [1, 6, 24, 48];

    private static readonly int[] StaleDays = [30, 60, 90, 180];

    private static readonly int[] OldDownloadDays = [30, 90, 180, 365];

    private readonly SettingsStore _settings;

    private readonly MonitoringScheduler _scheduler;

    private readonly ShellLinks _links;

    private readonly Elevation _elevation;

    private readonly ILogger<SettingsViewModel> _logger;

    private bool _loading;

    public SettingsViewModel(SettingsStore settings, MonitoringScheduler scheduler, ShellLinks links, Elevation elevation, ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _scheduler = scheduler;
        _links = links;
        _elevation = elevation;
        _logger = logger;

        DataFolder = settings.Location.DataDirectory;
        DataFolderDescription = settings.Location.IsPortable ? Strings.Settings_DataFolderPortable : Strings.Settings_DataFolderPerUser;
        IsElevated = Elevation.IsElevated;
        ElevationText = IsElevated ? Strings.Settings_Elevated : Strings.Settings_NotElevated;
        VersionText = string.Format(CultureInfo.CurrentCulture, Strings.Settings_VersionLine, AppInfo.Name, AppInfo.Version);
        WorkingSetText = string.Empty;

        LoadFrom(settings.Current);
        _settings.Changed += (_, current) => LoadFrom(current);
        _scheduler.SampleReady += OnSample;
    }

    public string DataFolder { get; }

    public string DataFolderDescription { get; }

    public bool IsElevated { get; }

    public string ElevationText { get; }

    public string VersionText { get; }

    public ObservableCollection<DiagnosticItem> Diagnostics { get; } = [];

    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    [ObservableProperty]
    public partial int RefreshIntervalIndex { get; set; }

    [ObservableProperty]
    public partial bool ShowSystemProcesses { get; set; }

    [ObservableProperty]
    public partial bool GroupByApplication { get; set; }

    [ObservableProperty]
    public partial int TemperatureUnitIndex { get; set; }

    [ObservableProperty]
    public partial bool SensorsEnabled { get; set; }

    [ObservableProperty]
    public partial bool TrayEnabled { get; set; }

    [ObservableProperty]
    public partial int TrayStyleIndex { get; set; }

    [ObservableProperty]
    public partial bool TrayShowCpu { get; set; }

    [ObservableProperty]
    public partial bool TrayShowMemory { get; set; }

    [ObservableProperty]
    public partial bool TrayShowDisk { get; set; }

    [ObservableProperty]
    public partial bool CloseToTray { get; set; }

    [ObservableProperty]
    public partial bool StartWithWindows { get; set; }

    [ObservableProperty]
    public partial bool StartMinimized { get; set; }

    [ObservableProperty]
    public partial int TemporaryAgeIndex { get; set; }

    [ObservableProperty]
    public partial int StaleDaysIndex { get; set; }

    [ObservableProperty]
    public partial int OldDownloadDaysIndex { get; set; }

    [ObservableProperty]
    public partial bool VerboseLogging { get; set; }

    [ObservableProperty]
    public partial string WorkingSetText { get; private set; }

    partial void OnThemeIndexChanged(int value) => Save(current => current with { Theme = (ThemePreference)value });

    partial void OnRefreshIntervalIndexChanged(int value) => Save(current => current with { RefreshIntervalSeconds = RefreshIntervals[Math.Clamp(value, 0, RefreshIntervals.Length - 1)] });

    partial void OnShowSystemProcessesChanged(bool value) => Save(current => current with { ShowSystemProcesses = value });

    partial void OnGroupByApplicationChanged(bool value) => Save(current => current with { GroupByApplication = value });

    partial void OnTemperatureUnitIndexChanged(int value) => Save(current => current with { TemperatureUnit = (TemperatureUnit)value });

    partial void OnSensorsEnabledChanged(bool value) => Save(current => current with { SensorsEnabled = value });

    partial void OnTrayEnabledChanged(bool value) => Save(current => current with { Tray = current.Tray with { Enabled = value } });

    partial void OnTrayStyleIndexChanged(int value) => Save(current => current with { Tray = current.Tray with { Style = (TrayIconStyle)value } });

    partial void OnTrayShowCpuChanged(bool value) => Save(current => current with { Tray = current.Tray with { ShowCpu = value } });

    partial void OnTrayShowMemoryChanged(bool value) => Save(current => current with { Tray = current.Tray with { ShowMemory = value } });

    partial void OnTrayShowDiskChanged(bool value) => Save(current => current with { Tray = current.Tray with { ShowDisk = value } });

    partial void OnCloseToTrayChanged(bool value) => Save(current => current with { Tray = current.Tray with { CloseToTray = value } });

    partial void OnStartWithWindowsChanged(bool value)
    {
        Save(current => current with { StartWithWindows = value });
        RegisterStartup();
    }

    partial void OnStartMinimizedChanged(bool value)
    {
        Save(current => current with { StartMinimized = value });
        RegisterStartup();
    }

    partial void OnTemporaryAgeIndexChanged(int value) => Save(current => current with { Cleanup = current.Cleanup with { TemporaryFileMinimumAgeHours = TemporaryAges[Math.Clamp(value, 0, TemporaryAges.Length - 1)] } });

    partial void OnStaleDaysIndexChanged(int value) => Save(current => current with { Cleanup = current.Cleanup with { StaleArtifactDays = StaleDays[Math.Clamp(value, 0, StaleDays.Length - 1)] } });

    partial void OnOldDownloadDaysIndexChanged(int value) => Save(current => current with { Cleanup = current.Cleanup with { OldDownloadDays = OldDownloadDays[Math.Clamp(value, 0, OldDownloadDays.Length - 1)] } });

    partial void OnVerboseLoggingChanged(bool value) => Save(current => current with { VerboseLogging = value });

    [RelayCommand]
    private void OpenLogFolder() => _links.OpenFolder(_settings.Location.LogDirectory);

    [RelayCommand]
    private void OpenDataFolder() => _links.OpenFolder(_settings.Location.DataDirectory);

    [RelayCommand]
    private void OpenReleases() => _links.OpenUrl(new Uri(AppInfo.ReleasesUrl));

    [RelayCommand]
    private void OpenRepository() => _links.OpenUrl(new Uri(AppInfo.RepositoryUrl));

    [RelayCommand]
    private void RestartAsAdministrator() => _elevation.RestartElevated(PageKeys.Settings);

    private void LoadFrom(AppSettings current)
    {
        if (_loading)
        {
            return;
        }

        _loading = true;

        try
        {
            ThemeIndex = (int)current.Theme;
            RefreshIntervalIndex = Math.Max(0, Array.IndexOf(RefreshIntervals, current.RefreshIntervalSeconds));
            ShowSystemProcesses = current.ShowSystemProcesses;
            GroupByApplication = current.GroupByApplication;
            TemperatureUnitIndex = (int)current.TemperatureUnit;
            SensorsEnabled = current.SensorsEnabled;
            TrayEnabled = current.Tray.Enabled;
            TrayStyleIndex = (int)current.Tray.Style;
            TrayShowCpu = current.Tray.ShowCpu;
            TrayShowMemory = current.Tray.ShowMemory;
            TrayShowDisk = current.Tray.ShowDisk;
            CloseToTray = current.Tray.CloseToTray;
            StartWithWindows = StartupRegistration.IsEnabled();
            StartMinimized = current.StartMinimized;
            TemporaryAgeIndex = Math.Max(0, Array.IndexOf(TemporaryAges, current.Cleanup.TemporaryFileMinimumAgeHours));
            StaleDaysIndex = Math.Max(0, Array.IndexOf(StaleDays, current.Cleanup.StaleArtifactDays));
            OldDownloadDaysIndex = Math.Max(0, Array.IndexOf(OldDownloadDays, current.Cleanup.OldDownloadDays));
            VerboseLogging = current.VerboseLogging;
        }
        finally
        {
            _loading = false;
        }
    }

    private void Save(Func<AppSettings, AppSettings> change)
    {
        if (!_loading)
        {
            _settings.Update(change);
        }
    }

    private void RegisterStartup()
    {
        if (_loading)
        {
            return;
        }

        try
        {
            StartupRegistration.Apply(StartWithWindows, StartMinimized);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException or IOException)
        {
            _logger.LogWarning(exception, "The startup registration could not be changed.");
        }
    }

    private void OnSample(object? sender, SystemSample sample)
    {
        WorkingSetText = string.Format(CultureInfo.CurrentCulture, Strings.Settings_WorkingSet, ByteSize.Format(Environment.WorkingSet));

        var statuses = _scheduler.Statuses;

        if (Diagnostics.Count != statuses.Count)
        {
            Diagnostics.Clear();

            foreach (var status in statuses)
            {
                Diagnostics.Add(new DiagnosticItem(status.Name, StatusText(status)));
            }

            return;
        }

        for (var index = 0; index < statuses.Count; index++)
        {
            Diagnostics[index].Status = StatusText(statuses[index]);
        }
    }

    private static string StatusText(SamplerStatus status)
    {
        return status.Available ? Strings.Settings_DiagnosticOk : status.Reason ?? Strings.NotAvailable;
    }
}
