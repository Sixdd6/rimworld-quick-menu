#!/usr/bin/env bash
# Symlink the generated stress mod into the game's Mods folder, or remove the link with --remove.
# Usage: scripts/link-stress-mod.sh [--remove] | [OUT_DIR]    (run scripts/make-stress-mod.sh [N] first)
# Enable in game: Mods list -> "Quick Menu STRESS (TEST ONLY)" after Core/Harmony/Quick Menu. Use a throwaway save.
# Disable: untick it in the Mods list (restart), or run with --remove. Never ship or leave enabled.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/.." && pwd)"
src="$(readlink -f "${1:-$repo/dist/stress/QuickMenuStress}")"
game="${RIMWORLD_DIR:-$HOME/.steam/steam/steamapps/common/RimWorld}"
target="$game/Mods/QuickMenuStress"
if [ "${1:-}" = "--remove" ]; then
  if [ -L "$target" ]; then rm "$target"; echo "removed $target"; else echo "no link at $target"; fi
  exit 0
fi
[ -d "$game" ] || { echo "RimWorld not found at $game (set RIMWORLD_DIR)" >&2; exit 1; }
[ -f "$src/About/About.xml" ] || { echo "Run scripts/make-stress-mod.sh first" >&2; exit 1; }
mkdir -p "$game/Mods"
if [ -L "$target" ]; then
  if [ "$(readlink -f "$target")" = "$(readlink -f "$src")" ]; then echo "already linked: $target"; exit 0; fi
  echo "$target is a symlink elsewhere: $(readlink "$target"). Remove it first." >&2; exit 1
fi
[ -e "$target" ] && { echo "$target exists and is not a symlink. Remove it first." >&2; exit 1; }
ln -s "$src" "$target"
echo "linked: $target -> $src"
echo "Launch with: -mod=brrainz.harmony,sixdd.QuickMenu,sixdd.QuickMenuStress -quicktest"
