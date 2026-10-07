# Cleanup rules

Generated from `KnownLocations` and `DeveloperArtifactRules` in `WinClean.Core`; do not edit by hand (`dotnet run eng/UpdateCleanupRules.cs`).

Every path below is measured on the Storage page. Only the locations marked cleanable can be selected on the Cleanup page, and only after an analysis that lists the exact files. Before each deletion the safety policy checks that the file is a plain local file inside the category root, not a link, not a system or read-only file, not inside the Windows folder except for the approved subfolders, not inside Desktop, Documents, Pictures, Videos or Music, and old enough. Files that are in use are skipped and reported.

Variables: `%LOCALAPPDATA%`, `%APPDATA%`, `%USERPROFILE%`, `%PROGRAMDATA%`, `%SYSTEMROOT%` and `%SYSTEMDRIVE%` are the folders of the signed-in account and the system. A `*` segment matches one folder level (profiles, versions).

## Temporary files

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Temporary files**<br>Files programs left in your temporary folder. | `%LOCALAPPDATA%\Temp` | yes | Safe | no | yes | files younger than the temporary-file age setting are left alone |
| **Windows temporary files**<br>Files installers and services left in the system temporary folder. | `%SYSTEMROOT%\Temp` | yes | Safe | yes | yes | files younger than the temporary-file age setting are left alone |
| **Internet cache**<br>Legacy web cache used by some Windows components and older applications. | `%LOCALAPPDATA%\Microsoft\Windows\INetCache` | yes | Safe | no | yes |  |

## Diagnostics

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Error reports**<br>Reports Windows queued for Microsoft about crashes and hangs. | `%LOCALAPPDATA%\Microsoft\Windows\WER\ReportQueue`<br>`%LOCALAPPDATA%\Microsoft\Windows\WER\ReportArchive` | yes | Safe | no | yes |  |
| **System error reports**<br>Error reports queued by services and other accounts. | `%PROGRAMDATA%\Microsoft\Windows\WER\ReportQueue`<br>`%PROGRAMDATA%\Microsoft\Windows\WER\ReportArchive` | yes | Safe | yes | yes |  |
| **Crash dumps**<br>Memory dumps of applications that crashed. Useful when reporting a bug, otherwise just large. | `%LOCALAPPDATA%\CrashDumps` | yes | Caution | no | no |  |
| **System crash dumps**<br>Memory dumps written when Windows itself crashed. | `%SYSTEMROOT%\Minidump`<br>`%SYSTEMROOT%` | yes | Caution | yes | no | only `*.dmp`, `MEMORY.DMP` |
| **Windows servicing logs**<br>Logs written while Windows installs updates and features. | `%SYSTEMROOT%\Logs\CBS` | yes | Caution | yes | no |  |

## System caches

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Thumbnail and icon caches**<br>Explorer rebuilds them, which makes folders slow to open for a while. | `%LOCALAPPDATA%\Microsoft\Windows\Explorer` | yes | Caution | no | no | only `thumbcache_*.db`, `iconcache_*.db`; files in use are skipped while explorer.exe runs |
| **Shader caches**<br>Compiled graphics shaders. Games and applications recompile them, with stutter the first time. | `%LOCALAPPDATA%\D3DSCache`<br>`%LOCALAPPDATA%\NVIDIA\DXCache`<br>`%LOCALAPPDATA%\NVIDIA\GLCache`<br>`%LOCALAPPDATA%\AMD\DxCache`<br>`%LOCALAPPDATA%\AMD\GLCache` | yes | Caution | no | no |  |
| **Driver installer leftovers**<br>Folders graphics driver installers extract themselves into and never remove. | `%SYSTEMDRIVE%\NVIDIA`<br>`%SYSTEMDRIVE%\AMD`<br>`%SYSTEMDRIVE%\Intel` | yes | Caution | yes | no |  |

