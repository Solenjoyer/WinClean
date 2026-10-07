using WinClean.Core.Applications;

namespace WinClean.Core.Storage;

/// <summary>
/// Folders developers accumulate and forget. Restorable ones (a package manager or build recreates
/// them) can be offered for cleanup when their project has gone stale; the rest are only measured.
/// </summary>
public static class DeveloperArtifactRules
{
    public static IReadOnlyList<DeveloperArtifactRule> All { get; } =
    [
        new(ArtifactKind.NodeModules, "node_modules", "Installed npm packages. npm, pnpm or yarn install recreates them.",
            ["node_modules"], SiblingMarkers: ["package.json"], Restorable: true),
        new(ArtifactKind.GitRepository, "Git repository", "Version history of a project.",
            [".git"]),
        new(ArtifactKind.RustTarget, "Rust build output", "Compiled Rust artifacts. cargo build recreates them.",
            ["target"], SiblingMarkers: ["Cargo.toml"], Restorable: true),
        new(ArtifactKind.JavaBuild, "Maven build output", "Compiled Java artifacts. mvn package recreates them.",
            ["target"], SiblingMarkers: ["pom.xml"], Restorable: true),
        new(ArtifactKind.DotNetBuildOutput, ".NET build output", "Compiled .NET artifacts. dotnet build recreates them.",
            ["bin", "obj", "artifacts"], SiblingMarkers: ["*.csproj", "*.fsproj", "*.vbproj", "*.sln", "*.slnx", "Directory.Build.props"], Restorable: true),
        new(ArtifactKind.VisualStudioCache, "Visual Studio solution cache", "Per-solution caches and settings Visual Studio rebuilds.",
            [".vs"], SiblingMarkers: ["*.sln", "*.slnx", "*.csproj"], Restorable: true),
        new(ArtifactKind.PythonEnvironment, "Python virtual environment", "Installed Python packages for one project. pip or uv recreates it from the project's requirements.",
            [".venv", "venv", ".env", "env"], ContentMarkers: ["pyvenv.cfg"], Restorable: true),
        new(ArtifactKind.PythonCache, "Python bytecode cache", "Compiled Python modules. Python recreates them as it runs.",
            ["__pycache__"], Restorable: true),
        new(ArtifactKind.GradleProjectCache, "Gradle project cache", "Build cache and outputs of a Gradle project.",
            [".gradle", "build"], SiblingMarkers: ["build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts"], Restorable: true),
        new(ArtifactKind.NextBuild, "Next.js build output", "Compiled Next.js application. next build recreates it.",
            [".next"], SiblingMarkers: ["package.json"], Restorable: true),
        new(ArtifactKind.NuxtBuild, "Nuxt build output", "Compiled Nuxt application. nuxt build recreates it.",
            [".nuxt", ".output"], SiblingMarkers: ["package.json"], Restorable: true),
        new(ArtifactKind.Terraform, "Terraform providers", "Downloaded providers and modules. terraform init recreates them.",
            [".terraform"], SiblingMarkers: ["*.tf"], Restorable: true),
        new(ArtifactKind.UnityLibrary, "Unity Library", "Imported assets of a Unity project. Unity rebuilds it, which can take a long time.",
            ["Library"], SiblingMarkers: ["Assets", "ProjectSettings"], AllSiblingMarkers: true, Restorable: true),
        new(ArtifactKind.AndroidSdk, "Android SDK", "Platform tools, build tools and system images.",
            ["Sdk", "android-sdk", "Android"], ContentMarkers: ["platform-tools", "build-tools"]),
        new(ArtifactKind.HomeCache, "Tool data in the profile folder", "Caches, stores and configuration developer tools keep in your user profile.",
            [".nuget", ".cargo", ".rustup", ".gradle", ".m2", ".docker", ".claude", ".codex", ".cursor", ".gemini", ".vscode", ".android", ".pub-cache", ".npm", ".yarn", ".pnpm-store", ".conda", ".ollama"],
            HomeOnly: true),
    ];

    /// <summary>
    /// Matches a folder by name and markers. Sibling markers are the names in the parent folder, content markers
    /// the names inside the folder itself; either set may be left null when it is not available yet.
    /// </summary>
    public static DeveloperArtifactRule? Match(string folderName, IReadOnlyCollection<string>? siblings, IReadOnlyCollection<string>? contents, bool inProfileRoot)
    {
        ArgumentNullException.ThrowIfNull(folderName);

        foreach (var rule in All)
        {
            if (!rule.FolderNames.Any(name => string.Equals(name, folderName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (rule.HomeOnly)
            {
                if (inProfileRoot)
                {
                    return rule;
                }

                continue;
            }

            if (rule.SiblingMarkers is { Count: > 0 })
            {
                var present = rule.AllSiblingMarkers ? ContainsAllMarkers(siblings, rule.SiblingMarkers) : ContainsMarker(siblings, rule.SiblingMarkers);

                if (!present)
                {
                    continue;
                }
            }

            if (rule.ContentMarkers is { Count: > 0 } && !ContainsAllMarkers(contents, rule.ContentMarkers))
            {
                continue;
            }

            return rule;
        }

        return null;
    }

    private static bool ContainsMarker(IReadOnlyCollection<string>? names, IReadOnlyList<string> markers)
    {
        return names is not null && names.Any(name => markers.Any(marker => Wildcard.IsMatch(name, marker)));
    }

    private static bool ContainsAllMarkers(IReadOnlyCollection<string>? names, IReadOnlyList<string> markers)
    {
        return names is not null && markers.All(marker => names.Any(name => Wildcard.IsMatch(name, marker)));
    }
}
