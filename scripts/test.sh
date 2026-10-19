#!/usr/bin/env bash
# Functional tests (Category!=Perf) with coverage on QuickMenu.Core and a line-coverage gate.
# Usage: scripts/test.sh [-c Release|Debug] ; MIN_COVERAGE=90 by default.
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
config="Release"; [ "${1:-}" = "-c" ] && config="${2:-Release}"
min="${MIN_COVERAGE:-90}"

# FR-17: nothing is written into save files. No game/world/map component and no Def subclass may exist in src/.
# Comment lines are ignored; only declarations and base-type uses count.
violations="$(grep -rnE --include='*.cs' '(:|,)[[:space:]]*(Verse\.)?(GameComponent|WorldComponent|MapComponent)\b|class[[:space:]]+[A-Za-z0-9_]+[[:space:]]*:[[:space:]]*([A-Za-z0-9_.]*\.)?[A-Za-z0-9_]*Def\b' src/ | grep -vE '^[^:]+:[0-9]+:[[:space:]]*(//|\*|/\*)' || true)"
if [ -n "$violations" ]; then
  echo "save-safety check FAILED (FR-17): save-file components or Def classes found in src/:" >&2
  echo "$violations" >&2
  exit 1
fi
echo "save-safety check: no GameComponent/WorldComponent/MapComponent/Def subclasses in src/"

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
