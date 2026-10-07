using WinClean.Core.Applications;
using WinClean.Core.Monitoring;
using WinClean.ViewModels;

namespace WinClean.App.Tests.ViewModels;

public class ProcessRowTests
{
    private static ProcessSample Sample(int pid, string name, double cpu, long memory, double io, bool suspended = false, bool hung = false)
    {
        var facts = new ProcessFacts(pid, 100, 1, name, @"C:\Apps\" + name, null, null, 1, false);
        return new ProcessSample(facts, cpu, 0, memory, memory, memory, 4, 40, io, 0, 0, suspended, true, hung);
    }

    [Fact]
    public void UpdateProcess_FormatsTheColumnsAndStatus()
    {
        var row = new ProcessRow("p:1", isGroup: false);
        row.UpdateProcess(Sample(1234, "Code.exe", 12.34, 1_300_000_000, 500), null, "Code.exe", "Editor", isChild: true);

        Assert.Equal("1234", row.PidText);
        Assert.Equal("12.3%", row.CpuText);
        Assert.Equal("1.21 GB", row.MemoryText);
        Assert.Equal(string.Empty, row.IoText);
        Assert.Equal(RowStatus.None, row.Status);
        Assert.True(row.IsChild);
        Assert.Equal("Editor", row.Badge);

        row.UpdateProcess(Sample(1234, "Code.exe", 0, 1_300_000_000, 5_000_000, suspended: true), null, "Code.exe", string.Empty, isChild: false);
        Assert.Equal(RowStatus.Suspended, row.Status);
        Assert.Equal("4.77 MB/s", row.IoText);
        Assert.True(row.IsSuspended);

        row.UpdateProcess(Sample(1234, "Code.exe", 0, 0, 0, hung: true), null, "Code.exe", string.Empty, isChild: false);
        Assert.Equal(RowStatus.NotResponding, row.Status);
    }

    [Fact]
    public void UpdateGroup_SumsMembersAndCapsCpu()
    {
        var group = new ProcessGroup("app:cursor", "Cursor", ApplicationCategory.Ide, null, [1, 2]);
        var members = new[] { Sample(1, "Cursor.exe", 60, 1_000_000, 0), Sample(2, "Cursor.exe", 70, 2_000_000, 2048) };
        var row = new ProcessRow("g:app:cursor", isGroup: true);

        row.UpdateGroup(group, members, null, isExpanded: true);

        Assert.Equal("Cursor", row.Name);
        Assert.Equal(3_000_000, row.Memory);
        Assert.Equal(100, row.Cpu);
        Assert.Equal("2 processes", row.Badge);
        Assert.True(row.IsExpanded);
        Assert.Equal(string.Empty, row.PidText);
    }
}
