using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WinClean.Core.Settings;
using WinClean.Dialogs;
using WinClean.Resources;
using WinClean.Services;
using WinClean.Services.Diagnostics;
using WinClean.Services.Logging;
using WinClean.Services.Monitoring;
using WinClean.Services.Processes;
using WinClean.Services.Shell;
using WinClean.ViewModels;

namespace WinClean;

public partial class App : Application
{
    private readonly StartupOptions _options;

    private SingleInstance? _instance;

    private ServiceProvider? _services;

    private bool _reportingCrash;

    public App(StartupOptions options)
    {
        _options = options;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _instance = SingleInstance.Acquire(_options.ReplacePid);

        if (!_instance.IsFirst)
        {
            SingleInstance.ActivateRunningInstance(_options.Page);
            Shutdown();
            return;
        }

        var location = DataLocation.Detect();
        _services = BuildServices(location);

        var settings = _services.GetRequiredService<SettingsStore>();
        settings.Load();
        ApplySettings(settings.Current);
        settings.Changed += (_, current) => ApplySettings(current);

        _services.GetRequiredService<ILogger<App>>().LogInformation(
            "{Name} {Version} starting; {Mode} data folder {Directory}",
            AppInfo.Name,
            AppInfo.Version,
            location.IsPortable ? "portable" : "per-user",
            location.DataDirectory);

        var shell = _services.GetRequiredService<ShellViewModel>();
        shell.IsCompact = settings.Current.CompactNavigation;
        shell.NavigateTo(_options.Page);

        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }

    private void ApplySettings(AppSettings current)
    {
        var logging = _services!.GetRequiredService<FileLoggerProvider>();
        logging.MinimumLevel = current.VerboseLogging ? LogLevel.Debug : LogLevel.Information;
        ThemeResources.Apply(this, current.Theme);
    }

    private ServiceProvider BuildServices(SettingsLocation location)
    {
        var services = new ServiceCollection();

        services.AddSingleton(_options);
        services.AddSingleton(location);
        services.AddSingleton(_ => new FileLoggerProvider(location.LogDirectory));
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .Services.AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<FileLoggerProvider>()));
        services.AddSingleton<SettingsStore>();
        services.AddSingleton<CrashReporter>();
        services.AddSingleton<ShellLinks>();
        services.AddSingleton<ProcessIconCache>();
        services.AddSingleton<ProcessDetailsCache>();
        services.AddSingleton<ProcessActions>();
        services.AddSingleton<MonitoringScheduler>();
        services.AddSingleton<MonitoringCoordinator>();

        services.AddSingleton<OverviewViewModel>();
        services.AddSingleton<ProcessesViewModel>();
        services.AddSingleton<StorageViewModel>();
        services.AddSingleton<CleanupViewModel>();
        services.AddSingleton<HealthViewModel>();
        services.AddSingleton<HardwareViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        ReportCrash(e.Exception, "the UI thread");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            WriteCrash(exception, "a background thread");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        WriteCrash(e.Exception, "a background task");
    }

    private string? WriteCrash(Exception exception, string origin)
    {
        return _services?.GetService<CrashReporter>()?.Write(exception, origin);
    }

    private void ReportCrash(Exception exception, string origin)
    {
        var path = WriteCrash(exception, origin);

        if (_reportingCrash)
        {
            return;
        }

        _reportingCrash = true;

        try
        {
            if (_services is null)
            {
                MessageBox.Show(CrashReporter.Describe(exception, origin), AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var message = path is null
                ? Strings.Error_MessageNoFile
                : string.Format(CultureInfo.CurrentCulture, Strings.Error_Message, path);
            var viewModel = new ErrorReportViewModel(
                message,
                CrashReporter.Describe(exception, origin),
                _services.GetRequiredService<SettingsLocation>().LogDirectory,
                _services.GetRequiredService<ShellLinks>());
            var dialog = new ErrorReportDialog(viewModel);

            if (MainWindow is { IsLoaded: true } owner)
            {
                dialog.Owner = owner;
            }

            dialog.ShowDialog();
        }
        finally
        {
            _reportingCrash = false;
        }
    }
}
