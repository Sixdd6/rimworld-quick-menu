#!/usr/bin/env bash
# SessionStart hook: prints project status into Claude's context and, in
# Claude Code cloud sessions, runs the project's idempotent setup script.
set -uo pipefail
cd "${CLAUDE_PROJECT_DIR:-.}" || exit 0

if [ "${CLAUDE_CODE_REMOTE:-}" = "true" ] && [ -x scripts/setup.sh ]; then
  echo "Running scripts/setup.sh (cloud session)..."
  if ! scripts/setup.sh >/tmp/claude-setup.log 2>&1; then
    echo "WARNING: scripts/setup.sh failed. See /tmp/claude-setup.log (last lines below)."
    tail -n 20 /tmp/claude-setup.log
  fi
fi

status=docs/project/STATUS.md
if [ -f "$status" ]; then
  echo "=== Agent team: project status (from $status) ==="
  sed -n '1,/^## Open questions/p' "$status" | head -n 30
  if [ -f docs/project/plan.md ]; then
    done_n=$(grep -cE '^\s*- \[x\] \*\*T-' docs/project/plan.md || true)
    total_n=$(grep -cE '^\s*- \[.\] \*\*T-' docs/project/plan.md || true)
    echo "Tasks done: ${done_n}/${total_n}"
  fi
  echo "Continue from the current phase (see CLAUDE.md). /project-status gives a full summary."
elif git remote get-url origin 2>/dev/null | grep -q 'Sixdd6/claude-agents-template'; then
  echo "=== This is the agent-team template repo itself, not a project. Read .claude/template-maintenance.md. Do not run /kickoff here. ==="
else
  echo "=== Agent team template: no project started. Run /kickoff \"<your idea>\" to begin. ==="
fi
exit 0
