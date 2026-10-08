# WinClean

A native Windows system monitor and cleanup utility for developers and power users. Free, open source (MIT), local-first.

WinClean answers three questions at a glance:

1. **What is using my CPU, memory and disk?** A process view that groups by application, so Cursor, Claude Code, Docker Desktop or WSL show up as one line with their real footprint.
2. **What is taking up space?** A read-only scan of a drive or folder with the largest folders and files, file types, installed applications, developer folders and the caches tools accumulate.
3. **Is my system healthy and up to date?** The Windows version and its support dates, update facts, pending restarts, firmware and the drivers that matter, with links to the official download pages.

It is not related to the other projects called "WinClean" on GitHub.

Screenshots will be added before the first release (`docs/screenshots/`).

## Features

**Overview.** CPU, memory, disk, network and GPU with a minute of history, the applications using the most memory, volumes, uptime, commit charge and battery. Temperatures when sensors are enabled.

**Processes.** One table, grouped by application: Claude Code, Codex, Cursor, Windsurf, Visual Studio Code, Visual Studio, JetBrains IDEs, Docker Desktop, WSL, Node.js, npm, pnpm, yarn, Python, Git, terminals, shells and browsers are recognised, and everything an application spawns is counted with it. Search, sort, a details pane with the command line and owner, and the actions: end, end tree, suspend, resume, open location, properties. Windows itself is protected: kernel-adjacent processes cannot be ended, Windows components ask for an explicit acknowledgement.

**Storage.** Drive cards, a scan on request that reads listings only, largest folders with drill-down, largest files, file types, installed applications, developer storage (`node_modules`, build output, virtual environments, package stores) and the known locations: temporary files, browser caches, package manager caches, IDE caches, Docker and WSL virtual disks, Windows Update leftovers, the Recycle Bin.

**Cleanup.** Categories with a risk level, exact file lists, a preview where single files can be left out, a confirmation that names what goes and what to know first, and a results view. Nothing is deleted silently and nothing is deleted without being shown first. See [docs/CLEANUP-RULES.md](docs/CLEANUP-RULES.md) for every rule.

**Health.** Edition, version and build; support dates from data bundled with the release, with their date; the last update check and install, recent history and pending-restart signals; firmware mode and Secure Boot; drivers for display, network, audio, storage and chipset with the version as the vendor writes it and a link to the vendor's download page. WinClean never compares driver versions online and never installs anything.

**Hardware.** Processor, graphics, memory modules, motherboard, firmware, TPM, disks, network adapters and displays, read from the firmware tables, the kernel and the drivers. Serial numbers stay hidden until asked for. Temperatures are optional and honest: if a reading is not available the page says why.

**Notification area.** Optional icon with CPU, memory and system drive as bars or figures, a menu, and an option to keep running when the window is closed.

**Desktop widget.** A small window with CPU, memory, disk and network and a minute of history for each, the developer tools and agents running right now (Claude Code at 12 % of the CPU and 1.2 GB, Docker Desktop, WSL) and the free space on the system drive. Always on top, behind every other window above the wallpaper, or a normal window; full or compact; adjustable opacity; optional click-through so it never gets in the way of a terminal or a game. Some ways to use it: keep an eye on an agent run while you code, watch memory with Docker and WSL up, see disk activity during a long build, leave it on a second monitor as an ambient dashboard, or start WinClean with `--widget` from a shortcut and never open the window at all. While the widget is shown, closing the main window keeps WinClean running.

## Download

WinClean is not released yet. Each release will ship:

- `WinClean-<version>-win-x64-setup.exe` and `-win-arm64-setup.exe`: per-user installers (no administrator rights needed), with an option for all users.
- `WinClean-<version>-win-x64-portable.zip` and `-win-arm64-portable.zip`: one executable; settings and logs live in a `data` folder next to it.
- `SHA256SUMS.txt` and a software bill of materials.

`winget install Solenjoyer.WinClean` once the package is published.

