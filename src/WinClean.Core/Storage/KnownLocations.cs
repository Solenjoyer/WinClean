namespace WinClean.Core.Storage;

/// <summary>
/// The folders WinClean knows about: where temporary files, caches, package stores, IDE data and
/// virtual disks live. The storage page measures them; the cleanup page offers the cleanable ones.
/// </summary>
public static class KnownLocations
{
    private static readonly string[] ChromiumCacheFolders =
        [@"*\Cache\Cache_Data", @"*\Code Cache", @"*\GPUCache", @"*\Service Worker\CacheStorage", @"*\DawnGraphiteCache", @"*\DawnWebGPUCache", "GrShaderCache", "ShaderCache"];

    private static readonly string[] CodeEditorCacheFolders =
        ["Cache", "CachedData", "CachedExtensionVSIXs", "Code Cache", "GPUCache", "DawnGraphiteCache", "DawnWebGPUCache", "logs"];

    public static IReadOnlyList<KnownLocation> All { get; } =
    [
        // Temporary files
        new("user-temp", LocationGroup.Temporary, "Temporary files", "Files programs left in your temporary folder.",
            [@"%LOCALAPPDATA%\Temp"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true, UsesTemporaryFileAge: true),
        new("windows-temp", LocationGroup.Temporary, "Windows temporary files", "Files installers and services left in the system temporary folder.",
            [@"%SYSTEMROOT%\Temp"], LocationKind.Cleanable, CleanupRisk.Safe, RequiresElevation: true, DefaultSelected: true, UsesTemporaryFileAge: true),
        new("inet-cache", LocationGroup.Temporary, "Internet cache", "Legacy web cache used by some Windows components and older applications.",
            [@"%LOCALAPPDATA%\Microsoft\Windows\INetCache"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),

        // Diagnostics
        new("error-reports", LocationGroup.Diagnostics, "Error reports", "Reports Windows queued for Microsoft about crashes and hangs.",
            [@"%LOCALAPPDATA%\Microsoft\Windows\WER\ReportQueue", @"%LOCALAPPDATA%\Microsoft\Windows\WER\ReportArchive"],
            LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("system-error-reports", LocationGroup.Diagnostics, "System error reports", "Error reports queued by services and other accounts.",
            [@"%PROGRAMDATA%\Microsoft\Windows\WER\ReportQueue", @"%PROGRAMDATA%\Microsoft\Windows\WER\ReportArchive"],
            LocationKind.Cleanable, CleanupRisk.Safe, RequiresElevation: true, DefaultSelected: true),
        new("crash-dumps", LocationGroup.Diagnostics, "Crash dumps", "Memory dumps of applications that crashed. Useful when reporting a bug, otherwise just large.",
            [@"%LOCALAPPDATA%\CrashDumps"], LocationKind.Cleanable, CleanupRisk.Caution),
        new("system-crash-dumps", LocationGroup.Diagnostics, "System crash dumps", "Memory dumps written when Windows itself crashed.",
            [@"%SYSTEMROOT%\Minidump", @"%SYSTEMROOT%"], LocationKind.Cleanable, CleanupRisk.Caution, RequiresElevation: true, FilePatterns: ["*.dmp", "MEMORY.DMP"]),
        new("servicing-logs", LocationGroup.Diagnostics, "Windows servicing logs", "Logs written while Windows installs updates and features.",
            [@"%SYSTEMROOT%\Logs\CBS"], LocationKind.Cleanable, CleanupRisk.Caution, RequiresElevation: true),

        // Caches that rebuild themselves with a visible cost
        new("thumbnail-cache", LocationGroup.Caches, "Thumbnail and icon caches", "Explorer rebuilds them, which makes folders slow to open for a while.",
            [@"%LOCALAPPDATA%\Microsoft\Windows\Explorer"], LocationKind.Cleanable, CleanupRisk.Caution, FilePatterns: ["thumbcache_*.db", "iconcache_*.db"], ConflictingProcesses: ["explorer.exe"]),
        new("shader-caches", LocationGroup.Caches, "Shader caches", "Compiled graphics shaders. Games and applications recompile them, with stutter the first time.",
            [@"%LOCALAPPDATA%\D3DSCache", @"%LOCALAPPDATA%\NVIDIA\DXCache", @"%LOCALAPPDATA%\NVIDIA\GLCache", @"%LOCALAPPDATA%\AMD\DxCache", @"%LOCALAPPDATA%\AMD\GLCache"],
            LocationKind.Cleanable, CleanupRisk.Caution),
        new("driver-leftovers", LocationGroup.Caches, "Driver installer leftovers", "Folders graphics driver installers extract themselves into and never remove.",
            [@"%SYSTEMDRIVE%\NVIDIA", @"%SYSTEMDRIVE%\AMD", @"%SYSTEMDRIVE%\Intel"], LocationKind.Cleanable, CleanupRisk.Caution, RequiresElevation: true),

        // Browsers
        Browser("chrome-cache", "Google Chrome cache", @"%LOCALAPPDATA%\Google\Chrome\User Data", "chrome.exe"),
        Browser("edge-cache", "Microsoft Edge cache", @"%LOCALAPPDATA%\Microsoft\Edge\User Data", "msedge.exe"),
        Browser("brave-cache", "Brave cache", @"%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data", "brave.exe"),
        Browser("vivaldi-cache", "Vivaldi cache", @"%LOCALAPPDATA%\Vivaldi\User Data", "vivaldi.exe"),
        new("firefox-cache", LocationGroup.Browsers, "Firefox cache", "Web content Firefox keeps for faster loading. It is downloaded again as needed.",
            [@"%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*\cache2", @"%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*\startupCache", @"%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*\shader-cache"],
            LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true, ConflictingProcesses: ["firefox.exe"]),

        // Package manager caches: downloaded again on demand
        new("npm-cache", LocationGroup.PackageManagers, "npm cache", "Packages npm downloaded. Reinstalls fetch them again.",
            [@"%LOCALAPPDATA%\npm-cache\_cacache", @"%LOCALAPPDATA%\npm-cache\_npx", @"%LOCALAPPDATA%\npm-cache\_logs"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("yarn-cache", LocationGroup.PackageManagers, "Yarn cache", "Packages Yarn downloaded.",
            [@"%LOCALAPPDATA%\Yarn\Cache", @"%LOCALAPPDATA%\Yarn\Berry\cache"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("corepack-cache", LocationGroup.PackageManagers, "Corepack and Bun caches", "Package manager binaries and packages Node's corepack and Bun downloaded.",
            [@"%LOCALAPPDATA%\node\corepack", @"%USERPROFILE%\.bun\install\cache"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("pip-cache", LocationGroup.PackageManagers, "pip cache", "Wheels and downloads pip keeps.",
            [@"%LOCALAPPDATA%\pip\cache"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("uv-cache", LocationGroup.PackageManagers, "uv cache", "Packages and interpreters uv downloaded. Existing environments keep working.",
            [@"%LOCALAPPDATA%\uv\cache"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("poetry-cache", LocationGroup.PackageManagers, "Poetry cache", "Packages Poetry downloaded.",
            [@"%LOCALAPPDATA%\pypoetry\Cache"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("nuget-http-cache", LocationGroup.PackageManagers, "NuGet download cache", "Package metadata and downloads NuGet keeps between restores.",
            [@"%LOCALAPPDATA%\NuGet\v3-cache", @"%LOCALAPPDATA%\NuGet\plugins-cache", @"%LOCALAPPDATA%\Temp\NuGetScratch"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("go-build-cache", LocationGroup.PackageManagers, "Go build cache", "Compiled Go packages. The next build recreates what it needs.",
            [@"%LOCALAPPDATA%\go-build"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),

        // Package stores: projects link into them, so they are managed by their own tools
        new("pnpm-store", LocationGroup.PackageStores, "pnpm store", "Content-addressable store that pnpm projects hard-link into.",
            [@"%LOCALAPPDATA%\pnpm\store", @"%LOCALAPPDATA%\pnpm-cache"], LocationKind.ReportOnly, CleanupRisk.Caution, Command: "pnpm store prune"),
        new("nuget-packages", LocationGroup.PackageStores, "NuGet packages", "The global packages folder every restore reads from.",
            [@"%USERPROFILE%\.nuget\packages"], LocationKind.ReportOnly, CleanupRisk.Caution, Command: "dotnet nuget locals global-packages --clear"),
        new("gradle-caches", LocationGroup.PackageStores, "Gradle caches", "Dependencies and build outputs Gradle keeps for every project.",
            [@"%USERPROFILE%\.gradle\caches"], LocationKind.ReportOnly, CleanupRisk.Caution),
        new("maven-repository", LocationGroup.PackageStores, "Maven repository", "Dependencies Maven downloaded for every project.",
            [@"%USERPROFILE%\.m2\repository"], LocationKind.ReportOnly, CleanupRisk.Caution),
        new("cargo-registry", LocationGroup.PackageStores, "Cargo registry", "Crate sources Cargo downloaded.",
            [@"%USERPROFILE%\.cargo\registry", @"%USERPROFILE%\.cargo\git"], LocationKind.ReportOnly, CleanupRisk.Caution, Command: "cargo cache --autoclean"),
        new("go-module-cache", LocationGroup.PackageStores, "Go module cache", "Module sources Go downloaded.",
            [@"%USERPROFILE%\go\pkg\mod"], LocationKind.ReportOnly, CleanupRisk.Caution, Command: "go clean -modcache"),

        // IDEs and editors
        CodeEditor("vscode-cache", "Visual Studio Code caches", @"%APPDATA%\Code", "Code.exe"),
        CodeEditor("vscode-insiders-cache", "Visual Studio Code Insiders caches", @"%APPDATA%\Code - Insiders", "Code - Insiders.exe"),
        CodeEditor("cursor-cache", "Cursor caches", @"%APPDATA%\Cursor", "Cursor.exe"),
        CodeEditor("windsurf-cache", "Windsurf caches", @"%APPDATA%\Windsurf", "Windsurf.exe"),
        new("visual-studio-caches", LocationGroup.Ides, "Visual Studio caches", "Component and designer caches that Visual Studio rebuilds on the next start.",
            [@"%LOCALAPPDATA%\Microsoft\VisualStudio\*\ComponentModelCache", @"%LOCALAPPDATA%\Microsoft\VisualStudio\*\Designer\ShadowCache", @"%LOCALAPPDATA%\Temp\SymbolCache", @"%LOCALAPPDATA%\Microsoft\VSApplicationInsights"],
            LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true, ConflictingProcesses: ["devenv.exe"]),
        new("jetbrains-logs", LocationGroup.Ides, "JetBrains logs", "Log and temporary files of JetBrains IDEs and Toolbox.",
            [@"%LOCALAPPDATA%\JetBrains\*\log", @"%LOCALAPPDATA%\JetBrains\*\tmp", @"%LOCALAPPDATA%\JetBrains\Toolbox\logs"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("jetbrains-caches", LocationGroup.Ides, "JetBrains caches and indexes", "Project indexes. The IDE rebuilds them, which takes a while on large projects.",
            [@"%LOCALAPPDATA%\JetBrains\*\caches", @"%LOCALAPPDATA%\JetBrains\*\index", @"%LOCALAPPDATA%\JetBrains\Transient"],
            LocationKind.Cleanable, CleanupRisk.Caution,
            ConflictingProcesses: ["idea64.exe", "rider64.exe", "pycharm64.exe", "webstorm64.exe", "clion64.exe", "goland64.exe", "datagrip64.exe", "phpstorm64.exe", "rubymine64.exe", "rustrover64.exe"]),
        new("vscode-extensions", LocationGroup.Ides, "Visual Studio Code extensions", "Installed extensions, managed from inside the editor.",
            [@"%USERPROFILE%\.vscode\extensions", @"%USERPROFILE%\.vscode-insiders\extensions"], LocationKind.ReportOnly, CleanupRisk.Advanced),
        new("cursor-extensions", LocationGroup.Ides, "Cursor extensions", "Installed extensions, managed from inside Cursor.",
            [@"%USERPROFILE%\.cursor\extensions"], LocationKind.ReportOnly, CleanupRisk.Advanced),
        new("vscode-workspaces", LocationGroup.Ides, "Visual Studio Code workspace storage", "Per-project state and local history kept by the editor.",
            [@"%APPDATA%\Code\User\workspaceStorage", @"%APPDATA%\Code\User\History", @"%APPDATA%\Cursor\User\workspaceStorage", @"%APPDATA%\Cursor\User\History"],
            LocationKind.ReportOnly, CleanupRisk.Advanced),

        // AI coding agents
        new("claude-code-data", LocationGroup.AiAgents, "Claude Code data", "Session transcripts, project memory and debug logs of Claude Code.",
            [@"%USERPROFILE%\.claude"], LocationKind.ReportOnly, CleanupRisk.Advanced),
        new("codex-data", LocationGroup.AiAgents, "Codex data", "Sessions and configuration of Codex.",
            [@"%USERPROFILE%\.codex"], LocationKind.ReportOnly, CleanupRisk.Advanced),
        new("gemini-data", LocationGroup.AiAgents, "Gemini CLI data", "Sessions and configuration of Gemini CLI.",
            [@"%USERPROFILE%\.gemini"], LocationKind.ReportOnly, CleanupRisk.Advanced),

        // Docker and WSL
        new("docker-data", LocationGroup.Docker, "Docker Desktop virtual disk", "Images, containers, volumes and build cache live inside this disk. It does not shrink by itself.",
            [@"%LOCALAPPDATA%\Docker\wsl\data\ext4.vhdx", @"%LOCALAPPDATA%\Docker\wsl\disk\docker_data.vhdx", @"%LOCALAPPDATA%\Docker\wsl\main\ext4.vhdx", @"%LOCALAPPDATA%\Docker\wsl\distro\ext4.vhdx", @"%PROGRAMDATA%\DockerDesktop\vm-data\DockerDesktop.vhdx"],
            LocationKind.ReportOnly, CleanupRisk.Advanced, Note: "Reclaim space from inside Docker: unused images and build cache can be pruned from the Cleanup page."),
        new("docker-logs", LocationGroup.Docker, "Docker Desktop logs", "Diagnostic logs Docker Desktop keeps.",
            [@"%LOCALAPPDATA%\Docker\log"], LocationKind.Cleanable, CleanupRisk.Safe, DefaultSelected: true),
        new("wsl-disks", LocationGroup.Wsl, "WSL virtual disks", "The Linux file systems of your distributions. They grow but do not shrink on their own.",
            [@"%LOCALAPPDATA%\wsl\*\ext4.vhdx", @"%LOCALAPPDATA%\Packages\CanonicalGroupLimited.*\LocalState\ext4.vhdx", @"%LOCALAPPDATA%\Packages\TheDebianProject.*\LocalState\ext4.vhdx", @"%LOCALAPPDATA%\Packages\KaliLinux.*\LocalState\ext4.vhdx", @"%LOCALAPPDATA%\Packages\*SUSE*\LocalState\ext4.vhdx"],
            LocationKind.ReportOnly, CleanupRisk.Advanced, Note: "wsl --manage <distribution> --set-sparse true lets a disk give space back."),
        new("wsl-swap", LocationGroup.Wsl, "WSL swap file", "Swap space of the WSL virtual machine, recreated when WSL starts.",
            [@"%LOCALAPPDATA%\Temp\swap.vhdx"], LocationKind.ReportOnly, CleanupRisk.Advanced),

        // Windows Update
        new("windows-update-cache", LocationGroup.WindowsUpdate, "Windows Update downloads", "Update packages that have already been installed. Windows downloads what it needs again.",
            [@"%SYSTEMROOT%\SoftwareDistribution\Download"], LocationKind.Cleanable, CleanupRisk.Safe, RequiresElevation: true, DefaultSelected: true,
            Note: "The Windows Update service holds some of these files; WinClean can stop it briefly and restart it."),
        new("delivery-optimization", LocationGroup.WindowsUpdate, "Delivery Optimization cache", "Update content shared with other PCs. Windows manages its size.",
            [@"%SYSTEMROOT%\SoftwareDistribution\DeliveryOptimization"], LocationKind.ReportOnly, CleanupRisk.Advanced),
        new("previous-windows", LocationGroup.WindowsUpdate, "Previous Windows installation", "Kept for ten days after a feature update so you can go back.",
            [@"%SYSTEMDRIVE%\Windows.old"], LocationKind.ReportOnly, CleanupRisk.Advanced, Note: "Remove it from Settings > System > Storage > Temporary files."),
        new("upgrade-leftovers", LocationGroup.WindowsUpdate, "Upgrade leftovers", "Working folders of Windows setup and recovery.",
            [@"%SYSTEMDRIVE%\$WinREAgent", @"%SYSTEMDRIVE%\$Windows.~BT", @"%SYSTEMDRIVE%\$Windows.~WS", @"%SYSTEMDRIVE%\$GetCurrent"],
            LocationKind.ReportOnly, CleanupRisk.Advanced, Note: "Remove them from Settings > System > Storage > Temporary files."),

        // Downloads
        new("downloads", LocationGroup.Downloads, "Downloads", "Everything you downloaded. WinClean never deletes it on its own.",
            [@"%USERPROFILE%\Downloads"], LocationKind.ReportOnly, CleanupRisk.Advanced),
        new("old-installers", LocationGroup.Downloads, "Old installers in Downloads", "Setup programs and archives that have been sitting in Downloads for months. Each one is listed; nothing is preselected.",
            [@"%USERPROFILE%\Downloads"], LocationKind.Cleanable, CleanupRisk.Caution, FilePatterns: ["*.exe", "*.msi", "*.msix", "*.msixbundle", "*.appx", "*.zip", "*.7z", "*.iso"],
            RecycleInsteadOfDelete: true),
    ];

    public static KnownLocation? Find(string id) => All.FirstOrDefault(location => string.Equals(location.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// Turns the templates into concrete paths. Variables come from the caller; * segments are expanded with
    /// the given directory lister so nothing here touches the file system.
    /// </summary>
    public static IReadOnlyList<string> Expand(KnownLocation location, IReadOnlyDictionary<string, string> variables, Func<string, IEnumerable<string>> listDirectories)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(listDirectories);

        var results = new List<string>();

        foreach (var template in location.PathTemplates)
        {
            var substituted = Substitute(template, variables);

            if (substituted is null)
            {
                continue;
            }

            if (!substituted.Contains('*', StringComparison.Ordinal))
            {
                results.Add(substituted);
                continue;
            }

            results.AddRange(ExpandWildcards(substituted, listDirectories));
        }

        return results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? Substitute(string template, IReadOnlyDictionary<string, string> variables)
    {
        var result = template;
        var start = result.IndexOf('%', StringComparison.Ordinal);

        while (start >= 0)
        {
            var end = result.IndexOf('%', start + 1);

            if (end < 0)
            {
                break;
            }

            var name = result[(start + 1)..end];

            if (!variables.TryGetValue(name, out var value) || string.IsNullOrEmpty(value))
            {
                return null;
            }

            result = string.Concat(result.AsSpan(0, start), value.TrimEnd('\\'), result.AsSpan(end + 1));
            start = result.IndexOf('%', start + value.Length);
        }

        return result;
    }

    private static IEnumerable<string> ExpandWildcards(string path, Func<string, IEnumerable<string>> listDirectories)
    {
        var segments = path.Split('\\');
        var firstWildcard = Array.FindIndex(segments, segment => segment.Contains('*', StringComparison.Ordinal));
        var prefix = string.Join('\\', segments, 0, firstWildcard);
        var pattern = segments[firstWildcard];
        var rest = string.Join('\\', segments, firstWildcard + 1, segments.Length - firstWildcard - 1);

        foreach (var directory in listDirectories(prefix))
        {
            var name = directory[(directory.LastIndexOf('\\') + 1)..];

            if (!Applications.Wildcard.IsMatch(name, pattern))
            {
                continue;
            }

            var candidate = rest.Length == 0 ? directory : directory + "\\" + rest;

            if (candidate.Contains('*', StringComparison.Ordinal))
            {
                foreach (var nested in ExpandWildcards(candidate, listDirectories))
                {
                    yield return nested;
                }
            }
            else
            {
                yield return candidate;
            }
        }
    }

    private static KnownLocation Browser(string id, string displayName, string userData, string process)
    {
        return new KnownLocation(id, LocationGroup.Browsers, displayName, "Web content kept for faster loading. It is downloaded again as needed.",
            ChromiumCacheFolders.Select(folder => userData + "\\" + folder).ToList(), LocationKind.Cleanable, CleanupRisk.Safe,
            DefaultSelected: true, ConflictingProcesses: [process]);
    }

    private static KnownLocation CodeEditor(string id, string displayName, string appData, string process)
    {
        return new KnownLocation(id, LocationGroup.Ides, displayName, "Caches and logs the editor rebuilds on the next start.",
            CodeEditorCacheFolders.Select(folder => appData + "\\" + folder).ToList(), LocationKind.Cleanable, CleanupRisk.Safe,
            DefaultSelected: true, ConflictingProcesses: [process]);
    }
}
