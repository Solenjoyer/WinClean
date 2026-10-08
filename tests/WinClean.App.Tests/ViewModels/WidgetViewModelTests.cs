using WinClean.Core.Applications;
using WinClean.Core.Formatting;
using WinClean.Core.Monitoring;
using WinClean.Core.Settings;
using WinClean.Resources;
using WinClean.Services.Monitoring;
using WinClean.ViewModels;

namespace WinClean.App.Tests.ViewModels;

public class WidgetViewModelTests
{
    private const long Megabyte = 1024L * 1024;

    private const long Gigabyte = 1024L * Megabyte;

    private static ProcessSample Process(int pid, string name, double cpu, long memory)
    {
        var facts = new ProcessFacts(pid, 100, 1, name, @"C:\Apps\" + name, null, null, 1, false);
        return new ProcessSample(facts, cpu, 0, memory, memory, memory, 4, 40, 0, 0, 0, false, true, false);
    }

    private static SystemSample Sample(bool withProcesses)
    {
        var processes = new[]
        {
            Process(1, "node.exe", 9.6, 812 * Megabyte),
            Process(2, "python.exe", 1.4, 142 * Megabyte),
            Process(3, "chrome.exe", 2.8, 1900 * Megabyte),
            Process(4, "com.docker.backend.exe", 1.3, 3000 * Megabyte),
        };
        var groups = new[]
        {
            new ProcessGroup("app:chrome", "Google Chrome", ApplicationCategory.Browser, null, [3]),
            new ProcessGroup("app:docker", "Docker Desktop", ApplicationCategory.Container, null, [4]),
            new ProcessGroup("app:claude-code", "Claude Code", ApplicationCategory.AiAgent, null, [1, 2]),
        };
        var snapshot = new ProcessSnapshot(processes, new GroupingResult(groups, new Dictionary<int, string>()), new Dictionary<int, IReadOnlyList<string>>());
        var memory = new MemorySample(32 * Gigabyte, 17 * Gigabyte, 32 * Gigabyte, 0, 0, 0, 0, 0, 150, 2000, 50000);
        var volume = new VolumeSample(@"C:\", "Windows", "NTFS", VolumeKind.Fixed, 931 * Gigabyte, 412 * Gigabyte, IsSystemVolume: true);

        return new SystemSample(
            DateTimeOffset.UtcNow,
            1,
            23.4,
            null,
            4520,
            12,
            memory,
            [volume],
            new DiskActivitySample(40_000_000, 8_000_000, 9, 1),
            new NetworkSample(12_000_000, 1_800_000, []),
            null,
            [],
            TimeSpan.FromHours(5),
            withProcesses ? snapshot : null);
    }

    [Fact]
    public void Apply_FormatsTheFiguresAndListsTheToolsByCpu()
    {
        var viewModel = new WidgetViewModel(new MetricHistory(), new AppSettings());

        viewModel.Apply(Sample(withProcesses: true));

        Assert.Equal("23%", viewModel.CpuValue);
        Assert.Equal(ByteSize.Format(15 * Gigabyte, ByteSize.UnitFor(32 * Gigabyte)), viewModel.MemoryValue);
        Assert.Equal(Rate.Format(48_000_000), viewModel.DiskValue);
        Assert.Equal(Rate.Format(13_800_000), viewModel.NetworkValue);
        Assert.False(viewModel.GpuVisible);
        Assert.False(viewModel.TemperatureVisible);
        Assert.True(viewModel.StorageVisible);
        Assert.Equal(string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings.Widget_FreeSpace, "C:", ByteSize.Format(412 * Gigabyte)), viewModel.StorageText);

        Assert.True(viewModel.ToolsVisible);
        Assert.Equal(2, viewModel.Tools.Count);
        Assert.Equal("Claude Code", viewModel.Tools[0].Name);
        Assert.Equal("11.0%", viewModel.Tools[0].CpuText);
        Assert.Equal(ByteSize.Format(954 * Megabyte), viewModel.Tools[0].MemoryText);
        Assert.Equal("Docker Desktop", viewModel.Tools[1].Name);
    }

    [Fact]
    public void Apply_WithoutProcessesOrWithToolsOff_HidesTheList()
    {
        var viewModel = new WidgetViewModel(new MetricHistory(), new AppSettings());
        viewModel.Apply(Sample(withProcesses: false));
        Assert.False(viewModel.ToolsVisible);

        var settings = new AppSettings { Widget = new WidgetSettings { ShowsTools = false, Layout = WidgetLayout.Compact, Opacity = 75 } };
        var compact = new WidgetViewModel(new MetricHistory(), settings);
        compact.Apply(Sample(withProcesses: true));

        Assert.False(compact.ToolsVisible);
        Assert.True(compact.IsCompact);
        Assert.Equal(0.75, compact.Opacity);
        Assert.Contains("23%", compact.CompactText, StringComparison.Ordinal);
    }
}
