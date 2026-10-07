; WinClean installer. Built by the release workflow with:
;   ISCC.exe /DMyAppVersion=0.1.0 /DMyAppFileVersion=0.1.0.0 /DMyArch=x64 /DPublishDir=..\artifacts\publish\win-x64 installer\WinClean.iss
; The AppId never changes: both architectures share it so an upgrade replaces the previous build.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef MyAppFileVersion
  #define MyAppFileVersion "0.0.0.0"
#endif
#ifndef MyArch
  #define MyArch "x64"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-" + MyArch
#endif

#define MyAppName "WinClean"
#define MyAppPublisher "Solenjoyer"
#define MyAppUrl "https://github.com/Solenjoyer/WinClean"
#define MyAppExeName "WinClean.exe"

[Setup]
AppId={{8C1D2A64-3F57-4E0B-9B52-7D1A6C4E2F90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}/issues
AppUpdatesURL={#MyAppUrl}/releases
VersionInfoVersion={#MyAppFileVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
OutputBaseFilename=WinClean-{#MyAppVersion}-win-{#MyArch}-setup
OutputDir=..\artifacts\release
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0.19041
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
AppMutex=WinClean.3E2F0C58-7B6A-4D1E-9C44-5A1F0B2D8E61
LicenseFile=..\LICENSE
#if MyArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
; SignTool=signtool $f   (enabled by the release workflow once a certificate exists)

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Start {#MyAppName} when I sign in"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; The same value the Settings page writes, so the two stay in step.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// An interactive uninstall asks whether to keep settings and logs; a silent one (winget) keeps them.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
  begin
    DataDir := ExpandConstant('{localappdata}\WinClean');
    if DirExists(DataDir) then
    begin
      if MsgBox('Delete WinClean settings and logs (' + DataDir + ')?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
