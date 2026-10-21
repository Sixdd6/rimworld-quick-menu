#!/usr/bin/env bash
# Perf-trait gates (time + zero allocation): Release, no coverage instrumentation.
# Optional: scripts/perf.sh --bench [BenchmarkDotNet args]  runs the full benchmarks (manual, slow).
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
if [ "${1:-}" = "--bench" ]; then
  shift
  exec dotnet run --project tests/QuickMenu.Core.Benchmarks -c Release -- --filter '*' --memory "$@"
fi
dotnet build QuickMenu.slnx -c Release --no-restore
dotnet run --no-build --project tests/QuickMenu.Core.Tests -c Release -- --filter-trait "Category=Perf"