Until the builds are signed, SmartScreen shows "Unknown publisher" for a new download. The hashes in the release notes let you verify what you downloaded.

## Requirements

- Windows 11, or Windows 10 version 2004 (build 19041) and later on a best-effort basis.
- x64 or ARM64.
- No runtime to install: the executable is self-contained.
- Administrator rights are only needed for a few things, and WinClean says which: Windows temporary files, the Windows Update cache, servicing logs, system crash dumps, processes of other accounts, and CPU and motherboard sensors.

## Privacy

WinClean has no account, no telemetry, no analytics, no crash reporting and no automatic update check. It never opens a network connection on its own. The two things that reach the network only happen when you click them: opening a link in your browser, and "Check now" on the Health page, which asks the Windows Update Agent to contact Microsoft Update exactly as the Settings app does. Details in [PRIVACY.md](PRIVACY.md).

## How it works

C# 14 on .NET 10, WPF with the Fluent theme, hand-written Win32 interop. The platform-neutral logic (grouping rules, cleanup policy, parsers of raw OS buffers, formatting) lives in `WinClean.Core` and is unit tested on Linux; the application reads the operating system and feeds that logic. A headless `WinClean.exe --self-check` exercises every reader and is run by CI on a Windows runner. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the technology choice, the API map and the threading model.

| Need | Source |
|---|---|
| CPU, memory, disk, network | `GetSystemTimes`, `NtQuerySystemInformation`, `GlobalMemoryStatusEx`, `IOCTL_DISK_PERFORMANCE`, `GetIfTable2` |
| Processes | one `NtQuerySystemInformation` call per tick, parsed in place; paths, command lines and owners read once per process |
| GPU | the "GPU Engine" and "GPU Adapter Memory" performance counters, the same source Task Manager uses |
| Storage | directory listings only, no file opens; links and mount points are never followed |
| Windows version and updates | `RtlGetVersion`, the registry, the Windows Update Agent's local history |
| Drivers and hardware | the Configuration Manager, SMBIOS, DXGI, storage IOCTLs |
| Temperatures | [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), opt-in; CPU sensors need the [PawnIO](https://pawnio.eu) driver and administrator rights, which WinClean does not install |

## Building from source

```
dotnet build WinClean.slnx -c Release
dotnet test tests/WinClean.Core.Tests -c Release
dotnet publish src/WinClean/WinClean.csproj -c Release -r win-x64
```

The solution builds on Windows, Linux and macOS with the .NET 10 SDK; the application itself only runs on Windows. See [CONTRIBUTING.md](CONTRIBUTING.md) for the development setup, the conventions and the release process.

## Contributing

Issues and pull requests are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) first; it is short. Security issues go through [SECURITY.md](SECURITY.md).

## Frequently asked questions

**Why does SmartScreen warn about the download?** The builds are not yet code-signed; a new unsigned executable gets the "Unknown publisher" warning until enough people have run it. Compare the SHA-256 in the release notes with the file you downloaded.

**Why is the CPU temperature "not available"?** Reading CPU and motherboard sensors needs a kernel driver. WinClean does not ship one. If you install [PawnIO](https://pawnio.eu), enable sensors in Settings and restart WinClean as administrator, the readings appear. GPU and drive temperatures usually work without it.

**Why do I see fewer processes than Task Manager?** Windows services and system processes are hidden until "Show system processes" is on.

**Does cleanup touch my documents?** No. Cleanup only deletes files inside the folders listed in [docs/CLEANUP-RULES.md](docs/CLEANUP-RULES.md); Desktop, Documents, Pictures, Videos and Music are refused by the safety policy even if a rule pointed there.

## Roadmap

Not in the first release: a treemap view, per-process network usage, translations (the strings are already in a resource file), Scoop and Chocolatey packages, code signing (the pipeline step exists, the certificate does not).

## License

MIT. See [LICENSE](LICENSE). WinClean uses [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL-2.0) and the libraries listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
