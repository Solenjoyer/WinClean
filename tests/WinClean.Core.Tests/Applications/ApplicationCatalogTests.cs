using WinClean.Core.Applications;

namespace WinClean.Core.Tests.Applications;

public class ApplicationCatalogTests
{
    [Theory]
    [InlineData("claude.exe", null, null, "claude-code")]
    [InlineData("node.exe", @"C:\Program Files\nodejs\node.exe", @"node C:/Users/dev/AppData/Roaming/npm/node_modules/@anthropic-ai/claude-code/cli.js", "claude-code")]
    [InlineData("bun.exe", null, @"bun C:\tools\@openai\codex\bin\codex.js", "codex")]
    [InlineData("codex-x86_64-pc-windows-msvc.exe", null, null, "codex")]
    [InlineData("node.exe", null, @"node C:\Users\dev\AppData\Roaming\npm\node_modules\npm\bin\npm-cli.js install", "npm")]
    [InlineData("node.exe", null, @"node C:\Users\dev\AppData\Local\pnpm\pnpm.cjs install", "pnpm")]
    [InlineData("node.exe", null, @"node server.js", "node")]
    [InlineData("ServiceHub.Host.dotnet.x64.exe", null, null, "visual-studio-tools")]
    [InlineData("python3.12.exe", null, null, "python")]
    [InlineData("bash.exe", @"C:\Program Files\Git\usr\bin\bash.exe", null, "git")]
    [InlineData("ssh.exe", @"C:\Windows\System32\OpenSSH\ssh.exe", null, "git")]
    [InlineData("Code - Insiders.exe", null, null, "vscode")]
    [InlineData("rider64.exe", null, null, "jetbrains")]
    [InlineData("vmmemWSL", null, null, "wsl")]
    [InlineData("svchost.exe", null, null, "windows-services")]
    public void Match_RecognisesKnownApplications(string name, string? path, string? commandLine, string expectedId)
    {
        var facts = new ProcessFacts(1, 1, 0, name, path, commandLine, null, 1, false);

        Assert.Equal(expectedId, ApplicationCatalog.Match(facts)?.Application.Id);
    }

    [Theory]
    [InlineData("bash.exe", @"C:\msys64\usr\bin\bash.exe")]
    [InlineData("notepad.exe", @"C:\Windows\System32\notepad.exe")]
    [InlineData("Mystery.exe", null)]
    public void Match_ReturnsNullForUnknownProcesses(string name, string? path)
    {
        var facts = new ProcessFacts(1, 1, 0, name, path, null, null, 1, false);

        Assert.Null(ApplicationCatalog.Match(facts));
    }

    [Fact]
    public void Match_PrefersTheCommandLineOverTheExecutableName()
    {
        var facts = new ProcessFacts(1, 1, 0, "node.exe", null, @"node C:\x\@anthropic-ai\claude-code\cli.js", null, 1, false);

        var match = ApplicationCatalog.Match(facts)!;

        Assert.Equal("claude-code", match.Application.Id);
        Assert.Equal(ProcessSignature.CommandLineScore, match.Score);
    }

    [Fact]
    public void Catalog_HasUniqueIds()
    {
        var ids = ApplicationCatalog.All.Select(application => application.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("ServiceHub.Host.exe", "ServiceHub.*.exe", true)]
    [InlineData("servicehub.exe", "ServiceHub.*.exe", false)]
    [InlineData("codex-aarch64-pc-windows-msvc.exe", "codex-*-windows-msvc.exe", true)]
    [InlineData("python3.exe", "python3*.exe", true)]
    [InlineData("python.exe", "python3*.exe", false)]
    [InlineData("CODE.EXE", "Code.exe", true)]
    public void Wildcard_MatchesStarPatternsCaseInsensitively(string text, string pattern, bool expected)
    {
        Assert.Equal(expected, Wildcard.IsMatch(text, pattern));
    }
}
