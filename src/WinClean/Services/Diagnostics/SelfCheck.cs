using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using WinClean.Core.Formatting;
using WinClean.Core.Health;
using WinClean.Core.Settings;
using WinClean.Core.Storage;
using WinClean.Services.Cleanup;
using WinClean.Services.Hardware;
using WinClean.Services.Health;
using WinClean.Services.Monitoring;
using WinClean.Services.Processes;
using WinClean.Services.Shell;
using WinClean.Services.Storage;

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

        foreach (var item in Checks(options))
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

    private static IEnumerable<SelfCheckItem> Checks(StartupOptions options)
    {
        yield return new SelfCheckItem("Settings round trip", CheckSettingsRoundTrip);
        yield return new SelfCheckItem("System metrics", CheckSystemMetrics);
        yield return new SelfCheckItem("Process list", CheckProcessList);
        yield return new SelfCheckItem("Windows version", CheckWindowsVersion);
        yield return new SelfCheckItem("Update facts", CheckUpdateFacts);
        yield return new SelfCheckItem("Driver inventory", CheckDrivers);
        yield return new SelfCheckItem("Hardware facts", CheckHardware);
        yield return new SelfCheckItem("Storage scan", CheckStorageScan);
        yield return new SelfCheckItem("Known locations", CheckKnownLocations);
        yield return new SelfCheckItem("Cleanup discovery (dry run)", CheckCleanupDiscovery);
        yield return new SelfCheckItem("Notification area icon", CheckTrayIcon);

        if (options.IncludeSensors)
        {
            yield return new SelfCheckItem("Sensors", CheckSensors);
        }
    }

    private static CheckResult CheckWindowsVersion()
    {
        var version = WindowsVersionReader.Read();
        var status = ServicingTable.Evaluate(version.Build, version.EditionId, DateOnly.FromDateTime(DateTime.Today));
        var restart = PendingRestartEvaluator.Evaluate(PendingRestartReader.Read());
        var summary = $"{version.DisplayName} {version.DisplayVersion} build {version.BuildString}, {status.State}, restart signals: {(restart.Count == 0 ? "none" : string.Join(", ", restart))}, firmware {(FirmwareReader.IsUefi() == true ? "UEFI" : "BIOS or unknown")}";
        return version.Build == 0 ? CheckResult.Failed(summary) : CheckResult.Ok(summary);
    }

    private static CheckResult CheckUpdateFacts()
    {
        var reader = new WindowsUpdateReader(NullLogger<WindowsUpdateReader>.Instance);
        var facts = reader.ReadAsync().GetAwaiter().GetResult();
        var summary = $"last check {facts.LastDetect?.ToString("u", CultureInfo.InvariantCulture) ?? "unknown"}, last install {facts.LastInstall?.ToString("u", CultureInfo.InvariantCulture) ?? "unknown"}, {facts.History.Count} history entries, reboot required: {facts.RebootRequired?.ToString() ?? "unknown"}";
        return facts.History.Count == 0 && facts.RebootRequired is null ? CheckResult.Degraded(summary + "; the Windows Update Agent did not answer") : CheckResult.Ok(summary);
    }

    private static CheckResult CheckDrivers()
    {
        var drivers = DriverInventory.Read();
        var highlighted = drivers.Count(driver => DeviceClasses.IsHighlighted(driver.Group));
        var problems = drivers.Count(driver => driver.ProblemCode != 0);
        var summary = $"{drivers.Count} devices, {highlighted} in the highlighted classes, {problems} with a problem code";
        return drivers.Count == 0 ? CheckResult.Failed(summary) : CheckResult.Ok(summary);
    }

    private static CheckResult CheckHardware()
    {
        var smbios = SmbiosReader.Read();
        var processor = ProcessorReader.Read();
        var gpus = GpuReader.Read();
        var disks = PhysicalDiskReader.Read(DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed).Select(drive => drive.RootDirectory.FullName));
        var displays = DisplayReader.Read();
        var adapters = NetworkAdapterReader.Read();
        var summary = $"SMBIOS {smbios.SpecificationVersion} with {smbios.MemoryDevices.Count} memory devices; {processor.Name} ({processor.Topology.Cores} cores, {processor.Topology.LogicalProcessors} threads, {processor.Topology.Caches.Count} cache levels); {gpus.Count} GPUs; {disks.Count} disks; {displays.Count} displays; {adapters.Count} network adapters; TPM {TpmReader.Version() ?? "none"}";

        if (processor.Topology.LogicalProcessors == 0)
        {
            return CheckResult.Failed(summary);
        }

        return smbios.System is null || disks.Count == 0 ? CheckResult.Degraded(summary) : CheckResult.Ok(summary);
    }

    private static CheckResult CheckStorageScan()
    {
        var scanner = new StorageScanner(NullLogger<StorageScanner>.Instance);
        var target = Path.Combine(Environment.SystemDirectory, "drivers");
        var result = scanner.ScanAsync(target, 4, null, CancellationToken.None).GetAwaiter().GetResult();
        var summary = $"{target}: {result.Tree.Count} folders, {result.TotalFiles} files, {result.TotalBytes} bytes, {result.LargestFiles.Count} largest kept, {result.Categories.Count} categories, {result.DeniedFolders} denied, {result.Duration.TotalMilliseconds:F0} ms";
        return result.TotalFiles == 0 ? CheckResult.Failed(summary) : CheckResult.Ok(summary);
    }

    private static CheckResult CheckKnownLocations()
    {
        var sizer = new KnownLocationSizer();
        var present = KnownLocations.All.Select(location => (location, paths: sizer.Expand(location))).Where(pair => pair.paths.Count > 0).ToList();
        var temp = sizer.Measure(KnownLocations.Find("user-temp")!, CancellationToken.None);
        var summary = $"{present.Count} of {KnownLocations.All.Count} locations present; temporary files: {temp.Files} files, {temp.Bytes} bytes; Recycle Bin: {RecycleBinReader.Query(Path.GetPathRoot(Environment.SystemDirectory)!)?.Items.ToString(CultureInfo.InvariantCulture) ?? "not answered"} items";
        return temp.Exists ? CheckResult.Ok(summary) : CheckResult.Failed(summary);
    }

    private static CheckResult CheckCleanupDiscovery()
    {
        var discovery = new CleanupDiscovery(new KnownLocationSizer(), new DockerCli(NullLogger<DockerCli>.Instance), new ScanResults(), NullLogger<CleanupDiscovery>.Instance);
        var categories = discovery.DiscoverAsync(new Core.Settings.AppSettings(), null, CancellationToken.None).GetAwaiter().GetResult();
        var items = categories.Sum(category => category.Count);
        var bytes = categories.Sum(category => category.Bytes);
        var summary = $"{categories.Count} categories, {items} items, {bytes} bytes; nothing was deleted";
        return categories.Count == 0 ? CheckResult.Failed(summary) : CheckResult.Ok(summary);
    }

    private static CheckResult CheckTrayIcon()
    {
        var size = Native.User32.GetSystemMetricsForDpi(Native.User32.SM_CXSMICON, Native.User32.GetDpiForSystem());
        var bars = TrayIconRenderer.RenderBars([23, 61, 44], size, darkTaskbar: true);
        var number = TrayIconRenderer.RenderNumber(42, size, darkTaskbar: false);
        var icon = Native.User32.CreateIconFromResourceEx(bars, (uint)bars.Length, true, 0x00030000, 0, 0, 0);

        if (icon == 0)
        {
            return CheckResult.Failed($"a {size} px icon could not be created from {bars.Length} PNG bytes: {Win32Reason.LastError()}");
        }

        Native.User32.DestroyIcon(icon);
        return CheckResult.Ok($"{size} px icons rendered: bars {bars.Length} bytes, number {number.Length} bytes");
    }

    private static CheckResult CheckSensors()
    {
        var settings = new SettingsStore(new SettingsLocation(Path.GetTempPath(), IsPortable: true), NullLogger<SettingsStore>.Instance);
        settings.Load();
        settings.Update(current => current with { SensorsEnabled = true });
        using var provider = new SensorProvider(settings, NullLogger<SensorProvider>.Instance);
        provider.Start();
        Thread.Sleep(8000);
        var readings = provider.Readings;
        var summary = $"{readings.Count} readings; {provider.Reason ?? "all sensor groups available"}";
        return readings.Count == 0 ? CheckResult.Degraded(summary) : CheckResult.Ok(summary);
    }

    private static CheckResult CheckProcessList()
    {
        using var details = new ProcessDetailsCache(new ProcessIconCache(), NullLogger<ProcessDetailsCache>.Instance);
        using var scheduler = new MonitoringScheduler(NullLogger<MonitoringScheduler>.Instance, details);
        var snapshot = scheduler.SampleOnce(processes: true).Processes;

        if (snapshot is null)
        {
            return CheckResult.Failed(scheduler.Statuses.First(status => status.Name == "Processes").Reason ?? "no snapshot");
        }

        var self = snapshot.Processes.FirstOrDefault(process => process.Pid == Environment.ProcessId);

        if (self is null)
        {
            return CheckResult.Failed($"{snapshot.Processes.Count} processes listed, but not this one");
        }

        var withPath = snapshot.Processes.Count(process => process.Facts.Path is not null);
        var withWindow = snapshot.Processes.Count(process => process.HasWindow);
        var summary = $"{snapshot.Processes.Count} processes in {snapshot.Grouping.Groups.Count} groups, {withPath} with a path, {withWindow} with windows; this one is {self.Name} at {self.Facts.Path ?? "an unknown path"}";

        return self.Facts.Path is null ? CheckResult.Degraded(summary) : CheckResult.Ok(summary);
    }

    private static CheckResult CheckSystemMetrics()
    {
        using var details = new ProcessDetailsCache(new ProcessIconCache(), NullLogger<ProcessDetailsCache>.Instance);
        using var scheduler = new MonitoringScheduler(NullLogger<MonitoringScheduler>.Instance, details);
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
