using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinClean.Core.Formatting;
using WinClean.Core.Health;
using WinClean.Core.Monitoring;
using WinClean.Resources;
using WinClean.Services.Health;
using WinClean.Services.Monitoring;
using WinClean.Services.Shell;

namespace WinClean.ViewModels;

/// <summary>Windows version and support, update facts, restart state, firmware and the driver table. Offline unless "Check now" is clicked.</summary>
public sealed partial class HealthViewModel : ObservableObject
{
    private const int HistoryRows = 6;

    private readonly WindowsUpdateReader _updates;

    private readonly MonitoringScheduler _scheduler;

    private readonly ShellLinks _links;

    private readonly ILogger<HealthViewModel> _logger;

    private IReadOnlyList<DriverInfo> _drivers = [];

    private bool _loaded;

    public HealthViewModel(WindowsUpdateReader updates, MonitoringScheduler scheduler, ShellLinks links, ILogger<HealthViewModel> logger)
    {
        _updates = updates;
        _scheduler = scheduler;
        _links = links;
        _logger = logger;

        WindowsName = Strings.Health_Reading;
        VersionText = string.Empty;
        BuildText = string.Empty;
        InstalledText = string.Empty;
        ServicingText = string.Empty;
        LastCheckText = Strings.NotAvailable;
        LastInstallText = Strings.NotAvailable;
        RestartText = string.Empty;
        SystemDriveText = Strings.NotAvailable;
        LastRestartText = Strings.NotAvailable;
        FirmwareText = Strings.NotAvailable;
        SecureBootText = Strings.NotAvailable;

        scheduler.SampleReady += OnSample;
    }

    public ObservableCollection<UpdateRow> History { get; } = [];

    public ObservableCollection<string> RestartSignals { get; } = [];

    public ObservableCollection<string> AvailableUpdates { get; } = [];

    public ObservableCollection<DriverRow> Drivers { get; } = [];

    [ObservableProperty]
    public partial string WindowsName { get; private set; }

    [ObservableProperty]
    public partial string VersionText { get; private set; }

    [ObservableProperty]
    public partial string BuildText { get; private set; }

    [ObservableProperty]
    public partial string InstalledText { get; private set; }

    [ObservableProperty]
    public partial string ServicingText { get; private set; }

    [ObservableProperty]
    public partial bool SupportEnded { get; private set; }

    [ObservableProperty]
    public partial string LastCheckText { get; private set; }

    [ObservableProperty]
    public partial string LastInstallText { get; private set; }

    [ObservableProperty]
    public partial bool HasHistory { get; private set; }

    [ObservableProperty]
    public partial string RestartText { get; private set; }

    [ObservableProperty]
    public partial bool RestartPending { get; private set; }

    [ObservableProperty]
    public partial bool IsChecking { get; private set; }

    [ObservableProperty]
    public partial string? CheckResult { get; private set; }

    [ObservableProperty]
    public partial string SystemDriveText { get; private set; }

    [ObservableProperty]
    public partial string LastRestartText { get; private set; }

    [ObservableProperty]
    public partial string FirmwareText { get; private set; }

    [ObservableProperty]
    public partial string SecureBootText { get; private set; }

    [ObservableProperty]
    public partial bool ShowAllDevices { get; set; }

    [ObservableProperty]
    public partial bool HasDrivers { get; private set; }

