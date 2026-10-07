# Architecture

## Technology

C# 14 on .NET 10 (LTS), WPF with the built-in Fluent theme, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, hand-written `LibraryImport` interop. Published as a self-contained single-file executable per architecture, so users install nothing.

| Option | Why not |
|---|---|
| WinUI 3 | a Windows-only XAML compiler and packaging, a runtime dependency or a much larger download, younger tooling |
| Tauri or Electron | a web view is not native and uses more memory at idle |
| C++ / Win32 | slow feature velocity and few contributors for the same result |
| Avalonia | renders its own controls; cross-platform is not a goal |

## Projects

- `src/WinClean.Core` (net10.0): models, rules, parsers of raw OS buffers and formatting. No `System.Windows`, no registry, no interop. Unit tested on every platform.
- `src/WinClean` (net10.0-windows): the application. `Native/` holds one static class per DLL; `Services/` reads the operating system and feeds Core; `ViewModels/` and `Views/` are MVVM with source-generated observable properties and commands; `Controls/` are small custom elements (sparkline, proportion bar, metric card, search box, banner, setting row).
- `tests/WinClean.Core.Tests`: the Core tests.
- `tests/WinClean.App.Tests` (Windows only): interop struct sizes and view-model formatting.

## Threading

- One sampler thread ("WinClean.Sampler", below-normal priority) owns every cheap read. Each tick builds one immutable `SystemSample` that reaches the UI thread coalesced: a sample the UI has not consumed yet is replaced, never queued.
- What is sampled follows what is on screen (`MonitoringDemand`): metrics every tick, GPU and frequency every two seconds on pages that show them, processes only on the Overview and Processes pages, nothing at all when the window is hidden and the notification area icon is off. Resuming from sleep re-baselines every rate instead of showing a spike.
- Slow per-process facts (path, description, command line, owner, icon) are read on a lowest-priority STA thread and merged into the next tick.
- Sensors run on their own thread so a slow hardware library cannot stall sampling.
- Scans, discovery and cleanup run on worker threads with cancellation and progress; the UI thread only receives results.

## The process table

One `NtQuerySystemInformation(SystemProcessInformation)` call per tick into a pinned buffer, walked by 64-bit offsets in Core. Processes are tracked by id and start time, so a reused id never inherits another process's rates. `ApplicationGrouper` turns the flat list into application groups: strong rules (an editor, an agent, a container runtime) always start a group; weak rules (node, python, git, shells) join their nearest ancestor's group unless that ancestor is a barrier root such as a terminal or Explorer; unknown helpers join a parent when they live in its install directory. Rows on screen are updated in place and only reordered between ticks, never while the mouse is down.

## Storage scans

A few threads list one directory each with `FileSystemEnumerable` and no file opens. Files are counted into their folder; the tree holds folders only, in chunked arrays linked by index. Developer folders are recognised while the parent listing is at hand. Links and mount points are never followed; cloud folders are entered, cloud-only files are counted but not hydrated.

## Cleanup

`KnownLocations` is the catalogue (paths as templates, risk, elevation, patterns, conflicting processes). `CleanupDiscovery` resolves and lists every candidate file and applies `CleanupSafetyPolicy` to each one: absolute local path, inside the category root, outside the Windows folder except for four approved subfolders, never inside document folders or program folders, never a link, a system file or a read-only file, never too new. `CleanupExecutor` works from exactly the previewed list, re-checks every file before deleting it, moves installers to the Recycle Bin, pauses the Windows Update service for a retry when it holds files, and writes a log. The only recursive deletion is a detected developer folder, after its project is checked again and the user has typed its name.

## Diagnostics

`WinClean.exe --self-check` runs every reader headlessly and reports OK, DEGRADED or FAIL per check with the reason; the Settings page shows the same list live. CI publishes the executable on a Windows runner and runs the self-check on every change.

## Known limitations

- Processor performance counters and the GPU counters depend on perflib; on a machine where they are broken, the corresponding values read "Not available" with the reason.
- The support table for Windows versions is bundled data with a date; a newer build than the table knows reads as such.
- CPU and motherboard sensors depend on the user-installed PawnIO driver and administrator rights; the application does not install either.
