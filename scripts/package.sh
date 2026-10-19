#!/usr/bin/env bash
# Build Release and zip mod/ to dist/QuickMenu-<version>.zip (version from mod/About/About.xml <modVersion>).
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
version="$(sed -n 's:.*<modVersion>\(.*\)</modVersion>.*:\1:p' mod/About/About.xml | head -1)"
[ -n "$version" ] || { echo "modVersion not found in mod/About/About.xml" >&2; exit 1; }
dotnet build QuickMenu.slnx -c Release --no-restore -v q
[ -f mod/1.7/Assemblies/QuickMenu.dll ] || { echo "QuickMenu.dll missing after build" >&2; exit 1; }
[ ! -e mod/1.7/Assemblies/0Harmony.dll ] || { echo "0Harmony.dll must not ship" >&2; exit 1; }
stage="$(mktemp -d)"; trap 'rm -rf "$stage"' EXIT
mkdir -p "$stage/QuickMenu" dist
# Ship content only: no pdb, no .gitkeep.
tar -C mod --exclude='*.pdb' --exclude='.gitkeep' -cf - . | tar -C "$stage/QuickMenu" -xf -
out="$REPO_ROOT/dist/QuickMenu-${version}.zip"
rm -f "$out"
( cd "$stage" && zip -qr "$out" QuickMenu )
echo "created $out"
unzip -l "$out"
