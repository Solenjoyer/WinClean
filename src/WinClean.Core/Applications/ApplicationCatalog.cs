namespace WinClean.Core.Applications;

/// <summary>
/// The applications WinClean recognises by name. AI agents that run on Node are identified by their
/// command line, which is why a Claude Code session started from a Cursor terminal still shows up as
/// Claude Code rather than as part of Cursor.
/// </summary>
public static class ApplicationCatalog
{
    public const string WindowsServicesId = "windows-services";

    public const string WindowsSystemId = "windows-system";

    public const string WindowsId = "windows";

    private static readonly string[] NodeHosts = ["node.exe", "bun.exe"];

    public static IReadOnlyList<KnownApplication> All { get; } =
    [
        new("claude-code", "Claude Code", ApplicationCategory.AiAgent, RuleStrength.Strong,
        [
            new ProcessSignature(["claude.exe"]),
            new ProcessSignature(NodeHosts, CommandLineContains: [@"@anthropic-ai\claude-code", @"claude-code\cli.js"]),
        ]),
        new("codex", "Codex", ApplicationCategory.AiAgent, RuleStrength.Strong,
        [
            new ProcessSignature(["codex.exe", "codex-*-windows-msvc.exe"]),
            new ProcessSignature(NodeHosts, CommandLineContains: [@"@openai\codex"]),
        ]),
        new("gemini-cli", "Gemini CLI", ApplicationCategory.AiAgent, RuleStrength.Strong,
        [
            new ProcessSignature(NodeHosts, CommandLineContains: [@"@google\gemini-cli", @"gemini-cli\bundle\gemini.js"]),
        ]),
        new("copilot-cli", "GitHub Copilot CLI", ApplicationCategory.AiAgent, RuleStrength.Strong,
        [
            new ProcessSignature(["copilot.exe"]),
            new ProcessSignature(NodeHosts, CommandLineContains: [@"@github\copilot"]),
        ]),
        new("aider", "Aider", ApplicationCategory.AiAgent, RuleStrength.Strong,
        [
            new ProcessSignature(["aider.exe"]),
            new ProcessSignature(["python.exe", "python3*.exe", "pythonw.exe"], CommandLineContains: [@"\aider\", "-m aider"]),
        ]),

        KnownApplication.Strong("cursor", "Cursor", ApplicationCategory.Ide, "Cursor.exe"),
        KnownApplication.Strong("windsurf", "Windsurf", ApplicationCategory.Ide, "Windsurf.exe"),
        KnownApplication.Strong("vscode", "Visual Studio Code", ApplicationCategory.Editor, "Code.exe", "Code - Insiders.exe", "VSCodium.exe"),
        KnownApplication.Strong("visual-studio", "Visual Studio", ApplicationCategory.Ide, "devenv.exe"),
        KnownApplication.Weak("visual-studio-tools", "Visual Studio tools", ApplicationCategory.Ide,
            "MSBuild.exe", "VBCSCompiler.exe", "ServiceHub.*.exe", "Microsoft.ServiceHub.Controller.exe", "PerfWatson2.exe", "MSBuildTaskHost.exe", "cl.exe", "link.exe"),
        KnownApplication.Strong("jetbrains", "JetBrains IDE", ApplicationCategory.Ide,
            "idea64.exe", "rider64.exe", "pycharm64.exe", "webstorm64.exe", "clion64.exe", "goland64.exe", "datagrip64.exe",
            "phpstorm64.exe", "rubymine64.exe", "rustrover64.exe", "fleet.exe", "jetbrains-toolbox.exe", "jetbrains-gateway.exe"),
        KnownApplication.Weak("jetbrains-helper", "JetBrains helper", ApplicationCategory.Ide, "fsnotifier.exe", "jcef_helper.exe"),

        new("docker-desktop", "Docker Desktop", ApplicationCategory.Container, RuleStrength.Strong,
        [
            new ProcessSignature(["Docker Desktop.exe", "com.docker.backend.exe", "com.docker.build.exe", "com.docker.dev-envs.exe", "com.docker.admin.exe", "dockerd.exe", "vpnkit.exe"]),
        ],
        Hint: "With the WSL 2 backend, container memory is held by the WSL virtual machine."),
        KnownApplication.Weak("docker-cli", "Docker CLI", ApplicationCategory.Container, "docker.exe", "docker-compose.exe"),
        new("wsl", "WSL", ApplicationCategory.VirtualMachine, RuleStrength.Strong,
        [
            new ProcessSignature(["wslservice.exe", "wslhost.exe", "wslrelay.exe", "wslg.exe", "vmmemWSL"]),
        ],
        Hint: "Memory of the WSL 2 virtual machine, shared with Docker Desktop's WSL 2 backend. Linux processes, including agents running inside WSL, only show up here."),
        new("hyper-v", "Virtual machine", ApplicationCategory.VirtualMachine, RuleStrength.Strong,
        [
            new ProcessSignature(["vmmem", "vmmem.exe", "vmwp.exe", "vmcompute.exe"]),
        ],
        Hint: "Memory of a Hyper-V virtual machine, which may belong to Docker Desktop, WSL or Windows Sandbox."),
        KnownApplication.Weak("wsl-cli", "WSL", ApplicationCategory.VirtualMachine, "wsl.exe"),

        new("npm", "npm", ApplicationCategory.PackageManager, RuleStrength.Weak,
        [
            new ProcessSignature(NodeHosts, CommandLineContains: [@"\npm-cli.js", @"\npx-cli.js"]),
        ]),
        new("pnpm", "pnpm", ApplicationCategory.PackageManager, RuleStrength.Weak,
        [
            new ProcessSignature(["pnpm.exe"]),
            new ProcessSignature(NodeHosts, CommandLineContains: [@"\pnpm.cjs", @"\pnpm\bin\"]),
        ]),
        new("yarn", "Yarn", ApplicationCategory.PackageManager, RuleStrength.Weak,
        [
            new ProcessSignature(NodeHosts, CommandLineContains: [@"\yarn.js", @"\yarn\bin\", @"\corepack\"]),
        ]),
        KnownApplication.Weak("node", "Node.js", ApplicationCategory.Runtime, "node.exe", "bun.exe", "deno.exe"),
        KnownApplication.Weak("python", "Python", ApplicationCategory.Runtime, "python.exe", "pythonw.exe", "python3*.exe", "py.exe", "uv.exe", "uvx.exe", "pip.exe"),
        new("git", "Git", ApplicationCategory.VersionControl, RuleStrength.Weak,
        [
            new ProcessSignature(["git.exe", "git-*.exe", "git-credential-manager.exe"]),
            new ProcessSignature(["bash.exe", "sh.exe", "mintty.exe"], PathContains: [@"\git\"]),
            new ProcessSignature(["ssh.exe"], PathContains: [@"\git\", @"\openssh\"]),
        ]),

        new("windows-terminal", "Windows Terminal", ApplicationCategory.Terminal, RuleStrength.Strong,
            [new ProcessSignature(["WindowsTerminal.exe"])], InheritanceBarrier: true),
        new("console-host", "Console host", ApplicationCategory.Terminal, RuleStrength.Weak,
            [new ProcessSignature(["OpenConsole.exe", "conhost.exe"])]),
        new("powershell", "PowerShell", ApplicationCategory.Shell, RuleStrength.Weak,
            [new ProcessSignature(["pwsh.exe", "powershell.exe", "powershell_ise.exe"])], InheritanceBarrier: true),
        new("cmd", "Command Prompt", ApplicationCategory.Shell, RuleStrength.Weak,
            [new ProcessSignature(["cmd.exe"])], InheritanceBarrier: true),

        KnownApplication.Strong("chrome", "Google Chrome", ApplicationCategory.Browser, "chrome.exe"),
        KnownApplication.Strong("edge", "Microsoft Edge", ApplicationCategory.Browser, "msedge.exe"),
        KnownApplication.Strong("firefox", "Firefox", ApplicationCategory.Browser, "firefox.exe"),
        KnownApplication.Strong("brave", "Brave", ApplicationCategory.Browser, "brave.exe"),
        KnownApplication.Strong("opera", "Opera", ApplicationCategory.Browser, "opera.exe"),
        KnownApplication.Strong("vivaldi", "Vivaldi", ApplicationCategory.Browser, "vivaldi.exe"),
        KnownApplication.Weak("webview2", "WebView2", ApplicationCategory.Browser, "msedgewebview2.exe"),

        new(WindowsServicesId, "Windows services", ApplicationCategory.Windows, RuleStrength.Strong,
            [new ProcessSignature(["svchost.exe"])], InheritanceBarrier: true),
        new("explorer", "Windows Explorer", ApplicationCategory.Windows, RuleStrength.Strong,
            [new ProcessSignature(["explorer.exe"])], InheritanceBarrier: true),
        new(WindowsId, "Windows", ApplicationCategory.Windows, RuleStrength.Strong,
        [
            new ProcessSignature(["services.exe", "wininit.exe", "winlogon.exe", "userinit.exe", "csrss.exe", "smss.exe", "lsass.exe", "LsaIso.exe",
                "fontdrvhost.exe", "dwm.exe", "sihost.exe", "RuntimeBroker.exe", "ApplicationFrameHost.exe", "taskhostw.exe", "ctfmon.exe",
                "SearchHost.exe", "StartMenuExperienceHost.exe", "ShellExperienceHost.exe", "TextInputHost.exe", "audiodg.exe", "spoolsv.exe"]),
        ],
        InheritanceBarrier: true),
        new(WindowsSystemId, "Windows system", ApplicationCategory.Windows, RuleStrength.Strong,
            [new ProcessSignature(["System", "Registry", "Memory Compression", "Secure System", "Idle"])], InheritanceBarrier: true),
    ];

    /// <summary>The best matching application, or null for anything WinClean has no rule for.</summary>
    public static ApplicationMatch? Match(ProcessFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        KnownApplication? best = null;
        var bestScore = 0;

        foreach (var application in All)
        {
            var score = application.Score(facts);

            if (score > bestScore)
            {
                best = application;
                bestScore = score;
            }
        }

        return best is null ? null : new ApplicationMatch(best, bestScore);
    }
}
