param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

$Project = "src/F1Tui/F1Tui.csproj"
$BaseDir = "artifacts"
$PublishDir = Join-Path $BaseDir "publish"
$PackageDir = Join-Path $BaseDir "packages"

$Rids = @(
    "linux-x64",
    "linux-arm64",
    "osx-x64",
    "osx-arm64",
    "win-x64"
)

Write-Host "Building PitTerm release artifacts (version $Version)"
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $PackageDir | Out-Null

dotnet restore

foreach ($rid in $Rids) {
    $outDir = Join-Path $PublishDir $rid
    $pkgBase = "pitterm-$Version-$rid"

    Write-Host "Publishing $rid..."
    dotnet publish $Project `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $outDir

    if ($rid.StartsWith("win-")) {
        $zipPath = Join-Path $PackageDir "$pkgBase.zip"
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path (Join-Path $outDir "*") -DestinationPath $zipPath
        Write-Host "Created $zipPath"
    }
    else {
        $tarPath = Join-Path $PackageDir "$pkgBase.tar.gz"
        if (Test-Path $tarPath) { Remove-Item $tarPath -Force }
        tar -czf $tarPath -C $PublishDir $rid
        Write-Host "Created $tarPath"
    }
}

Write-Host "Done. Publish dirs: $PublishDir"
Write-Host "Done. Packages: $PackageDir"
