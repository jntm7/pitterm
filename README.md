# PitTerm

TUI-based F1 Historical Data Viewer using OpenF1 API.

## Prerequisites

- C++20 compiler
- CMake 3.20+
- Git (for fetching dependencies)

## Build

```bash
cmake -B build
cmake --build build -j$(nproc)  # Linux
cmake --build build -j$(sysctl -n hw.ncpu)  # macOS
```

## Run

```bash
./build/pitterm
```

Press `Ctrl+C` to exit.