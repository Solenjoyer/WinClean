using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using WinClean.Core.Monitoring;
using WinClean.Native;
using WinClean.Services.Shell;

namespace WinClean.Services.Processes;

/// <summary>
/// The few things WinClean does to a process. Every action re-checks that the id still belongs to the
/// process the user looked at before touching it.
/// </summary>
public sealed class ProcessActions
{
    private readonly ILogger<ProcessActions> _logger;

    private readonly ShellLinks _links;

    public ProcessActions(ILogger<ProcessActions> logger, ShellLinks links)
    {
        _logger = logger;
        _links = links;
    }

    public ActionResult Terminate(ProcessIdentity identity)
    {
        return WithProcess(identity, Kernel32.PROCESS_TERMINATE, process => Kernel32.TerminateProcess(process, 1) ? ActionResult.Succeeded : FromLastError());
    }

    public ActionResult Suspend(ProcessIdentity identity)
    {
        return WithProcess(identity, Kernel32.PROCESS_SUSPEND_RESUME, process => FromStatus(NtDll.NtSuspendProcess(process)));
    }

    public ActionResult Resume(ProcessIdentity identity)
    {
        return WithProcess(identity, Kernel32.PROCESS_SUSPEND_RESUME, process => FromStatus(NtDll.NtResumeProcess(process)));
    }

    /// <summary>
    /// Ends a process and everything it started. The tree is frozen first so that nothing respawns a
    /// child half-way through, then ended from the root down; whatever could not be ended is thawed again.
    /// </summary>
    public IReadOnlyList<(ProcessSample Process, ActionResult Result)> TerminateTree(ProcessSample root, IReadOnlyList<ProcessSample> processes)
    {
        var tree = Descendants(root, processes);
        var suspended = new List<ProcessSample>();

        foreach (var process in tree)
        {
            if (Suspend(process.Identity).Ok)
            {
                suspended.Add(process);
            }
        }

        var results = new List<(ProcessSample, ActionResult)>(tree.Count);

        foreach (var process in tree)
        {
            var result = Terminate(process.Identity);
            results.Add((process, result));

            if (!result.Ok && suspended.Contains(process))
            {
                Resume(process.Identity);
            }
        }

        return results;
    }

    /// <summary>The root first, then its children breadth-first; a child must have started after its parent.</summary>
    public static IReadOnlyList<ProcessSample> Descendants(ProcessSample root, IReadOnlyList<ProcessSample> processes)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(processes);

        var byParent = new Dictionary<int, List<ProcessSample>>();

        foreach (var process in processes)
        {
            if (!byParent.TryGetValue(process.Facts.ParentPid, out var children))
            {
                children = [];
                byParent[process.Facts.ParentPid] = children;
            }

            children.Add(process);
        }

        var result = new List<ProcessSample> { root };
        var seen = new HashSet<int> { root.Pid };

        for (var index = 0; index < result.Count; index++)
        {
            var parent = result[index];

            if (!byParent.TryGetValue(parent.Pid, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (child.Facts.CreateTime >= parent.Facts.CreateTime && seen.Add(child.Pid))
                {
                    result.Add(child);
                }
            }
        }

        return result;
    }

    public void OpenLocation(string path) => _links.Reveal(path);

    public void ShowProperties(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var verb = Marshal.StringToCoTaskMemUni("properties");
        var file = Marshal.StringToCoTaskMemUni(path);

        try
        {
            var info = new Shell32.SHELLEXECUTEINFOW
            {
                cbSize = (uint)Marshal.SizeOf<Shell32.SHELLEXECUTEINFOW>(),
                fMask = Shell32.SEE_MASK_INVOKEIDLIST,
                lpVerb = verb,
                lpFile = file,
                nShow = Shell32.SW_SHOWNORMAL,
            };

            if (!Shell32.ShellExecuteExW(ref info))
            {
                _logger.LogWarning("The properties dialog for {Path} could not be opened: {Error}", path, new Win32Exception().Message);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(verb);
            Marshal.FreeCoTaskMem(file);
        }
    }

    private ActionResult WithProcess(ProcessIdentity identity, uint access, Func<SafeProcessHandle, ActionResult> action)
    {
        using var process = Kernel32.OpenProcess(access | Kernel32.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)identity.Pid);

        if (process.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            return error switch
            {
                Kernel32.ERROR_ACCESS_DENIED => new ActionResult(ActionOutcome.AccessDenied),
                Kernel32.ERROR_INVALID_PARAMETER => ActionResult.Gone,
                _ => new ActionResult(ActionOutcome.Failed, new Win32Exception(error).Message),
            };
        }

        if (!Kernel32.GetProcessTimes(process, out var created, out _, out _, out _) || created != identity.CreateTime)
        {
            return ActionResult.Gone;
        }

        var result = action(process);

        if (!result.Ok)
        {
            _logger.LogInformation("Action on process {Pid} ended with {Outcome}: {Error}", identity.Pid, result.Outcome, result.Error);
        }

        return result;
    }

    private static ActionResult FromLastError()
    {
        var error = Marshal.GetLastPInvokeError();
        return error == Kernel32.ERROR_ACCESS_DENIED
            ? new ActionResult(ActionOutcome.AccessDenied)
            : new ActionResult(ActionOutcome.Failed, new Win32Exception(error).Message);
    }

    private static ActionResult FromStatus(int status)
    {
        if (status == NtDll.STATUS_SUCCESS)
        {
            return ActionResult.Succeeded;
        }

        if (status == NtDll.STATUS_ACCESS_DENIED)
        {
            return new ActionResult(ActionOutcome.AccessDenied);
        }

        return new ActionResult(ActionOutcome.Failed, new Win32Exception((int)NtDll.RtlNtStatusToDosError(status)).Message);
    }
}
