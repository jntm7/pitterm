# PitTerm

Cross-platform F1 historical statistics TUI built with C# and .NET.

## Installation

Download the latest release for your platform from [Releases](https://github.com/jntm7/pitterm/releases):

| Platform | File |
|---|---|
| macOS (Apple Silicon) | `pitterm-osx-arm64.tar.gz` |
| Linux (x64) | `pitterm-linux-x64.tar.gz` |
| Windows (x64) | `pitterm-win-x64.zip` |

Extract and run — no .NET runtime required.

### macOS / Linux

```bash
tar -xzf pitterm-osx-arm64.tar.gz
chmod +x F1Tui
./F1Tui
```

### Windows

Extract `pitterm-win-x64.zip`, then run `F1Tui.exe`.

## Terminal Support

Recommended terminals:

- **macOS**: iTerm2, kitty, alacritty
- **Linux**: kitty, alacritty, GNOME Terminal
- **Windows**: Windows Terminal (PowerShell or pwsh)

### Data Sources

#### Historical Session Data

- [OpenF1 API](https://openf1.org/)

#### Driver Profile Info 

- [Jolpica F1 API](https://github.com/jolpica/jolpica-f1/)
