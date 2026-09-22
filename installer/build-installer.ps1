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

$appExe = Join-Path $RepoRoot "MasterKeyVault\publish\MasterKeyVault.exe"
$cliExe = Join-Path $RepoRoot "mkv\publish\mkv.exe"
$icon   = Join-Path $RepoRoot "MasterKeyVault\mkv.ico"
# DLL native OpenCvSharp: dotnet publish incremental co the lam rot file nay,
# neu thieu thi installer se gay (chuc nang quet camera hong) -> fail nhanh.
$requiredNative = @(
  (Join-Path $RepoRoot "MasterKeyVault\publish\OpenCvSharpExtern.dll"),
  (Join-Path $RepoRoot "MasterKeyVault\publish\opencv_videoio_ffmpeg4130_64.dll")
)

foreach ($f in @($appExe, $cliExe, $icon, $Iss)) {
  if (-not (Test-Path -LiteralPath $f)) {
    Write-Error "Thieu file: $f. Hay chay 'make publish-app publish-cli' truoc."
    exit 1
  }
}
foreach ($f in $requiredNative) {
  if (-not (Test-Path -LiteralPath $f)) {
    Write-Error "Thieu DLL native: $f. Xoa thu muc publish va chay lai 'make publish-app' (publish sach) roi build installer."
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
