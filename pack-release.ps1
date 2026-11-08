<#
.SYNOPSIS
    Builds the HudlReader release assets into Releases/:
      - HudlReader-win-Setup.exe        Velopack installer for the desktop app (HudlReader.UI)
      - HudlReader.Cli-win-x64.zip      Self-contained command-line tool + README.txt

.DESCRIPTION
    Reads the 4-part version (Year.Month.Build.Day) from HudlReader.UI.csproj (or from -Version,
    if passed) and repacks it as a 3-part SemVer2 version (Year.Month.Day) with the build number
    as build metadata (e.g. 2026.11.1.8 -> 2026.11.8+1), since Velopack requires a strict
    3-part SemVer2 package version.

    Requires the Velopack CLI: dotnet tool install -g vpk
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    # Overrides the 4-part version (Year.Month.Build.Day) instead of reading it from
    # HudlReader.UI.csproj -- used by CI to inject a version computed from the run number / tag.
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

$uiCsproj = Join-Path $PSScriptRoot "HudlReader.UI\HudlReader.UI.csproj"
$cliCsproj = Join-Path $PSScriptRoot "HudlReader.Cli\HudlReader.Cli.csproj"

if ($Version) {
    $fullVersion = $Version
} else {
    [xml]$csproj = Get-Content $uiCsproj
    $versionNode = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    $fullVersion = "$versionNode"
}
$parts = $fullVersion -split '\.'
if ($parts.Count -lt 4) {
    throw "Expected a 4-part version (Year.Month.Build.Day), got '$fullVersion'."
}
# Year.Month.Build.Day -> Year.Month.Day+Build (Velopack requires a 3-part SemVer2 core version;
# the build number is kept as build metadata rather than dropped).
$packVersion = "$($parts[0]).$($parts[1]).$($parts[3])+$($parts[2])"

$versionSource = if ($Version) { "-Version override" } else { "csproj Version" }
Write-Host "Packaging HudlReader version $packVersion (from $versionSource $fullVersion)..."

$publishRoot = Join-Path $PSScriptRoot "publish-temp"
$uiPublishDir = Join-Path $publishRoot "ui"
$cliPublishDir = Join-Path $publishRoot "cli"
$releasesDir = Join-Path $PSScriptRoot "Releases"

if (Test-Path $publishRoot) { Remove-Item $publishRoot -Recurse -Force }
# vpk refuses to pack a version <= one already in the output folder, so start clean each run
# (CI always starts empty; auto-update/delta packages aren't used, so nothing is lost).
if (Test-Path $releasesDir) { Remove-Item $releasesDir -Recurse -Force }
New-Item -ItemType Directory -Force $releasesDir | Out-Null

# --- Desktop app: framework-dependent single-file exe, wrapped in a Velopack installer ---
dotnet publish $uiCsproj -c $Configuration -r $Runtime --no-self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$fullVersion -o $uiPublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (UI) failed." }

vpk pack `
    -u HudlReader `
    -v $packVersion `
    -p $uiPublishDir `
    -e HudlReader.UI.exe `
    -o $releasesDir `
    --packTitle "HudlReader" `
    --packAuthors "dabeir" `
    --icon (Join-Path $PSScriptRoot "HudlReader.UI\Assets\hudlreader.ico")
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed." }

# --- Command-line tool: self-contained single-file exe (settings live in the .csproj), zipped ---
dotnet publish $cliCsproj -c $Configuration -p:Version=$fullVersion -o $cliPublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (CLI) failed." }

Copy-Item (Join-Path $PSScriptRoot "HudlReader.Cli\README.txt") $cliPublishDir
$cliZip = Join-Path $releasesDir "HudlReader.Cli-win-x64.zip"
if (Test-Path $cliZip) { Remove-Item $cliZip -Force }
Compress-Archive -Path (Join-Path $cliPublishDir "HudlReader.Cli.exe"), (Join-Path $cliPublishDir "README.txt") `
    -DestinationPath $cliZip

Write-Host ""
Write-Host "Release assets created in ${releasesDir}:"
Write-Host "  HudlReader-win-Setup.exe     (desktop app installer)"
Write-Host "  HudlReader.Cli-win-x64.zip   (command-line tool)"
Write-Host "(A warning about the legacy 'RELEASES' file omitting an asset with a '+' in its version is expected and harmless.)"
