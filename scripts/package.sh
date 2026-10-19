#!/usr/bin/env bash
# Build Release and zip mod/ to dist/QuickMenu-<version>.zip (version from mod/About/About.xml <modVersion>).
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
version="$(sed -n 's:.*<modVersion>\(.*\)</modVersion>.*:\1:p' mod/About/About.xml | head -1)"
[ -n "$version" ] || { echo "modVersion not found in mod/About/About.xml" >&2; exit 1; }
dotnet build QuickMenu.slnx -c Release --no-restore -v q
for dll in QuickMenu.dll QuickMenu.Core.dll; do
  [ -f "mod/1.7/Assemblies/$dll" ] || { echo "$dll missing after build" >&2; exit 1; }
done
[ ! -e mod/1.7/Assemblies/0Harmony.dll ] || { echo "0Harmony.dll must not ship" >&2; exit 1; }
# Privacy: the shipped DLLs must not embed the builder's home or absolute paths (see PathMap in Directory.Build.props).
for dll in QuickMenu.dll QuickMenu.Core.dll; do
  if strings -a "mod/1.7/Assemblies/$dll" | grep -qF -e "$HOME" -e "/home/"; then
    echo "$dll embeds a local path (\$HOME or /home/); check PathMap/DeterministicSourcePaths" >&2; exit 1
  fi
  if strings -a -e l "mod/1.7/Assemblies/$dll" | grep -qF -e "$HOME" -e "/home/"; then
    echo "$dll embeds a local path (UTF-16)" >&2; exit 1
  fi
done
stage="$(mktemp -d)"; trap 'rm -rf "$stage"' EXIT
mkdir -p "$stage/QuickMenu" dist
# Ship only tracked mod files (minus .gitkeep) plus the two build outputs; never the working folder as-is.
while IFS= read -r -d '' f; do
  case "$f" in
    */.gitkeep) continue ;;
    mod/1.7/Assemblies/*) echo "unexpected tracked file in Assemblies: $f" >&2; exit 1 ;;
    *0Harmony*) echo "0Harmony must not ship: $f" >&2; exit 1 ;;
  esac
  [ -f "$f" ] || { echo "tracked file missing: $f" >&2; exit 1; }
  rel="${f#mod/}"; mkdir -p "$stage/QuickMenu/$(dirname "$rel")"; cp "$f" "$stage/QuickMenu/$rel"
done < <(git ls-files -z mod/)
mkdir -p "$stage/QuickMenu/1.7/Assemblies"
cp mod/1.7/Assemblies/QuickMenu.dll mod/1.7/Assemblies/QuickMenu.Core.dll "$stage/QuickMenu/1.7/Assemblies/"
out="$REPO_ROOT/dist/QuickMenu-${version}.zip"
rm -f "$out"
( cd "$stage" && zip -qr "$out" QuickMenu )
echo "created $out"
unzip -l "$out"