    public async Task ActivateAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadAsync();
    }

    partial void OnShowAllDevicesChanged(bool value) => PresentDrivers();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void OpenWindowsUpdate() => _links.OpenUrl(new Uri("ms-settings:windowsupdate"));

    [RelayCommand]
    private void OpenSource(DriverRow? row)
    {
        if (row?.SourceUrl is { } url)
        {
            _links.OpenUrl(url);
        }
    }

    [RelayCommand]
    private async Task CheckNowAsync()
    {
        IsChecking = true;
        CheckResult = Strings.Health_Checking;
        AvailableUpdates.Clear();

        try
        {
            var titles = await _updates.CheckOnlineAsync();

            foreach (var title in titles)
            {
                AvailableUpdates.Add(title);
            }

            CheckResult = titles.Count == 0 ? Strings.Health_CheckResultNone : string.Format(CultureInfo.CurrentCulture, Strings.Health_CheckResult, titles.Count);
        }
        catch (InvalidOperationException exception)
        {
            CheckResult = string.Format(CultureInfo.CurrentCulture, Strings.Health_CheckFailed, exception.Message);
        }
        finally
        {
            IsChecking = false;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var version = await Task.Run(WindowsVersionReader.Read);
            PresentVersion(version);

            var restart = await Task.Run(PendingRestartReader.Read);
            PresentRestart(PendingRestartEvaluator.Evaluate(restart));

            var (uefi, secureBoot) = await Task.Run(() => (FirmwareReader.IsUefi(), FirmwareReader.SecureBootEnabled()));
            FirmwareText = uefi switch { true => Strings.Health_FirmwareUefi, false => Strings.Health_FirmwareBios, null => Strings.NotAvailable };
            SecureBootText = secureBoot switch { true => Strings.Health_On, false => Strings.Health_Off, null => Strings.NotAvailable };

            _drivers = await Task.Run(DriverInventory.Read);
            PresentDrivers();

            PresentUpdates(await _updates.ReadAsync());
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _logger.LogError(exception, "The health facts could not be read.");
        }
    }

    private void PresentVersion(WindowsVersion version)
    {
        var culture = CultureInfo.CurrentCulture;
        WindowsName = version.DisplayName;
        VersionText = version.DisplayVersion ?? Strings.NotAvailable;
        BuildText = version.BuildString;
        InstalledText = version.InstalledAt?.ToLocalTime().ToString("D", culture) ?? Strings.NotAvailable;

        var status = ServicingTable.Evaluate(version.Build, version.EditionId, DateOnly.FromDateTime(DateTime.Today));
        var editions = status.Release?.LongTermServicing == true ? Strings.Health_LongTermEditions
            : status.UsesEnterpriseDates ? Strings.Health_EnterpriseEditions : Strings.Health_ConsumerEditions;
        var dataDate = ServicingTable.DataAsOf.ToString("D", culture);

        ServicingText = status.State switch
        {
            ServicingState.Supported when status.EndOfServicing is { } end => string.Format(culture, Strings.Health_SupportedUntil, end.ToString("D", culture), editions, AppInfo.Name + " " + AppInfo.Version, dataDate),
            ServicingState.Ended when status.EndOfServicing is { } end => string.Format(culture, Strings.Health_SupportEnded, end.ToString("D", culture), editions, AppInfo.Name + " " + AppInfo.Version, dataDate),
            _ => string.Format(culture, Strings.Health_SupportUnknown, AppInfo.Name + " " + AppInfo.Version, dataDate),
        };
        SupportEnded = status.State == ServicingState.Ended;
    }

    private void PresentRestart(IReadOnlyList<PendingRestartSignal> signals)
    {
        RestartSignals.Clear();

        foreach (var signal in signals)
        {
            RestartSignals.Add(signal switch
            {
                PendingRestartSignal.WindowsUpdate => Strings.Health_SignalWindowsUpdate,
                PendingRestartSignal.ComponentServicing => Strings.Health_SignalServicing,
                PendingRestartSignal.FileRenames => Strings.Health_SignalRenames,
                PendingRestartSignal.ComputerRename => Strings.Health_SignalRename,
                _ => Strings.Health_SignalUpdateInProgress,
            });
        }

        RestartPending = PendingRestartEvaluator.IsRestartPending(signals);
        RestartText = RestartPending ? Strings.Health_RestartPending : Strings.Health_RestartNotPending;
    }

    private void PresentUpdates(UpdateFacts facts)
    {
        var culture = CultureInfo.CurrentCulture;
        var now = DateTimeOffset.Now;
        LastCheckText = facts.LastDetect is { } detect ? $"{detect.ToLocalTime().ToString("g", culture)} ({Durations.FormatRelative(detect, now)})" : Strings.NotAvailable;
        LastInstallText = facts.LastInstall is { } install ? $"{install.ToLocalTime().ToString("g", culture)} ({Durations.FormatRelative(install, now)})" : Strings.NotAvailable;

        History.Clear();

        foreach (var entry in facts.History.OrderByDescending(entry => entry.Date).Take(HistoryRows))
        {
            History.Add(new UpdateRow(entry.Title, entry.Date.ToLocalTime().ToString("d", culture), !entry.Succeeded));
        }

        HasHistory = History.Count > 0;

        if (facts.RebootRequired == true && !RestartPending)
        {
            RestartPending = true;
            RestartText = Strings.Health_RestartPending;
            RestartSignals.Add(Strings.Health_SignalWindowsUpdate);
        }
    }

    private void PresentDrivers()
    {
        var culture = CultureInfo.CurrentCulture;
        Drivers.Clear();

        var rows = _drivers
            .Where(driver => ShowAllDevices || DeviceClasses.IsHighlighted(driver.Group))
            .GroupBy(driver => (driver.InfPath ?? driver.Device, driver.Version))
            .Select(group => group.First())
            .OrderBy(driver => driver.Group)
            .ThenBy(driver => driver.Device, StringComparer.CurrentCultureIgnoreCase);

        foreach (var driver in rows)
        {
            var source = driver.Source;
            var dateText = driver.Date is { } date
                ? $"{date.ToString("d", culture)} ({Durations.FormatRelative(new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), DateTimeOffset.Now)})"
                : Strings.NotAvailable;

            Drivers.Add(new DriverRow(
                driver.Device,
                GroupName(driver.Group),
                driver.Provider ?? Strings.NotAvailable,
                driver.DisplayVersion.Length == 0 ? Strings.NotAvailable : driver.DisplayVersion,
                dateText,
                driver.Problem,
                source is null ? null : string.Format(culture, Strings.Health_DriverLink, source.Vendor),
                source is null ? null : string.Format(culture, Strings.Health_DriverLinkTooltip, source.Url.IsAbsoluteUri && source.Url.Scheme != "ms-settings" ? source.Url.Host : source.Vendor),
                source?.Url));
        }

        HasDrivers = Drivers.Count > 0;
    }

    private static string GroupName(DeviceGroup group) => group switch
    {
        DeviceGroup.Display => Strings.DeviceGroup_Display,
        DeviceGroup.Network => Strings.DeviceGroup_Network,
        DeviceGroup.Audio => Strings.DeviceGroup_Audio,
        DeviceGroup.Storage => Strings.DeviceGroup_Storage,
        DeviceGroup.Chipset => Strings.DeviceGroup_Chipset,
        DeviceGroup.Bluetooth => Strings.DeviceGroup_Bluetooth,
        DeviceGroup.Usb => Strings.DeviceGroup_Usb,
        DeviceGroup.Firmware => Strings.DeviceGroup_Firmware,
        _ => Strings.DeviceGroup_Other,
    };

    private void OnSample(object? sender, SystemSample sample)
    {
        var volume = sample.Volumes.FirstOrDefault(volume => volume.IsSystemVolume);

        if (volume is not null)
        {
            SystemDriveText = string.Format(CultureInfo.CurrentCulture, Strings.Overview_FreeOfTotal, ByteSize.Format(volume.Free), ByteSize.Format(volume.Total));
        }

        var booted = DateTimeOffset.Now - sample.Uptime;
        LastRestartText = $"{booted.ToString("g", CultureInfo.CurrentCulture)} ({Durations.Format(sample.Uptime)})";
    }
}
