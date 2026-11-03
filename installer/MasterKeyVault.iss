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
  #define MyAppVersion "1.0.0"
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
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=MasterKeyVault-Setup-{#MyAppVersion}
SetupIconFile=..\assets\{#MyAppIcon}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
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
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "addtopath"; Description: "Them thu muc cai dat vao PATH (de chay lenh `mkv` moi noi)"; GroupDescription: "Tuy chon CLI:"; Flags: checkedonce

[Files]
; TUI va CLI mkv
Source: "..\mkv\publish\{#MyCliExe}"; DestDir: "{app}"; Flags: ignoreversion
; Icon rieng de gan cho shortcut + UninstallDisplayIcon
Source: "..\assets\{#MyAppIcon}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Parameters: "--tui"; IconFilename: "{app}\{#MyAppIcon}"; Comment: "Mo {#MyAppName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Parameters: "--tui"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppIcon}"; Comment: "Mo {#MyAppName}"

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

function IsDotNet8DesktopInstalled(): boolean;
var
  Names: TArrayOfString;
  FindRec: TFindRec;
  I: Integer;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.NETCore.App\8.*'), FindRec) or
     FindFirst(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App\8.*'), FindRec) then
  begin
    FindClose(FindRec);
    Result := True;
    exit;
  end;
  { .NET 8 Desktop Runtime dang ky tai ...\sharedfx\Microsoft.WindowsDesktop.App\8.x }
  if RegGetSubkeyNames(HKEY_LOCAL_MACHINE,
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
    Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if Pos('8.', Names[I]) = 1 then
      begin
        Result := True;
        exit;
      end;
    end;
  end;
  { Thu them nhan x86 tren may 64-bit (registry 32-bit view) }
  if RegGetSubkeyNames(HKEY_LOCAL_MACHINE,
    'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App',
    Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if Pos('8.', Names[I]) = 1 then
      begin
        Result := True;
        exit;
      end;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  Res: Integer;
begin
  Result := True;
  if not IsDotNet8DesktopInstalled() then
  begin
    Res := MsgBox(
      'Chua phat hien .NET 8 Desktop Runtime (x64).' + #13#10 +
      'App can runtime nay de chay (framework-dependent).' + #13#10 + #13#10 +
      'Nhan Yes de mo trang tai .NET 8, No de tiep tuc cai dat.',
      mbConfirmation, MB_YESNO);
    if Res = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, Res);
  end;
end;
