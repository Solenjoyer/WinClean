using WinClean.Services;

namespace WinClean.App.Tests.ViewModels;

public class StartupOptionsTests
{
    [Fact]
    public void Parse_ReadsEverySwitchInEitherForm()
    {
        var options = StartupOptions.Parse(["--minimized", "--page=cleanup", "--self-check", "--report", "out.txt", "--elevated", "--replace-pid", "4242", "--unknown"]);

        Assert.True(options.StartMinimized);
        Assert.Equal("cleanup", options.Page);
        Assert.True(options.SelfCheck);
        Assert.Equal("out.txt", options.ReportPath);
        Assert.True(options.Elevated);
        Assert.Equal(4242, options.ReplacePid);
        Assert.False(options.IncludeSensors);
    }

    [Fact]
    public void Parse_WithNothing_OpensTheWindow()
    {
        var options = StartupOptions.Parse([]);

        Assert.False(options.StartMinimized);
        Assert.Null(options.Page);
        Assert.False(options.SelfCheck);
    }
}