## Browsers

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Google Chrome cache**<br>Web content kept for faster loading. It is downloaded again as needed. | `%LOCALAPPDATA%\Google\Chrome\User Data\*\Cache\Cache_Data`<br>`%LOCALAPPDATA%\Google\Chrome\User Data\*\Code Cache`<br>`%LOCALAPPDATA%\Google\Chrome\User Data\*\GPUCache`<br>`%LOCALAPPDATA%\Google\Chrome\User Data\*\Service Worker\CacheStorage`<br>`%LOCALAPPDATA%\Google\Chrome\User Data\*\DawnGraphiteCache`<br>`%LOCALAPPDATA%\Google\Chrome\User Data\*\DawnWebGPUCache`<br>`%LOCALAPPDATA%\Google\Chrome\User Data\GrShaderCache`<br>`%LOCALAPPDATA%\Google\Chrome\User Data\ShaderCache` | yes | Safe | no | yes | files in use are skipped while chrome.exe runs |
| **Microsoft Edge cache**<br>Web content kept for faster loading. It is downloaded again as needed. | `%LOCALAPPDATA%\Microsoft\Edge\User Data\*\Cache\Cache_Data`<br>`%LOCALAPPDATA%\Microsoft\Edge\User Data\*\Code Cache`<br>`%LOCALAPPDATA%\Microsoft\Edge\User Data\*\GPUCache`<br>`%LOCALAPPDATA%\Microsoft\Edge\User Data\*\Service Worker\CacheStorage`<br>`%LOCALAPPDATA%\Microsoft\Edge\User Data\*\DawnGraphiteCache`<br>`%LOCALAPPDATA%\Microsoft\Edge\User Data\*\DawnWebGPUCache`<br>`%LOCALAPPDATA%\Microsoft\Edge\User Data\GrShaderCache`<br>`%LOCALAPPDATA%\Microsoft\Edge\User Data\ShaderCache` | yes | Safe | no | yes | files in use are skipped while msedge.exe runs |
| **Brave cache**<br>Web content kept for faster loading. It is downloaded again as needed. | `%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\*\Cache\Cache_Data`<br>`%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\*\Code Cache`<br>`%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\*\GPUCache`<br>`%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\*\Service Worker\CacheStorage`<br>`%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\*\DawnGraphiteCache`<br>`%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\*\DawnWebGPUCache`<br>`%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\GrShaderCache`<br>`%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\ShaderCache` | yes | Safe | no | yes | files in use are skipped while brave.exe runs |
| **Vivaldi cache**<br>Web content kept for faster loading. It is downloaded again as needed. | `%LOCALAPPDATA%\Vivaldi\User Data\*\Cache\Cache_Data`<br>`%LOCALAPPDATA%\Vivaldi\User Data\*\Code Cache`<br>`%LOCALAPPDATA%\Vivaldi\User Data\*\GPUCache`<br>`%LOCALAPPDATA%\Vivaldi\User Data\*\Service Worker\CacheStorage`<br>`%LOCALAPPDATA%\Vivaldi\User Data\*\DawnGraphiteCache`<br>`%LOCALAPPDATA%\Vivaldi\User Data\*\DawnWebGPUCache`<br>`%LOCALAPPDATA%\Vivaldi\User Data\GrShaderCache`<br>`%LOCALAPPDATA%\Vivaldi\User Data\ShaderCache` | yes | Safe | no | yes | files in use are skipped while vivaldi.exe runs |
| **Firefox cache**<br>Web content Firefox keeps for faster loading. It is downloaded again as needed. | `%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*\cache2`<br>`%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*\startupCache`<br>`%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*\shader-cache` | yes | Safe | no | yes | files in use are skipped while firefox.exe runs |

## Package manager caches

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **npm cache**<br>Packages npm downloaded. Reinstalls fetch them again. | `%LOCALAPPDATA%\npm-cache\_cacache`<br>`%LOCALAPPDATA%\npm-cache\_npx`<br>`%LOCALAPPDATA%\npm-cache\_logs` | yes | Safe | no | yes |  |
| **Yarn cache**<br>Packages Yarn downloaded. | `%LOCALAPPDATA%\Yarn\Cache`<br>`%LOCALAPPDATA%\Yarn\Berry\cache` | yes | Safe | no | yes |  |
| **Corepack and Bun caches**<br>Package manager binaries and packages Node's corepack and Bun downloaded. | `%LOCALAPPDATA%\node\corepack`<br>`%USERPROFILE%\.bun\install\cache` | yes | Safe | no | yes |  |
| **pip cache**<br>Wheels and downloads pip keeps. | `%LOCALAPPDATA%\pip\cache` | yes | Safe | no | yes |  |
| **uv cache**<br>Packages and interpreters uv downloaded. Existing environments keep working. | `%LOCALAPPDATA%\uv\cache` | yes | Safe | no | yes |  |
| **Poetry cache**<br>Packages Poetry downloaded. | `%LOCALAPPDATA%\pypoetry\Cache` | yes | Safe | no | yes |  |
| **NuGet download cache**<br>Package metadata and downloads NuGet keeps between restores. | `%LOCALAPPDATA%\NuGet\v3-cache`<br>`%LOCALAPPDATA%\NuGet\plugins-cache`<br>`%LOCALAPPDATA%\Temp\NuGetScratch` | yes | Safe | no | yes |  |
| **Go build cache**<br>Compiled Go packages. The next build recreates what it needs. | `%LOCALAPPDATA%\go-build` | yes | Safe | no | yes |  |

