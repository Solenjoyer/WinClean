# Security

## Supported versions

Only the latest release is supported. Older releases do not receive fixes.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting on this repository rather than a public issue: open the Security tab and choose "Report a vulnerability". You will get an acknowledgement within seven days.

Include the WinClean version, the Windows version, whether WinClean was running as administrator, and the steps to reproduce.

## Scope

Reports that matter most:

- Cleanup deleting anything outside the folders listed in `docs/CLEANUP-RULES.md`, or deleting without the preview and the confirmation.
- Path handling that lets a crafted file name or link escape a cleanup root.
- Misuse of administrator rights, including the restart-as-administrator flow.
- Anything that makes WinClean open a network connection on its own.

## What WinClean does not do

WinClean ships no kernel driver and installs nothing on its own. CPU and motherboard sensors are read through the user-installed [PawnIO](https://pawnio.eu) driver only when sensors are enabled in Settings and WinClean runs as administrator.
