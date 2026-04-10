param(
    [string]$Version = "latest",
    [string]$Repo = "jntm7/pitterm",
    [string]$InstallDir = "$HOME\\.local\\bin"
)

$ErrorActionPreference = "Stop"

function Get-ArchPart {
    switch ($env:PROCESSOR_ARCHITECTURE.ToLower()) {
        "amd64" { return "x64" }
        "arm64" { return "arm64" }
        default { throw "Unsupported architecture: $($env:PROCESSOR_ARCHITECTURE)" }
    }
}

if (-not ($IsWindows)) {
    throw "This installer script is for Windows PowerShell/pwsh."
}

$rid = "win-$(Get-ArchPart)"
$apiBase = "https://api.github.com/repos/$Repo/releases"

if ($Version -eq "latest") {
    $release = Invoke-RestMethod -Uri "$apiBase/latest"
} else {
    $release = Invoke-RestMethod -Uri "$apiBase/tags/$Version"
}

$tag = $release.tag_name
$assetName = "pitterm-$($tag.TrimStart('v'))-$rid.zip"
$asset = $release.assets | Where-Object { $_.name -eq $assetName } | Select-Object -First 1

if (-not $asset) {
    throw "Could not find asset $assetName in release $tag"
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

$tmpRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("pitterm-install-" + [guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $tmpRoot $assetName
$extractPath = Join-Path $tmpRoot "extract"

New-Item -ItemType Directory -Force -Path $tmpRoot | Out-Null
New-Item -ItemType Directory -Force -Path $extractPath | Out-Null

try {
    Write-Host "Downloading $assetName..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath

    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

    $binary = Get-ChildItem -Path $extractPath -Recurse -File |
        Where-Object { $_.Name -ieq "F1Tui.exe" -or $_.Name -ieq "pitterm.exe" } |
        Select-Object -First 1

    if (-not $binary) {
        $binary = Get-ChildItem -Path $extractPath -Recurse -File | Select-Object -First 1
    }

    if (-not $binary) {
        throw "Could not locate binary in extracted archive."
    }

    $destPath = Join-Path $InstallDir "pitterm.exe"
    Copy-Item -Path $binary.FullName -Destination $destPath -Force

    Write-Host "Installed pitterm to $destPath"
    Write-Host "Run: pitterm"
}
finally {
    if (Test-Path $tmpRoot) {
        Remove-Item -Path $tmpRoot -Recurse -Force
    }
}
