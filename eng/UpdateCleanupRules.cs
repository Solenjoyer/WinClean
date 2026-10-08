#:project ../src/WinClean.Core/WinClean.Core.csproj
#:property RestorePackagesWithLockFile=false

// Generates docs/CLEANUP-RULES.md from the catalogue in WinClean.Core, so the documentation and the
// behaviour cannot drift apart. Run: dotnet run eng/UpdateCleanupRules.cs [-- --check]

using System.Globalization;
using System.Text;
using WinClean.Core.Storage;

var root = FindRepositoryRoot();
var target = Path.Combine(root, "docs", "CLEANUP-RULES.md");
var check = args.Contains("--check", StringComparer.Ordinal);
var text = Generate();

if (check)
{
    var current = File.Exists(target) ? File.ReadAllText(target) : string.Empty;

    if (current.ReplaceLineEndings("\n") != text.ReplaceLineEndings("\n"))
    {
        Console.Error.WriteLine("docs/CLEANUP-RULES.md is out of date. Run: dotnet run eng/UpdateCleanupRules.cs");
        return 1;
    }

    Console.WriteLine("docs/CLEANUP-RULES.md is up to date.");
    return 0;
}

File.WriteAllText(target, text);
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Wrote {KnownLocations.All.Count} locations and {DeveloperArtifactRules.All.Count} developer folder rules to docs/CLEANUP-RULES.md"));
return 0;

static string Generate()
{
    var text = new StringBuilder();
    text.AppendLine("# Cleanup rules");
    text.AppendLine();
    text.AppendLine("Generated from `KnownLocations` and `DeveloperArtifactRules` in `WinClean.Core`; do not edit by hand (`dotnet run eng/UpdateCleanupRules.cs`).");
    text.AppendLine();
    text.AppendLine("Every path below is measured on the Storage page. Only the locations marked cleanable can be selected on the Cleanup page, and only after an analysis that lists the exact files. Before each deletion the safety policy checks that the file is a plain local file inside the category root, not a link, not a system or read-only file, not inside the Windows folder except for the approved subfolders, not inside Desktop, Documents, Pictures, Videos or Music, and old enough. Files that are in use are skipped and reported.");
    text.AppendLine();
    text.AppendLine("Variables: `%LOCALAPPDATA%`, `%APPDATA%`, `%USERPROFILE%`, `%PROGRAMDATA%`, `%SYSTEMROOT%` and `%SYSTEMDRIVE%` are the folders of the signed-in account and the system. A `*` segment matches one folder level (profiles, versions).");

    foreach (var group in KnownLocations.All.GroupBy(location => location.Group))
    {
        text.AppendLine();
        text.AppendLine(CultureInfo.InvariantCulture, $"## {Title(group.Key)}");
        text.AppendLine();
        text.AppendLine("| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |");
        text.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var location in group)
        {
            var paths = string.Join("<br>", location.PathTemplates.Select(path => $"`{path}`"));
            var notes = new List<string>();

            if (location.FilePatterns is { Count: > 0 } patterns)
            {
                notes.Add("only " + string.Join(", ", patterns.Select(pattern => $"`{pattern}`")));
            }

            if (location.UsesTemporaryFileAge)
            {
                notes.Add("files younger than the temporary-file age setting are left alone");
            }

            if (location.ConflictingProcesses is { Count: > 0 } processes)
            {
                notes.Add("files in use are skipped while " + string.Join(", ", processes) + " runs");
            }

            if (location.RecycleInsteadOfDelete)
            {
                notes.Add("moved to the Recycle Bin, not deleted");
            }

            if (location.Note is not null)
            {
                notes.Add(location.Note);
            }

            text.AppendLine(CultureInfo.InvariantCulture, $"| **{location.DisplayName}**<br>{location.Description} | {paths} | {(location.IsCleanable ? "yes" : "report only")} | {location.Risk} | {(location.RequiresElevation ? "yes" : "no")} | {(location.DefaultSelected ? "yes" : "no")} | {string.Join("; ", notes)} |");
        }
    }

    text.AppendLine();
    text.AppendLine("## Special categories");
    text.AppendLine();
    text.AppendLine("| Category | What happens | Risk |");
    text.AppendLine("|---|---|---|");
    text.AppendLine("| Recycle Bin | The entries of the signed-in account are listed from their `$I` metadata; the bin of each fixed drive is emptied as a whole with the shell's own function. | Caution |");
    text.AppendLine("| Docker | `docker system df` reports what is reclaimable; the offered commands are `docker image prune -f`, `docker container prune -f` and `docker builder prune -f`, each run through the local Docker command line. Volumes are never touched. | Caution |");
    text.AppendLine("| Stale developer folders | Folders recognised by the rules below whose project has not changed for longer than the stale threshold setting. Deleted recursively, after the project is checked again and the user has typed the folder name. | Advanced |");
    text.AppendLine();
    text.AppendLine("## Developer folder rules");
    text.AppendLine();
    text.AppendLine("| Folder | Recognised by | Offered for cleanup |");
    text.AppendLine("|---|---|---|");

    foreach (var rule in DeveloperArtifactRules.All)
    {
        var how = new List<string>();

        if (rule.SiblingMarkers is { Count: > 0 } siblings)
        {
            how.Add((rule.AllSiblingMarkers ? "all of " : "one of ") + string.Join(", ", siblings.Select(marker => $"`{marker}`")) + " next to it");
        }

        if (rule.ContentMarkers is { Count: > 0 } contents)
        {
            how.Add(string.Join(", ", contents.Select(marker => $"`{marker}`")) + " inside it");
        }

        if (rule.HomeOnly)
        {
            how.Add("directly in the profile folder");
        }

        text.AppendLine(CultureInfo.InvariantCulture, $"| **{rule.DisplayName}**<br>{string.Join(", ", rule.FolderNames.Select(name => $"`{name}`"))} | {(how.Count == 0 ? "name alone" : string.Join("; ", how))}<br>{rule.Description} | {(rule.IsCandidateForCleanup ? "yes" : "no, listed only")} |");
    }

    return text.ToString();
}

static string Title(LocationGroup group) => group switch
{
    LocationGroup.Temporary => "Temporary files",
    LocationGroup.Diagnostics => "Diagnostics",
    LocationGroup.Caches => "System caches",
    LocationGroup.Browsers => "Browsers",
    LocationGroup.PackageManagers => "Package manager caches",
    LocationGroup.PackageStores => "Package stores",
    LocationGroup.Ides => "Editors and IDEs",
    LocationGroup.AiAgents => "Coding agents",
    LocationGroup.Docker => "Docker Desktop",
    LocationGroup.Wsl => "WSL",
    LocationGroup.WindowsUpdate => "Windows Update",
    LocationGroup.Downloads => "Downloads",
    _ => group.ToString(),
};

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WinClean.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? Directory.GetCurrentDirectory();
}
