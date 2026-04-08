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
