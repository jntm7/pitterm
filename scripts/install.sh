#!/usr/bin/env bash
set -euo pipefail

REPO="jntm7/pitterm"
VERSION="latest"
INSTALL_DIR="${PITTERM_INSTALL_DIR:-$HOME/.local/bin}"

usage() {
  cat <<'EOF'
Usage: install.sh [--version <tag>] [--repo <owner/name>] [--install-dir <path>]

Examples:
  curl -fsSL https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.sh | bash
  curl -fsSL https://raw.githubusercontent.com/jntm7/pitterm/main/scripts/install.sh | bash -s -- --version v0.1.0

Environment:
  PITTERM_INSTALL_DIR  Override install directory (default: ~/.local/bin)
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      VERSION="$2"
      shift 2
      ;;
    --repo)
      REPO="$2"
      shift 2
      ;;
    --install-dir)
      INSTALL_DIR="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required." >&2
  exit 1
fi

if ! command -v tar >/dev/null 2>&1; then
  echo "tar is required." >&2
  exit 1
fi

os="$(uname -s)"
arch="$(uname -m)"

case "$os" in
  Darwin)
    os_part="osx"
    ;;
  Linux)
    os_part="linux"
    ;;
  *)
    echo "Unsupported OS: $os" >&2
    exit 1
    ;;
esac

case "$arch" in
  x86_64|amd64)
    arch_part="x64"
    ;;
  arm64|aarch64)
    arch_part="arm64"
    ;;
  *)
    echo "Unsupported architecture: $arch" >&2
    exit 1
    ;;
esac

rid="${os_part}-${arch_part}"

api_base="https://api.github.com/repos/${REPO}/releases"
if [[ "$VERSION" == "latest" ]]; then
  release_json="$(curl -fsSL "${api_base}/latest")"
else
  release_json="$(curl -fsSL "${api_base}/tags/${VERSION}")"
fi

tag="$(printf '%s' "$release_json" | python3 -c 'import sys,json; print(json.load(sys.stdin)["tag_name"])')"
asset_name="pitterm-${tag#v}-${rid}.tar.gz"

download_url="$(printf '%s' "$release_json" | python3 -c 'import sys,json; rel=json.load(sys.stdin); name=sys.argv[1]; assets=rel.get("assets",[]); print(next((a["browser_download_url"] for a in assets if a.get("name")==name), ""))' "$asset_name")"

if [[ -z "$download_url" ]]; then
  echo "Could not find asset ${asset_name} on release ${tag}." >&2
  exit 1
fi

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

archive_path="${tmp_dir}/${asset_name}"
echo "Downloading ${asset_name}..."
curl -fL "$download_url" -o "$archive_path"

extract_dir="${tmp_dir}/extract"
mkdir -p "$extract_dir"
tar -xzf "$archive_path" -C "$extract_dir"

binary_path="$(python3 - <<PY
import pathlib
root = pathlib.Path("${extract_dir}")
candidates = []
for p in root.rglob("*"):
    if p.is_file() and p.name in ("F1Tui", "pitterm"):
        candidates.append(p)
if candidates:
    print(candidates[0])
PY
)"

if [[ -z "$binary_path" ]]; then
  echo "Could not locate binary in extracted archive." >&2
  exit 1
fi

mkdir -p "$INSTALL_DIR"
install -m 0755 "$binary_path" "$INSTALL_DIR/pitterm"

echo "Installed pitterm to ${INSTALL_DIR}/pitterm"
if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
  echo "Note: ${INSTALL_DIR} is not on your PATH."
  echo "Add this to your shell profile:"
  echo "  export PATH=\"${INSTALL_DIR}:\$PATH\""
fi

echo "Run: pitterm"
