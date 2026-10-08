# Contributing

Thank you for taking the time. This page covers the setup, the conventions and the release process; it is meant to be read once.

## Prerequisites

- The .NET 10 SDK (the exact band is in `global.json`).
- To run the application: Windows 10 version 2004 or later, or Windows 11.
- An editor: Visual Studio 2022 17.14 or later with the .NET desktop workload, or Visual Studio Code with the C# Dev Kit, or JetBrains Rider.

Everything except running the application also works on Linux and macOS: the solution compiles there, including the WPF project, and the Core tests run.

## Building and testing

```
dotnet restore WinClean.slnx --locked-mode
dotnet build WinClean.slnx -c Release --no-restore
dotnet test tests/WinClean.Core.Tests -c Release --no-build
dotnet test tests/WinClean.App.Tests -c Release --no-build      # Windows only
dotnet format WinClean.slnx --verify-no-changes --no-restore
dotnet run eng/UpdateStrings.cs -- --check
dotnet run eng/UpdateCleanupRules.cs -- --check
```

CI runs exactly these on Linux and Windows, then publishes the executable and runs `WinClean.exe --self-check`, which exercises every system reader and writes one line per check. Run it yourself after touching the native layer:

```
dotnet publish src/WinClean/WinClean.csproj -c Release -r win-x64 -o artifacts/publish/smoke
artifacts/publish/smoke/WinClean.exe --self-check --report artifacts/self-check.txt
```

Add `--include-sensors` to also open the hardware monitoring library.

### Strings and generated files

User-facing text lives in `src/WinClean/Resources/Strings.resx`. After editing it, regenerate the accessor class and commit both files:

```
dotnet run eng/UpdateStrings.cs
```

`docs/CLEANUP-RULES.md` is generated from the catalogue in `WinClean.Core`:

```
dotnet run eng/UpdateCleanupRules.cs
```

### Debugging an elevated instance

Start your IDE as administrator, or run WinClean normally, use "Restart as administrator" on the Settings page and attach the debugger to the new process.

### Local release build

```
pwsh eng/build-release.ps1 -Rid win-x64 -Version 0.0.0-local
```

This publishes, zips the portable build and, when Inno Setup 6 is installed, builds the installer into `artifacts/release`.

### Optional pre-commit hook

```
git config core.hooksPath eng/git-hooks
```

The hook runs `dotnet format` on the staged C# files.

## Conventions

The code should read as if one careful person wrote it.

- One top-level type per file; namespaces mirror folders; file-scoped namespaces; no regions, banners or license headers.
- Comments explain why, not what. XML documentation only where it adds something.
- Platform-neutral logic goes into `WinClean.Core` (no `System.Windows`, no registry, no P/Invoke) with tests; the application reads the operating system and feeds that logic.
- Interop is hand-written with `LibraryImport`, one static class per DLL, names as in the Windows SDK. Add a size assertion to `tests/WinClean.App.Tests` for every new struct.
- Expected failures (access denied, a missing counter, a driver that is not there) are modelled results that degrade to "Not available" with the reason. Nothing is swallowed.
- Every user-facing string goes through `Strings.resx`. No emojis, no exclamation marks, no colours that carry meaning on their own.
- Cleanup rules are data in `KnownLocations`; the safety policy in `CleanupSafetyPolicy` is the contract and has adversarial tests. Do not add a deletion path that bypasses it.
- Commits: imperative subject, under 72 characters, a body that says why. One logical change per commit; every commit builds.

## Pull requests

- Open an issue first for anything beyond a small fix, so the approach can be agreed.
- Fill in the pull request template; say which Windows version you tested on.
- Keep the changelog up to date for user-visible changes (`CHANGELOG.md`, Unreleased section).
- There is no contributor license agreement: contributions are MIT like the rest.

## Repository settings (maintainer checklist)

Private vulnerability reporting on; secret scanning with push protection; Dependabot alerts; Actions limited to GitHub-owned actions; read-only default workflow token; squash merges with the pull request title; wiki off; Discussions on.

## Release process

See [docs/RELEASING.md](docs/RELEASING.md).
