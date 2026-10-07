namespace WinClean.Core.Applications;

/// <summary>
/// Turns a flat process list into application groups. A strong match always starts a group; a weak
/// match joins its nearest ancestor's group unless that ancestor is a barrier root such as a terminal,
/// a shell or Explorer; unknown executables group by path, and helper processes living in the
/// parent's install directory join the parent.
/// </summary>
public static class ApplicationGrouper
{
    private static readonly string[] AlwaysJoinParent = ["conhost.exe", "OpenConsole.exe"];

    public static GroupingResult Group(IReadOnlyList<ProcessFacts> processes)
    {
        ArgumentNullException.ThrowIfNull(processes);

        var byPid = new Dictionary<int, ProcessFacts>(processes.Count);

        foreach (var process in processes)
        {
            byPid.TryAdd(process.Pid, process);
        }

        var children = new Dictionary<int, List<ProcessFacts>>();
        var roots = new List<ProcessFacts>();

        foreach (var process in byPid.Values)
        {
            if (HasLiveParent(process, byPid))
            {
                if (!children.TryGetValue(process.ParentPid, out var siblings))
                {
                    siblings = [];
                    children[process.ParentPid] = siblings;
                }

                siblings.Add(process);
            }
            else
            {
                roots.Add(process);
            }
        }

        var assignments = new Dictionary<int, Assignment>(byPid.Count);
        var groups = new Dictionary<string, GroupBuilder>(StringComparer.Ordinal);
        var order = new List<string>();
        var stack = new Stack<(ProcessFacts Process, Assignment? Parent)>();

        // The stack pops the last push first, so the earliest process goes on last.
        foreach (var root in roots.OrderByDescending(root => root.CreateTime).ThenByDescending(root => root.Pid))
        {
            stack.Push((root, null));
        }

        while (stack.Count > 0)
        {
            var (process, parent) = stack.Pop();
            var assignment = Assign(process, parent, byPid);
            assignments[process.Pid] = assignment;

            if (!groups.TryGetValue(assignment.GroupKey, out var builder))
            {
                builder = new GroupBuilder(assignment.GroupKey, assignment.DisplayName, assignment.Category, assignment.Application);
                groups[assignment.GroupKey] = builder;
                order.Add(assignment.GroupKey);
            }

            builder.Members.Add(process.Pid);

            if (children.TryGetValue(process.Pid, out var siblings))
            {
                foreach (var child in siblings.OrderByDescending(child => child.CreateTime).ThenByDescending(child => child.Pid))
                {
                    stack.Push((child, assignment));
                }
            }
        }

        var result = order.Select(key => groups[key].Build()).ToList();
        var keyByPid = assignments.ToDictionary(pair => pair.Key, pair => pair.Value.GroupKey);

        return new GroupingResult(result, keyByPid);
    }

    /// <summary>An edge is trusted only when the parent started first: process ids get reused.</summary>
    private static bool HasLiveParent(ProcessFacts process, Dictionary<int, ProcessFacts> byPid)
    {
        return process.ParentPid != process.Pid
            && byPid.TryGetValue(process.ParentPid, out var parent)
            && parent.CreateTime <= process.CreateTime;
    }

    private static Assignment Assign(ProcessFacts process, Assignment? parent, Dictionary<int, ProcessFacts> byPid)
    {
        if (parent is not null && AlwaysJoinParent.Any(name => string.Equals(name, process.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return parent.Inherited();
        }

        var match = ApplicationCatalog.Match(process);

        if (match is not null)
        {
            var application = match.Application;

            if (application.Strength == RuleStrength.Strong || parent is null || parent.BlocksInheritance)
            {
                return Assignment.Root(process.Pid, "app:" + application.Id, application.DisplayName, application.Category, application, application.InheritanceBarrier);
            }

            return parent.Inherited();
        }

        // A known application owns whatever it spawns (an agent's tools, an IDE's language servers);
        // an unknown one only owns helpers that live in its own install directory.
        if (parent is not null && !parent.BlocksInheritance && (parent.Application is not null || IsHelperOf(process, parent, byPid)))
        {
            return parent.Inherited();
        }

        var key = process.Path is { Length: > 0 } path ? "path:" + path.ToLowerInvariant() : "name:" + process.Name.ToLowerInvariant();
        var displayName = string.IsNullOrWhiteSpace(process.Description) ? process.Stem : process.Description.Trim();

        return Assignment.Root(process.Pid, key, displayName, ApplicationCategory.Other, null, barrier: false);
    }

    /// <summary>Electron renderers, crash handlers and the like live in the parent's install directory.</summary>
    private static bool IsHelperOf(ProcessFacts process, Assignment parent, Dictionary<int, ProcessFacts> byPid)
    {
        if (process.Path is null || !byPid.TryGetValue(parent.RootPid, out var rootProcess) || rootProcess.Path is null)
        {
            return false;
        }

        var installDirectory = TopLevelDirectory(rootProcess.Path);
        return installDirectory is not null && process.Path.StartsWith(installDirectory, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>For C:\Users\dev\AppData\Local\Programs\cursor\Cursor.exe that is the cursor folder, with a trailing separator.</summary>
    private static string? TopLevelDirectory(string path)
    {
        var separator = path.LastIndexOf('\\');

        if (separator <= 0)
        {
            return null;
        }

        var directory = path[..separator];

        // Shared folders would make everything in them look like one application.
        if (directory.EndsWith(@":\Windows\System32", StringComparison.OrdinalIgnoreCase)
            || directory.EndsWith(@":\Windows", StringComparison.OrdinalIgnoreCase)
            || directory.EndsWith(@":\Program Files", StringComparison.OrdinalIgnoreCase)
            || directory.EndsWith(@":\Program Files (x86)", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return directory + "\\";
    }

    private sealed record Assignment(string GroupKey, string DisplayName, ApplicationCategory Category, KnownApplication? Application, int RootPid, bool IsRoot, bool Barrier)
    {
        public bool BlocksInheritance => IsRoot && Barrier;

        public static Assignment Root(int pid, string key, string displayName, ApplicationCategory category, KnownApplication? application, bool barrier)
        {
            return new Assignment(key, displayName, category, application, pid, IsRoot: true, Barrier: barrier);
        }

        public Assignment Inherited() => this with { IsRoot = false };
    }

    private sealed class GroupBuilder(string key, string displayName, ApplicationCategory category, KnownApplication? application)
    {
        public List<int> Members { get; } = [];

        public ProcessGroup Build() => new(key, displayName, category, application, Members);
    }
}
