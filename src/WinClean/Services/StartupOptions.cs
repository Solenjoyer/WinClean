namespace WinClean.Services;

/// <summary>Command-line switches. All of them are optional; a plain launch opens the window.</summary>
public sealed record StartupOptions
{
    public bool StartMinimized { get; init; }

    /// <summary>Page to open first, as a navigation key such as "cleanup".</summary>
    public string? Page { get; init; }

    public bool ShowVersion { get; init; }

    public bool SelfCheck { get; init; }

    public string? ReportPath { get; init; }

    public bool IncludeSensors { get; init; }

    /// <summary>Set by "Restart as administrator" so the elevated instance knows it replaces another one.</summary>
    public bool Elevated { get; init; }

    public int? ReplacePid { get; init; }

    /// <summary>Opens the desktop widget without the main window, and turns the widget on in the settings.</summary>
    public bool Widget { get; init; }

    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var options = new StartupOptions();
        var index = 0;

        while (index < args.Count)
        {
            var argument = args[index++];
            var separator = argument.IndexOf('=', StringComparison.Ordinal);
            var name = separator < 0 ? argument : argument[..separator];
            var inlineValue = separator < 0 ? null : argument[(separator + 1)..];

            string? TakeValue()
            {
                if (inlineValue is not null)
                {
                    return inlineValue;
                }

                return index < args.Count ? args[index++] : null;
            }

            switch (name.ToLowerInvariant())
            {
                case "--minimized":
                    options = options with { StartMinimized = true };
                    break;
                case "--page":
                    options = options with { Page = TakeValue() };
                    break;
                case "--version":
                    options = options with { ShowVersion = true };
                    break;
                case "--self-check":
                    options = options with { SelfCheck = true };
                    break;
                case "--report":
                    options = options with { ReportPath = TakeValue() };
                    break;
                case "--include-sensors":
                    options = options with { IncludeSensors = true };
                    break;
                case "--elevated":
                    options = options with { Elevated = true };
                    break;
                case "--replace-pid":
                    options = options with { ReplacePid = int.TryParse(TakeValue(), out var pid) ? pid : null };
                    break;
                case "--widget":
                    options = options with { Widget = true };
                    break;
                default:
                    // Unknown switches are ignored on purpose: an old shortcut must not stop the app from starting.
                    break;
            }
        }

        return options;
    }
}
