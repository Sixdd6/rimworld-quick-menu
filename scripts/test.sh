#!/usr/bin/env bash
# Functional tests (Category!=Perf) with coverage on QuickMenu.Core and a line-coverage gate.
# Usage: scripts/test.sh [-c Release|Debug] ; MIN_COVERAGE=90 by default.
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
config="Release"; [ "${1:-}" = "-c" ] && config="${2:-Release}"
min="${MIN_COVERAGE:-90}"

# FR-17: nothing is written into save files. No saved component or Def-like class may exist in src/, and nothing may
# implement saving outside src/QuickMenu/Settings/ (the mod settings are the only state). Comment lines are ignored.
# SRC_DIR can be overridden to test the check itself against synthetic sources.
src_dir="${SRC_DIR:-src/}"
settings_dir="${SETTINGS_DIR:-src/QuickMenu/Settings/}"
ns='(global::)?([A-Za-z0-9_]+\.)*'
comps='(GameComponent|WorldComponent|MapComponent|ThingComp|HediffComp|WorldObject|GameCondition)[A-Za-z0-9_]*'
defs='[A-Za-z0-9_]*(Def|Thing|Pawn|Building)'
base_re="class[[:space:]]+[A-Za-z0-9_]+[^:{]*:[^{]*[[:space:],]${ns}(${comps}|${defs})([^A-Za-z0-9_]|\$)"
not_comment='^[^:]+:[0-9]+:[[:space:]]*(//|\*|/\*)'
scan() { # scan <pattern>: matching lines into $scan_out; no match is fine, a grep error (exit 2) fails the script
  local rc=0
  scan_out="$(grep -rnE --include='*.cs' "$1" "$src_dir")" || rc=$?
  [ "$rc" -le 1 ] || { echo "save-safety check: grep failed (exit $rc)" >&2; exit 2; }
  scan_out="$(printf '%s\n' "$scan_out" | grep -vE "$not_comment" || true)"
}
scan "$base_re"
if [ -n "$scan_out" ]; then
  echo "save-safety check FAILED (FR-17): save-file components or Def/Thing classes found in src/:" >&2
  echo "$scan_out" >&2
  exit 1
fi
scan '(ExposeData|Scribe_[A-Za-z]+|Scribe\.|IExposable)'
exposure="$(printf '%s\n' "$scan_out" | awk -v d="$settings_dir" 'NF && index($0, d) != 1')"
if [ -n "$exposure" ]; then
  echo "save-safety check FAILED (FR-17): ExposeData/Scribe_/IExposable used outside ${settings_dir}:" >&2
  echo "$exposure" >&2
  exit 1
fi
echo "save-safety check: no saved components, Def/Thing subclasses or Scribe use outside Settings in ${src_dir}"
[ -z "${SAVE_CHECK_ONLY:-}" ] || exit 0

rm -rf TestResults
dotnet build QuickMenu.slnx -c "$config" --no-restore
dotnet run --no-build --project tests/QuickMenu.Core.Tests -c "$config" -- \
  --filter-not-trait "Category=Perf" \
  --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml \
  --coverage-settings "$REPO_ROOT/tests/QuickMenu.Core.Tests/coverage.settings" \
  --results-directory "$REPO_ROOT/TestResults"

report="TestResults/coverage.cobertura.xml"
[ -f "$report" ] || { echo "coverage gate: no report at $report" >&2; exit 1; }
rate="$(grep -o '<coverage [^>]*line-rate="[0-9.]*"' "$report" | grep -o 'line-rate="[0-9.]*"' | head -1 | tr -dc '0-9.')"
if grep -q '<packages */>' "$report"; then
  echo "coverage gate: report has no instrumented code yet (Core has no executable lines). Gate is vacuous until Core has code."
  exit 0
fi
pct="$(awk -v r="$rate" 'BEGIN { printf "%.1f", r * 100 }')"
echo "coverage gate: QuickMenu.Core line coverage ${pct}% (minimum ${min}%)"
awk -v p="$pct" -v m="$min" 'BEGIN { exit (p + 0 >= m + 0) ? 0 : 1 }' || { echo "coverage gate FAILED" >&2; exit 1; }
