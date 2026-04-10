#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-0.1.0}"
PROJECT="src/F1Tui/F1Tui.csproj"
BASE_DIR="artifacts"
PUBLISH_DIR="${BASE_DIR}/publish"
PACKAGE_DIR="${BASE_DIR}/packages"

RIDS=(
  "linux-x64"
  "linux-arm64"
  "osx-x64"
  "osx-arm64"
  "win-x64"
)

echo "Building PitTerm release artifacts (version ${VERSION})"
mkdir -p "${PUBLISH_DIR}" "${PACKAGE_DIR}"

dotnet restore

for rid in "${RIDS[@]}"; do
  out_dir="${PUBLISH_DIR}/${rid}"
  pkg_base="pitterm-${VERSION}-${rid}"

  echo "Publishing ${rid}..."
  dotnet publish "${PROJECT}" \
    -c Release \
    -r "${rid}" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o "${out_dir}"

  if [[ "${rid}" == win-* ]]; then
    python3 - <<PY
import pathlib, zipfile
root = pathlib.Path("${out_dir}")
zip_path = pathlib.Path("${PACKAGE_DIR}/${pkg_base}.zip")
with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
    for p in root.rglob("*"):
        if p.is_file():
            zf.write(p, pathlib.Path("${pkg_base}") / p.relative_to(root))
print(f"Created {zip_path}")
PY
  else
    tar_path="${PACKAGE_DIR}/${pkg_base}.tar.gz"
    tar -czf "${tar_path}" -C "${PUBLISH_DIR}" "${rid}"
    echo "Created ${tar_path}"
  fi
done

echo "Done. Publish dirs: ${PUBLISH_DIR}"
echo "Done. Packages: ${PACKAGE_DIR}"
