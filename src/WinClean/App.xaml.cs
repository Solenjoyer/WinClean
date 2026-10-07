using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WinClean.Core.Settings;
using WinClean.Services;
using WinClean.Services.Logging;
using WinClean.Services.Shell;
using WinClean.ViewModels;

namespace WinClean;

public partial class App : Application
{
    private readonly StartupOptions _options;

    private ServiceProvider? _services;

    public App(StartupOptions options)
    {
        _options = options;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
}
