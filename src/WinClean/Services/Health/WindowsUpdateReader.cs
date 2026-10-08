using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinClean.Core.Health;

namespace WinClean.Services.Health;

/// <summary>
/// Update facts from the registry and the local history of the Windows Update Agent. Only "Check now"
/// goes online, and only when the user clicks it; it asks the agent exactly what the Settings app asks.
/// </summary>
public sealed class WindowsUpdateReader
{
    private const string ResultsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\Results";

    private const int HistoryEntries = 25;

    private readonly ILogger<WindowsUpdateReader> _logger;

    public WindowsUpdateReader(ILogger<WindowsUpdateReader> logger)
    {
        _logger = logger;
    }

    public Task<UpdateFacts> ReadAsync() => Task.Run(Read);

    /// <summary>Titles of the updates Microsoft Update reports as not yet installed.</summary>
    public Task<IReadOnlyList<string>> CheckOnlineAsync() => Task.Run(CheckOnline);

    private UpdateFacts Read()
    {
        var detect = LastSuccess("Detect");
        var download = LastSuccess("Download");
        var install = LastSuccess("Install");
        var history = new List<UpdateHistoryEntry>();
        bool? rebootRequired = null;

        try
        {
            dynamic session = CreateComObject("Microsoft.Update.Session");
            dynamic searcher = session.CreateUpdateSearcher();
            int count = searcher.GetTotalHistoryCount();

            if (count > 0)
            {
                dynamic entries = searcher.QueryHistory(0, Math.Min(count, HistoryEntries));

                for (var index = 0; index < entries.Count; index++)
                {
                    dynamic entry = entries.Item(index);
                    string title = entry.Title ?? string.Empty;
                    DateTime date = entry.Date;
                    int result = entry.ResultCode;
                    history.Add(new UpdateHistoryEntry(title, new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)), result));
                }
            }

            dynamic systemInfo = CreateComObject("Microsoft.Update.SystemInfo");
            rebootRequired = (bool)systemInfo.RebootRequired;
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException or InvalidOperationException)
        {
            _logger.LogInformation(exception, "The Windows Update Agent history could not be read.");
        }

        return new UpdateFacts(detect, download, install, history, rebootRequired);
    }

    private IReadOnlyList<string> CheckOnline()
    {
        var titles = new List<string>();

        try
        {
            dynamic session = CreateComObject("Microsoft.Update.Session");
            dynamic searcher = session.CreateUpdateSearcher();
            searcher.Online = true;
            dynamic result = searcher.Search("IsInstalled=0 and IsHidden=0");
            dynamic updates = result.Updates;

            for (var index = 0; index < updates.Count; index++)
            {
                titles.Add((string)updates.Item(index).Title);
            }
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "The online update check failed.");
            throw new InvalidOperationException(exception.Message, exception);
        }

        return titles;
    }

    private static object CreateComObject(string progId)
    {
        var type = Type.GetTypeFromProgID(progId, throwOnError: true)!;
        return Activator.CreateInstance(type) ?? throw new InvalidOperationException(progId + " could not be created.");
    }

    private static DateTimeOffset? LastSuccess(string phase)
    {
        using var key = Registry.LocalMachine.OpenSubKey(ResultsKey + "\\" + phase);

        if (key?.GetValue("LastSuccessTime") is string text
            && DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return new DateTimeOffset(parsed, TimeSpan.Zero);
        }

        return null;
    }
}
