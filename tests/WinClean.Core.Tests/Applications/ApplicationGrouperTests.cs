using WinClean.Core.Applications;

namespace WinClean.Core.Tests.Applications;

public class ApplicationGrouperTests
{
    private const string CursorExe = @"C:\Users\dev\AppData\Local\Programs\cursor\Cursor.exe";

    private const string NodeExe = @"C:\Program Files\nodejs\node.exe";

    private const string ClaudeCommandLine = @"""C:\Program Files\nodejs\node.exe"" C:\Users\dev\AppData\Roaming\npm\node_modules\@anthropic-ai\claude-code\cli.js";

    [Fact]
    public void ClaudeCodeStartedInsideCursor_IsItsOwnGroupWithItsTools()
    {
        var result = new SnapshotBuilder()
            .Add(4000, 800, "explorer.exe", @"C:\Windows\explorer.exe")
            .Add(5000, 4000, "Cursor.exe", CursorExe)
            .Add(5010, 5000, "Cursor.exe", CursorExe, "Cursor.exe --type=ptyHost")
            .Add(5020, 5010, "pwsh.exe", @"C:\Program Files\PowerShell\7\pwsh.exe")
            .Add(5030, 5020, "node.exe", NodeExe, ClaudeCommandLine)
            .Add(5040, 5030, "cmd.exe", @"C:\Windows\System32\cmd.exe")
            .Add(5050, 5040, "git.exe", @"C:\Program Files\Git\cmd\git.exe")
            .Add(5060, 5030, "rg.exe", @"C:\Users\dev\AppData\Roaming\npm\node_modules\@anthropic-ai\claude-code\vendor\ripgrep\x64-win32\rg.exe")
            .Group();

        Assert.Equal(["app:explorer", "app:cursor", "app:claude-code"], result.Groups.Select(group => group.Key));
        Assert.Equal([5000, 5010, 5020], Group(result, "app:cursor").MemberPids);
        Assert.Equal([5030, 5040, 5050, 5060], Group(result, "app:claude-code").MemberPids.Order());
        Assert.Equal("Claude Code", Group(result, "app:claude-code").DisplayName);
        Assert.Equal(ApplicationCategory.AiAgent, Group(result, "app:claude-code").Category);
    }

    [Fact]
    public void DevServerStartedFromATerminal_StandsAlone()
    {
        var result = new SnapshotBuilder()
            .Add(100, 1, "WindowsTerminal.exe", @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21\WindowsTerminal.exe")
            .Add(101, 100, "OpenConsole.exe", @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21\OpenConsole.exe")
            .Add(110, 100, "pwsh.exe", @"C:\Program Files\PowerShell\7\pwsh.exe")
            .Add(120, 110, "node.exe", NodeExe, @"node C:\src\app\server.js")
            .Add(121, 120, "node.exe", NodeExe, @"node C:\src\app\worker.js")
            .Group();

        Assert.Equal([100, 101], Group(result, "app:windows-terminal").MemberPids);
        Assert.Equal([110], Group(result, "app:powershell").MemberPids);
        Assert.Equal([120, 121], Group(result, "app:node").MemberPids);
    }

    [Fact]
    public void ChromeProcesses_CollapseIntoOneGroup()
    {
        const string chrome = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        var result = new SnapshotBuilder()
            .Add(10, 1, "explorer.exe", @"C:\Windows\explorer.exe")
            .Add(20, 10, "chrome.exe", chrome)
            .Add(21, 20, "chrome.exe", chrome, "--type=gpu-process")
            .Add(22, 20, "chrome.exe", chrome, "--type=renderer")
            .Add(23, 20, "chrome.exe", chrome, "--type=utility")
            .Group();

        Assert.Equal([20, 21, 22, 23], Group(result, "app:chrome").MemberPids.Order());
        Assert.Equal("app:chrome", result.GroupKeyByPid[22]);
        Assert.Equal(ApplicationCategory.Browser, Group(result, "app:chrome").Category);
    }

    [Fact]
    public void ServiceHosts_CollapseIntoWindowsServices()
    {
        var result = new SnapshotBuilder()
            .Add(700, 500, "services.exe", @"C:\Windows\System32\services.exe", sessionId: 0, systemAccount: true)
            .Add(900, 700, "svchost.exe", @"C:\Windows\System32\svchost.exe", "svchost.exe -k netsvcs", sessionId: 0, systemAccount: true)
            .Add(901, 700, "svchost.exe", @"C:\Windows\System32\svchost.exe", "svchost.exe -k LocalService", sessionId: 0, systemAccount: true)
            .Add(950, 700, "MsMpEng.exe", @"C:\ProgramData\Microsoft\Windows Defender\Platform\4.18\MsMpEng.exe", description: "Antimalware Service Executable", sessionId: 0, systemAccount: true)
            .Group();

        Assert.Equal([900, 901], Group(result, "app:windows-services").MemberPids);
        Assert.Equal([700], Group(result, "app:windows").MemberPids);
        Assert.Equal("Antimalware Service Executable", result.Groups.Single(group => group.MemberPids.Contains(950)).DisplayName);
    }

    [Fact]
    public void ReusedProcessId_DoesNotLinkAChildToAYoungerParent()
    {
        var result = new SnapshotBuilder()
            .Add(300, 1, "Code.exe", @"C:\Users\dev\AppData\Local\Programs\Microsoft VS Code\Code.exe", createTime: 5000)
            .Add(310, 300, "node.exe", NodeExe, "node server.js", createTime: 4000)
            .Group();

        Assert.Equal([300], Group(result, "app:vscode").MemberPids);
        Assert.Equal([310], Group(result, "app:node").MemberPids);
    }

    [Fact]
    public void UnknownApplication_OwnsHelpersFromItsInstallDirectoryOnly()
    {
        var result = new SnapshotBuilder()
            .Add(10, 1, "explorer.exe", @"C:\Windows\explorer.exe")
            .Add(600, 10, "Spotify.exe", @"C:\Users\dev\AppData\Roaming\Spotify\Spotify.exe", description: "Spotify")
            .Add(601, 600, "Spotify.exe", @"C:\Users\dev\AppData\Roaming\Spotify\Spotify.exe", "--type=renderer")
            .Add(700, 10, "steam.exe", @"C:\Program Files (x86)\Steam\steam.exe", description: "Steam")
            .Add(701, 700, "game.exe", @"D:\Games\Example\game.exe", description: "Example Game")
            .Group();

        var spotify = result.Groups.Single(group => group.DisplayName == "Spotify");
        Assert.Equal([600, 601], spotify.MemberPids);
        Assert.Equal(ApplicationCategory.Other, spotify.Category);
        Assert.Equal([700], result.Groups.Single(group => group.DisplayName == "Steam").MemberPids);
        Assert.Equal([701], result.Groups.Single(group => group.DisplayName == "Example Game").MemberPids);
    }

    [Fact]
    public void KnownApplication_OwnsUnknownProcessesItSpawns()
    {
        var result = new SnapshotBuilder()
            .Add(300, 1, "Code.exe", @"C:\Users\dev\AppData\Local\Programs\Microsoft VS Code\Code.exe")
            .Add(310, 300, "rg.exe", @"C:\Users\dev\AppData\Local\Programs\Microsoft VS Code\resources\app\node_modules\@vscode\ripgrep\bin\rg.exe")
            .Add(320, 300, "pwsh.exe", @"C:\Program Files\PowerShell\7\pwsh.exe")
            .Add(330, 320, "node.exe", NodeExe, @"node C:\Users\dev\AppData\Roaming\npm\node_modules\npm\bin\npm-cli.js run dev")
            .Add(340, 320, "dotnet.exe", @"C:\Program Files\dotnet\dotnet.exe", "dotnet build")
            .Group();

        var vscode = Group(result, "app:vscode");
        Assert.Equal([300, 310, 320, 330, 340], vscode.MemberPids.Order());
        Assert.Single(result.Groups);
    }

    [Fact]
    public void ExplorerChildren_StartTheirOwnGroups()
    {
        var result = new SnapshotBuilder()
            .Add(10, 1, "explorer.exe", @"C:\Windows\explorer.exe")
            .Add(11, 10, "notepad.exe", @"C:\Windows\System32\notepad.exe", description: "Notepad")
            .Add(12, 10, "msedgewebview2.exe", @"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\130.0\msedgewebview2.exe")
            .Group();

        Assert.Equal([10], Group(result, "app:explorer").MemberPids);
        Assert.Equal([11], result.Groups.Single(group => group.DisplayName == "Notepad").MemberPids);
        Assert.Equal([12], Group(result, "app:webview2").MemberPids);
    }

    [Fact]
    public void WebView2_InsideAnotherApplication_JoinsIt()
    {
        var result = new SnapshotBuilder()
            .Add(800, 1, "ms-teams.exe", @"C:\Program Files\WindowsApps\MSTeams_24\ms-teams.exe", description: "Microsoft Teams")
            .Add(801, 800, "msedgewebview2.exe", @"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\130.0\msedgewebview2.exe")
            .Add(802, 801, "msedgewebview2.exe", @"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\130.0\msedgewebview2.exe", "--type=renderer")
            .Group();

        var teams = Assert.Single(result.Groups);
        Assert.Equal("Microsoft Teams", teams.DisplayName);
        Assert.Equal([800, 801, 802], teams.MemberPids);
    }

    [Fact]
    public void ProcessesWithoutPath_GroupByName()
    {
        var result = new SnapshotBuilder()
            .Add(4, 0, "System", sessionId: 0, systemAccount: true)
            .Add(120, 4, "Registry", sessionId: 0, systemAccount: true)
            .Add(2000, 4, "vmmemWSL", sessionId: 0, systemAccount: true)
            .Add(3000, 1, "Mystery.exe")
            .Add(3001, 1, "Mystery.exe")
            .Group();

        Assert.Equal([4, 120], Group(result, "app:windows-system").MemberPids);
        Assert.Equal([2000], Group(result, "app:wsl").MemberPids);
        Assert.Equal([3000, 3001], Group(result, "name:mystery.exe").MemberPids);
        Assert.Equal("Mystery", Group(result, "name:mystery.exe").DisplayName);
    }

    [Fact]
    public void Group_IsDeterministic()
    {
        var snapshot = new SnapshotBuilder()
            .Add(10, 1, "explorer.exe", @"C:\Windows\explorer.exe")
            .Add(20, 10, "Cursor.exe", CursorExe)
            .Add(21, 20, "Cursor.exe", CursorExe)
            .Add(30, 10, "chrome.exe", @"C:\Program Files\Google\Chrome\Application\chrome.exe")
            .Build();

        var first = ApplicationGrouper.Group(snapshot);
        var second = ApplicationGrouper.Group(snapshot.Reverse().ToList());

        Assert.Equal(first.Groups.Select(group => group.Key), second.Groups.Select(group => group.Key));
        Assert.Equal(first.GroupKeyByPid, second.GroupKeyByPid);
    }

    private static ProcessGroup Group(GroupingResult result, string key) => result.Groups.Single(group => group.Key == key);
}
