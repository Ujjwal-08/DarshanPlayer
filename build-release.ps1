<#
  build-release.ps1 — one-shot optimized release build for Darshan Player.

  Pipeline:
    1. dotnet publish (self-contained, ReadyToRun) -> publish\
    2. Generate VLC plugins.dat cache  -> kills the 323-DLL scan on every launch
    3. vpk pack (Velopack)             -> Releases\  (auto-update + setup exe)

  Usage:
    .\build-release.ps1 -Version 1.1.0
    .\build-release.ps1 -Version 1.2.0 -SkipPack   # build+cache only, no Velopack

  Requires: dotnet SDK, vpk (dotnet tool install -g vpk).
  vlc-cache-gen.exe is fetched once into .vlctool\ and reused.
#>

param(
    [Parameter(Mandatory = $true)] [string]$Version,
    [switch]$SkipPack
)

$ErrorActionPreference = "Stop"
$root      = $PSScriptRoot
$publish   = Join-Path $root "publish"
$releases  = Join-Path $root "Releases"
$libvlcDir = Join-Path $publish "libvlc\win-x64"
$pluginDir = Join-Path $libvlcDir "plugins"
$toolDir   = Join-Path $root ".vlctool"
$cacheGen  = Join-Path $toolDir "vlc-cache-gen.exe"

# --- 1. Publish ----------------------------------------------------------------
Write-Host "==> [1/3] dotnet publish (ReadyToRun, self-contained)..." -ForegroundColor Cyan
dotnet publish (Join-Path $root "DarshanPlayer.csproj") `
    -c Release -r win-x64 --self-contained -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# --- 2. Plugin cache -----------------------------------------------------------
Write-Host "==> [2/3] Generating VLC plugin cache (plugins.dat)..." -ForegroundColor Cyan

# IMPORTANT: cache-gen MUST be the same VLC version as the VideoLAN.LibVLC.Windows
# nuget (currently 3.0.23), AND must run from its OWN extracted folder (next to its
# native libvlccore), pointed at our plugins via an ABSOLUTE path. Running the exe
# next to the nuget core writes an EMPTY plugins.dat — do not do that.
$vlcVer  = "3.0.23"
$vlcHome = Join-Path $toolDir "vlc-$vlcVer"   # full extracted VLC, kept for reuse
$cacheGen = Join-Path $vlcHome "vlc-cache-gen.exe"

if (-not (Test-Path $cacheGen)) {
    Write-Host "    Downloading VLC $vlcVer (one-time, for vlc-cache-gen)..."
    New-Item -ItemType Directory -Force $toolDir | Out-Null
    $zip = Join-Path $toolDir "vlc.zip"
    Invoke-WebRequest -TimeoutSec 300 `
        -Uri "https://download.videolan.org/pub/videolan/vlc/$vlcVer/win64/vlc-$vlcVer-win64.zip" `
        -OutFile $zip
    $extract = Join-Path $toolDir "extract"
    Expand-Archive -Path $zip -DestinationPath $extract -Force
    $home = Get-ChildItem $extract -Recurse -Filter "vlc-cache-gen.exe" | Select-Object -First 1
    if (-not $home) { throw "vlc-cache-gen.exe not found in VLC archive" }
    Move-Item $home.Directory.FullName $vlcHome -Force
    Remove-Item $zip, $extract -Recurse -Force -ErrorAction SilentlyContinue
}

Remove-Item (Join-Path $pluginDir "plugins.dat") -Force -ErrorAction SilentlyContinue
Push-Location $vlcHome
try {
    & ".\vlc-cache-gen.exe" $pluginDir   # absolute path to OUR plugins
} finally {
    Pop-Location
}

$dat = Join-Path $pluginDir "plugins.dat"
if (Test-Path $dat) {
    Write-Host "    plugins.dat created ($([math]::Round((Get-Item $dat).Length/1KB,1)) KB) — fast startup enabled." -ForegroundColor Green
} else {
    Write-Warning "    plugins.dat NOT created — startup will still scan plugins."
}

# --- 3. Velopack pack ----------------------------------------------------------
if ($SkipPack) {
    Write-Host "==> [3/3] Skipped (-SkipPack). publish\ is ready." -ForegroundColor Yellow
    return
}

Write-Host "==> [3/3] vpk pack $Version..." -ForegroundColor Cyan
# packId must stay "DarshanPlayer" forever: it is the install folder and the update identity.
# packTitle/packAuthors/icon are what users see in Start, on the Desktop and in Apps & Features.
vpk pack --packId DarshanPlayer --packVersion $Version `
    --packTitle "Darshan Player" --packAuthors "Ujjwal Dadhich" `
    --icon (Join-Path $root "icon.ico") `
    --shortcuts "Desktop,StartMenuRoot" `
    --packDir $publish --mainExe DarshanPlayer.exe --outputDir $releases
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

Write-Host "`nDone. Artifacts in $releases" -ForegroundColor Green
Write-Host "Next: gh release create v$Version Releases\* --repo Ujjwal-08/DarshanPlayer" -ForegroundColor Green
