using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WinClean.Core.Settings;
using WinClean.Services;
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

        _services = BuildServices();
        ThemeResources.Apply(this, ThemePreference.System);

        var shell = _services.GetRequiredService<ShellViewModel>();
        shell.NavigateTo(_options.Page);

        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_options);

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
