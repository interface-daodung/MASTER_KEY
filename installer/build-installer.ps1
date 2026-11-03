#Requires -Version 5.1
<#
  Build installer MasterKeyVault-Setup-<version>.exe tu thu muc publish/ bang Inno Setup 6.
  Dung chung cho: make installer / make publish
  Tham so: -Version "1.0.0"
#>
param(
  [string]$Version = "1.0.0",
  [string]$Iss = (Join-Path $PSScriptRoot "MasterKeyVault.iss"),
  [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Stop"

function Find-Iscc {
  $c = Get-Command ISCC.exe -ErrorAction SilentlyContinue
  if ($c) { return $c.Source }
  $localPrograms = Join-Path $env:LOCALAPPDATA "Programs"
  foreach ($p in @(
    (Join-Path $localPrograms "Inno Setup 6\ISCC.exe"),
    (Join-Path $localPrograms "Inno Setup 7\ISCC.exe"),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 7\ISCC.exe"
  )) {
    if (Test-Path -LiteralPath $p) { return $p }
  }
  return $null
}

$cliExe = Join-Path $RepoRoot "mkv\publish\mkv.exe"
$icon   = Join-Path $RepoRoot "MasterKeyVault\mkv.ico"

foreach ($f in @($cliExe, $icon, $Iss)) {
  if (-not (Test-Path -LiteralPath $f)) {
    Write-Error "Thieu file: $f. Hay chay 'make publish-cli' truoc."
    exit 1
  }
}

$iscc = Find-Iscc
if (-not $iscc) {
  Write-Error @"
Khong tim thay ISCC.exe (Inno Setup 6). Cai bang:
  winget install -e --id JRSoftware.InnoSetup
Roi chay lai: make installer
"@
  exit 1
}

Write-Host "Dung ISCC: $iscc"
& "$iscc" "/DMyAppVersion=$Version" "$Iss"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$dist = Join-Path $RepoRoot "dist"
Get-ChildItem -LiteralPath $dist -Filter "MasterKeyVault-Setup-*.exe" | ForEach-Object {
  Write-Host ("OK: " + $_.FullName + " (" + [math]::Round($_.Length / 1MB, 2) + " MB)")
}
