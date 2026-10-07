using WinClean.Services;
using WinClean.Services.Diagnostics;

namespace WinClean;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var options = StartupOptions.Parse(args);

        if (options.ShowVersion)
        {
            ConsoleOutput.Attach();
            Console.WriteLine($"{AppInfo.Name} {AppInfo.Version}");
            return 0;
        }

        if (options.SelfCheck)
        {
            ConsoleOutput.Attach();
            return SelfCheck.Run(options);
        }

        var app = new App(options);
        app.InitializeComponent();
        return app.Run();
    }
}
