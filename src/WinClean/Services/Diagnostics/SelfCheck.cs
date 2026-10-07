using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using WinClean.Core.Formatting;
using WinClean.Core.Settings;
using WinClean.Services.Monitoring;

namespace WinClean.Services.Diagnostics;

/// <summary>
/// Headless diagnostics behind the --self-check switch. Every system reader is exercised once and the
/// outcome is written as one line per check. CI runs this on a Windows runner, which is the only place
/// the native layer can run before a release; users can run it to see why a value is not available.
/// </summary>
internal static class SelfCheck
{
    public const int ExitOk = 0;

    public const int ExitFailed = 1;

    public const int ExitDegraded = 2;

    public static int Run(StartupOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var report = new StringBuilder();
        report.AppendLine(CultureInfo.InvariantCulture, $"{AppInfo.Name} {AppInfo.Version} self-check");
        report.AppendLine(CultureInfo.InvariantCulture, $"{DateTimeOffset.UtcNow:u}  {RuntimeInformation.OSDescription}  {RuntimeInformation.ProcessArchitecture}  elevated: {ProcessContext.IsElevated}");
        report.AppendLine();

        var worst = CheckStatus.Ok;

        foreach (var item in Checks())
        {
            var stopwatch = Stopwatch.StartNew();
            CheckResult result;

            try
            {
                result = item.Run();
            }
            catch (Exception exception)
            {
                result = CheckResult.Failed($"{exception.GetType().Name}: {exception.Message}");
            }

            stopwatch.Stop();
            report.AppendLine(CultureInfo.InvariantCulture, $"{Label(result.Status),-9}{stopwatch.ElapsedMilliseconds,6} ms  {item.Name}: {result.Message}");

            if (result.Status > worst)
            {
                worst = result.Status;
            }
        }

        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"Result: {Label(worst)}");

        Console.Write(report);

        if (options.ReportPath is not null)
        {
            var path = Path.GetFullPath(options.ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, report.ToString());
        }

        return worst switch
        {
            CheckStatus.Ok => ExitOk,
            CheckStatus.Degraded => ExitDegraded,
            _ => ExitFailed,
        };
    }

    private static IEnumerable<SelfCheckItem> Checks()
    {
        yield return new SelfCheckItem("Settings round trip", CheckSettingsRoundTrip);
        yield return new SelfCheckItem("System metrics", CheckSystemMetrics);
    }

    private static CheckResult CheckSystemMetrics()
    {
        using var scheduler = new MonitoringScheduler(NullLogger<MonitoringScheduler>.Instance);
        var sample = scheduler.SampleOnce();
        var summary = new StringBuilder();

        summary.Append(CultureInfo.InvariantCulture, $"cpu {Percent.Format(sample.CpuPercent ?? double.NaN, 0)}");

        if (sample.Memory is { } memory)
        {
            summary.Append(CultureInfo.InvariantCulture, $", memory {ByteSize.Format(memory.Used)} of {ByteSize.Format(memory.Total)}");
        }

        summary.Append(CultureInfo.InvariantCulture, $", {sample.Volumes.Count} volumes, uptime {Durations.Format(sample.Uptime)}");
        summary.Append(sample.Battery is null ? ", no battery" : ", battery present");

        var unavailable = scheduler.Statuses
            .Where(status => !status.Available && status.Reason is not null)
            .Select(status => $"{status.Name}: {status.Reason}")
            .ToList();

        if (unavailable.Count == 0 || sample.CpuPercent is null || sample.Memory is null)
        {
            return sample.CpuPercent is null || sample.Memory is null
                ? CheckResult.Failed(string.Join("; ", unavailable))
                : CheckResult.Ok(summary.ToString());
        }

        return CheckResult.Degraded($"{summary}; not available: {string.Join("; ", unavailable)}");
    }

    private static CheckResult CheckSettingsRoundTrip()
    {
        var directory = Path.Combine(Path.GetTempPath(), "WinClean-self-check-" + Environment.ProcessId);
        Directory.CreateDirectory(directory);

        try
        {
            var location = new SettingsLocation(directory, IsPortable: true);
            var expected = new AppSettings { Theme = ThemePreference.Dark, RefreshIntervalSeconds = 2 };

            File.WriteAllText(location.SettingsFile, SettingsSerializer.Serialize(expected));
            var loaded = SettingsSerializer.Deserialize(File.ReadAllText(location.SettingsFile));

            return loaded.Error is null && loaded.Settings == expected
                ? CheckResult.Ok("settings.json written and read back")
                : CheckResult.Failed(loaded.Error ?? "the settings read back differ from the ones written");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string Label(CheckStatus status) => status switch
    {
        CheckStatus.Ok => "OK",
        CheckStatus.Degraded => "DEGRADED",
        _ => "FAIL",
    };
}
