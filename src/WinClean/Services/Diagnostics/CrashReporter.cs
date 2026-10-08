using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using WinClean.Core.Settings;

namespace WinClean.Services.Diagnostics;

/// <summary>Writes a crash report next to the logs. Nothing is sent anywhere; the user decides what to do with it.</summary>
internal sealed class CrashReporter
{
    private readonly SettingsLocation _location;

    private readonly ILogger<CrashReporter> _logger;

    public CrashReporter(SettingsLocation location, ILogger<CrashReporter> logger)
    {
        _location = location;
        _logger = logger;
    }

    public static string Describe(Exception exception, string origin)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{AppInfo.Name} {AppInfo.Version}")
            .AppendLine(CultureInfo.InvariantCulture, $"{RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}, elevated: {ProcessContext.IsElevated}")
            .AppendLine(CultureInfo.InvariantCulture, $"{DateTimeOffset.Now:u}, on {origin}")
            .AppendLine()
            .Append(exception)
            .AppendLine()
            .ToString();
    }

    /// <summary>Returns the report path, or null when the data folder is not writable.</summary>
    public string? Write(Exception exception, string origin)
    {
        _logger.LogCritical(exception, "Unhandled exception on {Origin}.", origin);

        try
        {
            Directory.CreateDirectory(_location.LogDirectory);
            var path = Path.Combine(_location.LogDirectory, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, Describe(exception, origin));
            return path;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
