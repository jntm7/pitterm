# PitTerm

Cross-platform F1 historical statistics TUI built with C# and .NET.

## Prerequisites

- .NET SDK 10.0+
- A modern terminal emulator (kitty, alacritty, Windows Terminal, iTerm2)

### Terminal Support

Recommended Terminals:

- macOS: iTerm2, kitty, alacritty
- Linux: kitty, alacritty, GNOME Terminal
- Windows: Windows Terminal (PowerShell or pwsh shell)

## Project Structure

- `src/F1Tui` - interactive terminal UI app
- `src/F1.Core` - domain models and application interfaces
- `src/F1.Infrastructure` - API clients and persistence adapters
- `tests/F1.Core.Tests` - unit tests for core logic
- `tests/F1.Infrastructure.Tests` - infrastructure tests

## Installation (One-Line)

### macOS/Linux

```bash
curl -fsSL https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.sh | bash
```

or install a specific version tag:

```bash
curl -fsSL https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.sh | bash -s -- --version v0.1.0
```

### Windows (PowerShell)

```powershell
irm https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.ps1 | iex
```

Install a specific version tag:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.ps1))) -Version v0.1.0
```

## Uninstall

### macOS/Linux

If installed to the default location:

```bash
rm -f "$HOME/.local/bin/pitterm"
```

If you used a custom install directory, remove that binary path instead.

### Windows (PowerShell)

If installed to the default location:

```powershell
Remove-Item "$HOME\.local\bin\pitterm.exe" -Force
```

If you used a custom install directory, remove that binary path instead.

### Optional PATH cleanup

If you added `~/.local/bin` to your PATH only for PitTerm and want to remove it, delete the PATH export line from your shell profile (for example `~/.zshrc` or `~/.bashrc`) and reload the shell.
