<#
.SYNOPSIS
Publishes WinClean for one architecture, zips the portable build and builds the installer.

.EXAMPLE
pwsh eng/build-release.ps1 -Rid win-x64 -Version 0.1.0
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Rid = 'win-x64',

    [string] $Version = '0.0.0-local',

    [switch] $SkipInstaller
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "artifacts/publish/$Rid"
$releaseDir = Join-Path $root 'artifacts/release'
$numericVersion = ($Version -split '-')[0]
$arch = $Rid.Substring(4)

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

dotnet publish (Join-Path $root 'src/WinClean/WinClean.csproj') -c Release -r $Rid -p:Version=$Version -p:RestoreLockedMode=true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }

$portable = Join-Path $releaseDir "WinClean-$Version-$Rid-portable.zip"
$staging = Join-Path $root "artifacts/portable/$Rid"
Remove-Item -Recurse -Force $staging -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $staging | Out-Null
Copy-Item (Join-Path $publishDir 'WinClean.exe') $staging
Copy-Item (Join-Path $root 'LICENSE') (Join-Path $staging 'LICENSE.txt')
Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') $staging
New-Item -ItemType File -Force -Path (Join-Path $staging 'portable') | Out-Null
Remove-Item $portable -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $portable -CompressionLevel Optimal
Write-Host "Portable: $portable"

if (-not $SkipInstaller) {
    $iscc = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'
    if (-not (Test-Path $iscc)) {
        Write-Warning 'Inno Setup 6 is not installed; the installer was skipped.'
        return
    }

    & $iscc "/DMyAppVersion=$Version" "/DMyAppFileVersion=$numericVersion.0" "/DMyArch=$arch" "/DPublishDir=$publishDir" (Join-Path $root 'installer/WinClean.iss')
    if ($LASTEXITCODE -ne 0) { throw 'the installer build failed' }
    Write-Host "Installer: $releaseDir/WinClean-$Version-win-$arch-setup.exe"
}