## Package stores

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **pnpm store**<br>Content-addressable store that pnpm projects hard-link into. | `%LOCALAPPDATA%\pnpm\store`<br>`%LOCALAPPDATA%\pnpm-cache` | report only | Caution | no | no |  |
| **NuGet packages**<br>The global packages folder every restore reads from. | `%USERPROFILE%\.nuget\packages` | report only | Caution | no | no |  |
| **Gradle caches**<br>Dependencies and build outputs Gradle keeps for every project. | `%USERPROFILE%\.gradle\caches` | report only | Caution | no | no |  |
| **Maven repository**<br>Dependencies Maven downloaded for every project. | `%USERPROFILE%\.m2\repository` | report only | Caution | no | no |  |
| **Cargo registry**<br>Crate sources Cargo downloaded. | `%USERPROFILE%\.cargo\registry`<br>`%USERPROFILE%\.cargo\git` | report only | Caution | no | no |  |
| **Go module cache**<br>Module sources Go downloaded. | `%USERPROFILE%\go\pkg\mod` | report only | Caution | no | no |  |

## Editors and IDEs

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Visual Studio Code caches**<br>Caches and logs the editor rebuilds on the next start. | `%APPDATA%\Code\Cache`<br>`%APPDATA%\Code\CachedData`<br>`%APPDATA%\Code\CachedExtensionVSIXs`<br>`%APPDATA%\Code\Code Cache`<br>`%APPDATA%\Code\GPUCache`<br>`%APPDATA%\Code\DawnGraphiteCache`<br>`%APPDATA%\Code\DawnWebGPUCache`<br>`%APPDATA%\Code\logs` | yes | Safe | no | yes | files in use are skipped while Code.exe runs |
| **Visual Studio Code Insiders caches**<br>Caches and logs the editor rebuilds on the next start. | `%APPDATA%\Code - Insiders\Cache`<br>`%APPDATA%\Code - Insiders\CachedData`<br>`%APPDATA%\Code - Insiders\CachedExtensionVSIXs`<br>`%APPDATA%\Code - Insiders\Code Cache`<br>`%APPDATA%\Code - Insiders\GPUCache`<br>`%APPDATA%\Code - Insiders\DawnGraphiteCache`<br>`%APPDATA%\Code - Insiders\DawnWebGPUCache`<br>`%APPDATA%\Code - Insiders\logs` | yes | Safe | no | yes | files in use are skipped while Code - Insiders.exe runs |
| **Cursor caches**<br>Caches and logs the editor rebuilds on the next start. | `%APPDATA%\Cursor\Cache`<br>`%APPDATA%\Cursor\CachedData`<br>`%APPDATA%\Cursor\CachedExtensionVSIXs`<br>`%APPDATA%\Cursor\Code Cache`<br>`%APPDATA%\Cursor\GPUCache`<br>`%APPDATA%\Cursor\DawnGraphiteCache`<br>`%APPDATA%\Cursor\DawnWebGPUCache`<br>`%APPDATA%\Cursor\logs` | yes | Safe | no | yes | files in use are skipped while Cursor.exe runs |
| **Windsurf caches**<br>Caches and logs the editor rebuilds on the next start. | `%APPDATA%\Windsurf\Cache`<br>`%APPDATA%\Windsurf\CachedData`<br>`%APPDATA%\Windsurf\CachedExtensionVSIXs`<br>`%APPDATA%\Windsurf\Code Cache`<br>`%APPDATA%\Windsurf\GPUCache`<br>`%APPDATA%\Windsurf\DawnGraphiteCache`<br>`%APPDATA%\Windsurf\DawnWebGPUCache`<br>`%APPDATA%\Windsurf\logs` | yes | Safe | no | yes | files in use are skipped while Windsurf.exe runs |
| **Visual Studio caches**<br>Component and designer caches that Visual Studio rebuilds on the next start. | `%LOCALAPPDATA%\Microsoft\VisualStudio\*\ComponentModelCache`<br>`%LOCALAPPDATA%\Microsoft\VisualStudio\*\Designer\ShadowCache`<br>`%LOCALAPPDATA%\Temp\SymbolCache`<br>`%LOCALAPPDATA%\Microsoft\VSApplicationInsights` | yes | Safe | no | yes | files in use are skipped while devenv.exe runs |
| **JetBrains logs**<br>Log and temporary files of JetBrains IDEs and Toolbox. | `%LOCALAPPDATA%\JetBrains\*\log`<br>`%LOCALAPPDATA%\JetBrains\*\tmp`<br>`%LOCALAPPDATA%\JetBrains\Toolbox\logs` | yes | Safe | no | yes |  |
| **JetBrains caches and indexes**<br>Project indexes. The IDE rebuilds them, which takes a while on large projects. | `%LOCALAPPDATA%\JetBrains\*\caches`<br>`%LOCALAPPDATA%\JetBrains\*\index`<br>`%LOCALAPPDATA%\JetBrains\Transient` | yes | Caution | no | no | files in use are skipped while idea64.exe, rider64.exe, pycharm64.exe, webstorm64.exe, clion64.exe, goland64.exe, datagrip64.exe, phpstorm64.exe, rubymine64.exe, rustrover64.exe runs |
| **Visual Studio Code extensions**<br>Installed extensions, managed from inside the editor. | `%USERPROFILE%\.vscode\extensions`<br>`%USERPROFILE%\.vscode-insiders\extensions` | report only | Advanced | no | no |  |
| **Cursor extensions**<br>Installed extensions, managed from inside Cursor. | `%USERPROFILE%\.cursor\extensions` | report only | Advanced | no | no |  |
| **Visual Studio Code workspace storage**<br>Per-project state and local history kept by the editor. | `%APPDATA%\Code\User\workspaceStorage`<br>`%APPDATA%\Code\User\History`<br>`%APPDATA%\Cursor\User\workspaceStorage`<br>`%APPDATA%\Cursor\User\History` | report only | Advanced | no | no |  |

