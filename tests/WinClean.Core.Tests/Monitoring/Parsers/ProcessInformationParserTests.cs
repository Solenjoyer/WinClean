using WinClean.Core.Monitoring.Parsers;

namespace WinClean.Core.Tests.Monitoring.Parsers;

public class ProcessInformationParserTests
{
    [Fact]
    public void Parse_ReadsEveryEntry()
    {
        var builder = new ProcessInformationBuffer()
            .Add(0, 0, null, runningThreads: 4)
            .Add(4, 0, "System", createTime: 100, kernelTime: 5000, handles: 2500, sessionId: 0, runningThreads: 200)
            .Add(1234, 4, "Code.exe", createTime: 900, kernelTime: 10, userTime: 20, privateWorkingSet: 1 << 20, workingSet: 2 << 20, commit: 3 << 20, handles: 42, readTransfer: 1, writeTransfer: 2, otherTransfer: 3, runningThreads: 3);
        var buffer = builder.Build();
        var records = new List<ProcessRecord>();

        ProcessInformationParser.Parse(buffer, builder.Address, records);

        Assert.Equal(3, records.Count);

        var idle = records[0];
        Assert.Equal(0, idle.Pid);
        Assert.Equal(0, idle.NameLength);
        Assert.Equal(string.Empty, ProcessInformationParser.ReadName(buffer, idle));

        var system = records[1];
        Assert.Equal(4, system.Pid);
        Assert.Equal("System", ProcessInformationParser.ReadName(buffer, system));
        Assert.Equal(200, system.Threads);
        Assert.Equal(2500, system.Handles);
        Assert.Equal(0, system.SessionId);

        var code = records[2];
        Assert.Equal(1234, code.Pid);
        Assert.Equal(4, code.ParentPid);
        Assert.Equal(900, code.CreateTime);
        Assert.Equal(30, code.CpuTime);
        Assert.Equal(1 << 20, code.PrivateWorkingSet);
        Assert.Equal(2 << 20, code.WorkingSet);
        Assert.Equal(3 << 20, code.Commit);
        Assert.Equal(42, code.Handles);
        Assert.Equal(1, code.SessionId);
        Assert.Equal(6, code.IoTransfer);
        Assert.Equal("Code.exe", ProcessInformationParser.ReadName(buffer, code));
        Assert.False(code.IsSuspended);
    }

    [Fact]
    public void Parse_CountsSuspendedThreads()
    {
        var builder = new ProcessInformationBuffer()
            .Add(10, 1, "paused.exe", runningThreads: 0, suspendedThreads: 3)
            .Add(11, 1, "mixed.exe", runningThreads: 1, suspendedThreads: 2);
        var records = new List<ProcessRecord>();

        ProcessInformationParser.Parse(builder.Build(), builder.Address, records);

        Assert.True(records[0].IsSuspended);
        Assert.Equal(3, records[0].SuspendedThreads);
        Assert.False(records[1].IsSuspended);
        Assert.Equal(2, records[1].SuspendedThreads);
    }

    [Fact]
    public void Parse_IgnoresNamesPointingOutsideTheBuffer()
    {
        var builder = new ProcessInformationBuffer().Add(7, 1, "Code.exe");
        var buffer = builder.Build();
        var records = new List<ProcessRecord>();

        // The name pointer belongs to another address space when the base address is wrong.
        ProcessInformationParser.Parse(buffer, builder.Address + 0x1000, records);

        Assert.Single(records);
        Assert.Equal(0, records[0].NameLength);
        Assert.Equal(string.Empty, ProcessInformationParser.ReadName(buffer, records[0]));
    }

    [Fact]
    public void Parse_StopsAtTruncatedOrMalformedData()
    {
        var builder = new ProcessInformationBuffer().Add(1, 0, "a.exe").Add(2, 1, "b.exe");
        var buffer = builder.Build();
        var records = new List<ProcessRecord>();

        ProcessInformationParser.Parse(buffer.AsSpan(0, buffer.Length - 40), builder.Address, records);
        Assert.Equal(2, records.Count);

        ProcessInformationParser.Parse(buffer.AsSpan(0, 100), builder.Address, records);
        Assert.Empty(records);

        // A next-entry offset smaller than the header can only be garbage; the walk ends there.
        buffer[0] = 8;
        buffer[1] = 0;
        buffer[2] = 0;
        buffer[3] = 0;
        ProcessInformationParser.Parse(buffer, builder.Address, records);
        Assert.Single(records);
    }
}
