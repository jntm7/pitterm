# PitTerm

Cross-platform F1 historical statistics TUI built with C# and .NET.

## Prerequisites

- .NET SDK 10.0+
- A modern terminal emulator (kitty, alacritty, Windows Terminal, iTerm2)

## Project Structure

- `src/F1Tui` - interactive terminal UI app
- `src/F1.Core` - domain models and application interfaces
- `src/F1.Infrastructure` - API clients and persistence adapters
- `tests/F1.Core.Tests` - unit tests for core logic
- `tests/F1.Infrastructure.Tests` - infrastructure tests

## Setup

```bash
dotnet restore
dotnet build
```

## Run

```bash
dotnet run --project src/F1Tui/F1Tui.csproj
```

Use `PITTERM_ENVIRONMENT=Development` to load `appsettings.Development.json`.

## Test

```bash
dotnet test
```

## Release Packaging (Cross-Platform)

PitTerm can be packaged as self-contained binaries for Linux, macOS, and Windows.

### Build release artifacts (macOS/Linux)

```bash
./scripts/release.sh 0.1.0
```

### Build release artifacts (PowerShell)

```powershell
./scripts/release.ps1 -Version 0.1.0
```

Both scripts publish for:

- `linux-x64`
- `linux-arm64`
- `osx-x64`
- `osx-arm64`
- `win-x64`

Output locations:

- Raw publish output: `artifacts/publish/<rid>/`
- Release archives: `artifacts/packages/`

Archive format:

- Windows: `.zip`
- Linux/macOS: `.tar.gz`

### Manual publish example

```bash
dotnet publish src/F1Tui/F1Tui.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/publish/osx-arm64
```

### Terminal support notes

Recommended terminals:

- macOS: iTerm2, kitty, alacritty
- Linux: kitty, alacritty, GNOME Terminal
- Windows: Windows Terminal (PowerShell or pwsh shell)

If rendering appears odd, switch to Windows Terminal or a modern VT-compatible emulator.

## Installation (One-Liner)

You can continue creating GitHub Releases manually (recommended if you want custom release notes/comments), then let users install from release assets.

### macOS/Linux

```bash
curl -fsSL https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.sh | bash
```

Install a specific version tag:

```bash
curl -fsSL https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.sh | bash -s -- --version v0.1.0
```

### Windows PowerShell

```powershell
irm https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.ps1 | iex
```

Install a specific version tag:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.ps1))) -Version v0.1.0
```

### Recommended release flow

1. Build artifacts: `./scripts/release.sh <version>` or `./scripts/release.ps1 -Version <version>`
2. Create GitHub Release manually and write custom notes
3. Upload files from `artifacts/packages/`
4. Users install with one-line scripts above