## Coding agents

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Claude Code data**<br>Session transcripts, project memory and debug logs of Claude Code. | `%USERPROFILE%\.claude` | report only | Advanced | no | no |  |
| **Codex data**<br>Sessions and configuration of Codex. | `%USERPROFILE%\.codex` | report only | Advanced | no | no |  |
| **Gemini CLI data**<br>Sessions and configuration of Gemini CLI. | `%USERPROFILE%\.gemini` | report only | Advanced | no | no |  |

## Docker Desktop

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Docker Desktop virtual disk**<br>Images, containers, volumes and build cache live inside this disk. It does not shrink by itself. | `%LOCALAPPDATA%\Docker\wsl\data\ext4.vhdx`<br>`%LOCALAPPDATA%\Docker\wsl\disk\docker_data.vhdx`<br>`%LOCALAPPDATA%\Docker\wsl\main\ext4.vhdx`<br>`%LOCALAPPDATA%\Docker\wsl\distro\ext4.vhdx`<br>`%PROGRAMDATA%\DockerDesktop\vm-data\DockerDesktop.vhdx` | report only | Advanced | no | no | Reclaim space from inside Docker: unused images and build cache can be pruned from the Cleanup page. |
| **Docker Desktop logs**<br>Diagnostic logs Docker Desktop keeps. | `%LOCALAPPDATA%\Docker\log` | yes | Safe | no | yes |  |

## WSL

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **WSL virtual disks**<br>The Linux file systems of your distributions. They grow but do not shrink on their own. | `%LOCALAPPDATA%\wsl\*\ext4.vhdx`<br>`%LOCALAPPDATA%\Packages\CanonicalGroupLimited.*\LocalState\ext4.vhdx`<br>`%LOCALAPPDATA%\Packages\TheDebianProject.*\LocalState\ext4.vhdx`<br>`%LOCALAPPDATA%\Packages\KaliLinux.*\LocalState\ext4.vhdx`<br>`%LOCALAPPDATA%\Packages\*SUSE*\LocalState\ext4.vhdx` | report only | Advanced | no | no | wsl --manage <distribution> --set-sparse true lets a disk give space back. |
| **WSL swap file**<br>Swap space of the WSL virtual machine, recreated when WSL starts. | `%LOCALAPPDATA%\Temp\swap.vhdx` | report only | Advanced | no | no |  |

## Windows Update

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Windows Update downloads**<br>Update packages that have already been installed. Windows downloads what it needs again. | `%SYSTEMROOT%\SoftwareDistribution\Download` | yes | Safe | yes | yes | The Windows Update service holds some of these files; WinClean can stop it briefly and restart it. |
| **Delivery Optimization cache**<br>Update content shared with other PCs. Windows manages its size. | `%SYSTEMROOT%\SoftwareDistribution\DeliveryOptimization` | report only | Advanced | no | no |  |
| **Previous Windows installation**<br>Kept for ten days after a feature update so you can go back. | `%SYSTEMDRIVE%\Windows.old` | report only | Advanced | no | no | Remove it from Settings > System > Storage > Temporary files. |
| **Upgrade leftovers**<br>Working folders of Windows setup and recovery. | `%SYSTEMDRIVE%\$WinREAgent`<br>`%SYSTEMDRIVE%\$Windows.~BT`<br>`%SYSTEMDRIVE%\$Windows.~WS`<br>`%SYSTEMDRIVE%\$GetCurrent` | report only | Advanced | no | no | Remove them from Settings > System > Storage > Temporary files. |

