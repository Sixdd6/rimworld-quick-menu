#!/usr/bin/env bash
# Symlink mod/ into the game's Mods folder as Mods/QuickMenu. Run once per machine. Idempotent.
# RIMWORLD_DIR defaults to ~/.steam/steam/steamapps/common/RimWorld.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/.." && pwd)"
game="${RIMWORLD_DIR:-$HOME/.steam/steam/steamapps/common/RimWorld}"
[ -d "$game" ] || { echo "RimWorld not found at $game (set RIMWORLD_DIR)" >&2; exit 1; }
mkdir -p "$game/Mods"
target="$game/Mods/QuickMenu"
if [ -L "$target" ]; then
  if [ "$(readlink -f "$target")" = "$repo/mod" ]; then echo "already linked: $target -> $repo/mod"; exit 0; fi
  echo "$target is a symlink to somewhere else: $(readlink "$target"). Remove it first." >&2; exit 1
fi
[ -e "$target" ] && { echo "$target exists and is not a symlink. Remove it first." >&2; exit 1; }
ln -s "$repo/mod" "$target"
echo "linked: $target -> $repo/mod"
echo "Launch with: -mod=brrainz.harmony,sixdd.QuickMenu -quicktest"
