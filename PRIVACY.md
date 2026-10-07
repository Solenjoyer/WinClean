# Privacy

WinClean runs entirely on your computer.

## What it reads

- The process list, performance counters, memory and volume statistics, and the per-process facts Task Manager also shows (path, command line, owner).
- Named registry keys: the Windows version, update results, the Run key of your account, uninstall entries, WSL distributions, device driver details, personalisation (theme) and the processor description.
- Firmware tables (SMBIOS), the Configuration Manager, DXGI and storage device descriptors.
- File and folder metadata (names, sizes, dates, attributes) during a scan or an analysis. File contents are never read, with one exception: the small `$I` metadata files of the Recycle Bin, which record the original path and size of deleted files.
- Docker's own usage report through `docker system df`, against your local daemon.
- Optionally, hardware sensors through LibreHardwareMonitor, when you enable them.

## What it writes

- `%LOCALAPPDATA%\WinClean\settings.json` and `%LOCALAPPDATA%\WinClean\logs\`, or a `data` folder next to the executable in portable mode.
- The single-file extraction folder .NET creates on first start under `%TEMP%\.net\WinClean\`.
- The `WinClean` value under `HKCU\...\CurrentVersion\Run`, only when you enable "Start with Windows".

## What it deletes

Only the items you have selected, previewed and confirmed, inside the folders documented in `docs/CLEANUP-RULES.md`. Every run is logged to `logs\cleanup-<date>.log`.

## What it never does

No account, no telemetry, no analytics, no crash upload, no update check, no background service, no scheduled task, no silent elevation, no bundled driver.

## The two cases that reach the network

Both only happen when you click:

1. Opening a link in your default browser (official driver pages, the project pages, a prefilled GitHub issue from the crash dialog).
2. "Check now" on the Health page, which asks the Windows Update Agent to contact Microsoft Update, exactly as the Settings app does.

## How to verify

The source is public. A tool such as Process Monitor or a firewall with outbound logging will show no connections from `WinClean.exe` other than the two above.