## Downloads

| Location | Paths | Cleanable | Risk | Administrator | Preselected | Notes |
|---|---|---|---|---|---|---|
| **Downloads**<br>Everything you downloaded. WinClean never deletes it on its own. | `%USERPROFILE%\Downloads` | report only | Advanced | no | no |  |
| **Old installers in Downloads**<br>Setup programs and archives that have been sitting in Downloads for months. Each one is listed; nothing is preselected. | `%USERPROFILE%\Downloads` | yes | Caution | no | no | only `*.exe`, `*.msi`, `*.msix`, `*.msixbundle`, `*.appx`, `*.zip`, `*.7z`, `*.iso`; moved to the Recycle Bin, not deleted |

## Special categories

| Category | What happens | Risk |
|---|---|---|
| Recycle Bin | The entries of the signed-in account are listed from their `$I` metadata; the bin of each fixed drive is emptied as a whole with the shell's own function. | Caution |
| Docker | `docker system df` reports what is reclaimable; the offered commands are `docker image prune -f`, `docker container prune -f` and `docker builder prune -f`, each run through the local Docker command line. Volumes are never touched. | Caution |
| Stale developer folders | Folders recognised by the rules below whose project has not changed for longer than the stale threshold setting. Deleted recursively, after the project is checked again and the user has typed the folder name. | Advanced |

## Developer folder rules

| Folder | Recognised by | Offered for cleanup |
|---|---|---|
| **node_modules**<br>`node_modules` | one of `package.json` next to it<br>Installed npm packages. npm, pnpm or yarn install recreates them. | yes |
| **Git repository**<br>`.git` | name alone<br>Version history of a project. | no, listed only |
| **Rust build output**<br>`target` | one of `Cargo.toml` next to it<br>Compiled Rust artifacts. cargo build recreates them. | yes |
| **Maven build output**<br>`target` | one of `pom.xml` next to it<br>Compiled Java artifacts. mvn package recreates them. | yes |
| **.NET build output**<br>`bin`, `obj`, `artifacts` | one of `*.csproj`, `*.fsproj`, `*.vbproj`, `*.sln`, `*.slnx`, `Directory.Build.props` next to it<br>Compiled .NET artifacts. dotnet build recreates them. | yes |
| **Visual Studio solution cache**<br>`.vs` | one of `*.sln`, `*.slnx`, `*.csproj` next to it<br>Per-solution caches and settings Visual Studio rebuilds. | yes |
| **Python virtual environment**<br>`.venv`, `venv`, `.env`, `env` | `pyvenv.cfg` inside it<br>Installed Python packages for one project. pip or uv recreates it from the project's requirements. | yes |
| **Python bytecode cache**<br>`__pycache__` | name alone<br>Compiled Python modules. Python recreates them as it runs. | yes |
| **Gradle project cache**<br>`.gradle`, `build` | one of `build.gradle`, `build.gradle.kts`, `settings.gradle`, `settings.gradle.kts` next to it<br>Build cache and outputs of a Gradle project. | yes |
| **Next.js build output**<br>`.next` | one of `package.json` next to it<br>Compiled Next.js application. next build recreates it. | yes |
| **Nuxt build output**<br>`.nuxt`, `.output` | one of `package.json` next to it<br>Compiled Nuxt application. nuxt build recreates it. | yes |
| **Terraform providers**<br>`.terraform` | one of `*.tf` next to it<br>Downloaded providers and modules. terraform init recreates them. | yes |
| **Unity Library**<br>`Library` | all of `Assets`, `ProjectSettings` next to it<br>Imported assets of a Unity project. Unity rebuilds it, which can take a long time. | yes |
| **Android SDK**<br>`Sdk`, `android-sdk`, `Android` | `platform-tools`, `build-tools` inside it<br>Platform tools, build tools and system images. | no, listed only |
| **Tool data in the profile folder**<br>`.nuget`, `.cargo`, `.rustup`, `.gradle`, `.m2`, `.docker`, `.claude`, `.codex`, `.cursor`, `.gemini`, `.vscode`, `.android`, `.pub-cache`, `.npm`, `.yarn`, `.pnpm-store`, `.conda`, `.ollama` | directly in the profile folder<br>Caches, stores and configuration developer tools keep in your user profile. | no, listed only |
