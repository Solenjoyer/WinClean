<#
.SYNOPSIS
Signs one or more files with the certificate held in CODE_SIGNING_PFX (base64) and CODE_SIGNING_PASSWORD.
The release workflow calls this only when the HAS_CERT repository variable is true.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, ValueFromRemainingArguments)]
    [string[]] $Path
)

$ErrorActionPreference = 'Stop'

if (-not $env:CODE_SIGNING_PFX) { throw 'CODE_SIGNING_PFX is not set.' }

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}/Windows Kits/10/bin/*/x64/signtool.exe" |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if (-not $signtool) { throw 'signtool.exe was not found in the Windows SDK.' }

$pfx = Join-Path $env:RUNNER_TEMP 'codesign.pfx'
[IO.File]::WriteAllBytes($pfx, [Convert]::FromBase64String($env:CODE_SIGNING_PFX))
try {
    & $signtool.FullName sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /f $pfx /p $env:CODE_SIGNING_PASSWORD $Path
    if ($LASTEXITCODE -ne 0) { throw 'signtool failed' }
}
finally {
    Remove-Item $pfx -Force
}
