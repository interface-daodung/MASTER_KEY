; Inno Setup script cho mkv terminal vault UI
; Build bang:  make publish   (hoac: make installer)
; Yeu cau: Inno Setup 6 (ISCC.exe). Neu chua co: winget install -e --id JRSoftware.InnoSetup
;
; Hanh vi cai dat:
;   - Cai vao {autopf}\MasterKeyVault (Program Files)
;   - Cai mkv.exe (TUI + CLI) va icon
;   - Tu dong them {app} vao PATH may (HKLM) de go `mkv` o dau cung chay
;   - Tao shortcut Start Menu + Desktop cho app (icon mkv.ico)
;   - Go PATH khi Uninstall

#ifndef MyAppVersion
  #define MyAppVersion "1.2.0"
#endif
#define MyAppName "MasterKeyVault"
#define MyAppPublisher "MasterKeyVault"
#define MyAppExe "mkv.exe"
#define MyCliExe "mkv.exe"
#define MyAppIcon "mkv.ico"

[Setup]
AppId={{7A3B9C1D-4E2F-4A6B-9C0D-1F2A3B4C5D6E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=MasterKeyVault-Setup-{#MyAppVersion}
SetupIconFile=..\assets\{#MyAppIcon}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
ChangesEnvironment=yes
CloseApplications=yes
RestartIfNeededByRun=no
UninstallDisplayIcon={app}\{#MyAppIcon}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]

Name: "addtopath"; Description: "Them thu muc cai dat vao PATH (de chay lenh `mkv` moi noi)"; GroupDescription: "Tuy chon CLI:"; Flags: checkedonce

[Files]
; TUI va CLI mkv
Source: "..\mkv\publish\{#MyCliExe}"; DestDir: "{app}"; Flags: ignoreversion
; Icon rieng de gan cho shortcut + UninstallDisplayIcon
Source: "..\assets\{#MyAppIcon}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]

Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"


[Registry]
; Them {app} vao PATH may (HKLM) khi chon Task addtopath. Check NeedsAddPath tranh trung lap.
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addtopath; Check: NeedsAddPath('{app}')

[Run]
Filename: "{app}\{#MyAppExe}"; Parameters: "--tui"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath) then
  begin
    Result := True;
    exit;
  end;
  { Tim chinh xac, case-insensitive, co dau ; bao quanh de tranh match 1 phan }
  Result := Pos(';' + Uppercase(Param) + ';', ';' + Uppercase(OrigPath) + ';') = 0;
end;

procedure RemovePath(UninstallPathDir: string);
var
  PathStr: string;
  P, DelimPos: Integer;
  Parts, NewPath: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', PathStr) then
    exit;
  NewPath := '';
  P := 1;
  while P <= Length(PathStr) do
  begin
    DelimPos := P;
    while (DelimPos <= Length(PathStr)) and (PathStr[DelimPos] <> ';') do
      DelimPos := DelimPos + 1;
    Parts := Trim(Copy(PathStr, P, DelimPos - P));
    if (Parts <> '') and (CompareText(Parts, UninstallPathDir) <> 0) then
    begin
      if NewPath <> '' then
        NewPath := NewPath + ';';
      NewPath := NewPath + Parts;
    end;
    P := DelimPos + 1;
  end;
  RegWriteExpandStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', NewPath);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemovePath(ExpandConstant('{app}'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  LegacyDir: string;
begin
  if CurStep = ssInstall then
  begin
    LegacyDir := ExpandConstant('{commonpf32}\{#MyAppName}');
    RemovePath(LegacyDir);
    if DirExists(LegacyDir) then
      DelTree(LegacyDir, True, True, True);
  end;
end;
