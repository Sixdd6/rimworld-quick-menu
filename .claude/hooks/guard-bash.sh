#!/usr/bin/env bash
# PreToolUse guard for Bash commands.
# - Blocks destructive commands (force-push to main/master, rm -rf on / or ~).
# - Blocks publish/deploy/tag-push commands unless .claude/.publish-approved exists
#   (the /publish skill creates it only after explicit user confirmation).
# Exit code 2 blocks the call; stderr is shown to Claude.
set -uo pipefail

input="$(cat)"
if command -v jq >/dev/null 2>&1; then
  cmd="$(printf '%s' "$input" | jq -r '.tool_input.command // empty')"
elif command -v python3 >/dev/null 2>&1; then
  cmd="$(printf '%s' "$input" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("tool_input",{}).get("command",""))')"
else
  cmd="$input"
fi
[ -z "$cmd" ] && exit 0

block() { echo "BLOCKED by .claude/hooks/guard-bash.sh: $1" >&2; exit 2; }

# Destructive operations. Force-push is checked per chained command
# (split on && || ; | and newlines) so "git push --force feat && git log main"
# is not a false positive.
while IFS= read -r seg; do
  if printf '%s' "$seg" | grep -Eq 'git[[:space:]]+push' \
    && printf '%s' "$seg" | grep -Eq '(--force|[[:space:]]-f([[:space:]]|$)|[[:space:]]\+)' \
    && printf '%s' "$seg" | grep -Eq '(^|[[:space:]:/+])(main|master)([[:space:]]|$)'; then
    block "force-push to main/master is not allowed."
  fi
done < <(printf '%s\n' "$cmd" | awk '{ gsub(/&&|\|\||;|\|/, "\n"); print }')
if printf '%s' "$cmd" | grep -Eq 'rm[[:space:]]+-[a-zA-Z]*r[a-zA-Z]*f?[a-zA-Z]*[[:space:]]+(/|~|\$HOME)([[:space:]]|/?$)'; then
  block "recursive delete of / or home directory."
fi

# Publishing / deployment requires explicit approval via /publish
publish_re='(npm|pnpm|bun)[[:space:]]+publish|yarn([[:space:]]+npm)?[[:space:]]+publish|twine[[:space:]]+upload|(uv|poetry|flit|hatch)[[:space:]]+publish|cargo[[:space:]]+publish|gem[[:space:]]+push|docker[[:space:]]+(image[[:space:]]+)?push|gh[[:space:]]+release[[:space:]]+create|vercel.*--prod|netlify[[:space:]]+deploy.*--prod|fly(ctl)?[[:space:]]+deploy|wrangler[[:space:]]+(deploy|publish)|firebase[[:space:]]+deploy|nuget[[:space:]]+push|mvn[[:space:]].*deploy|gradlew?[[:space:]].*publish|git[[:space:]]+push.*(--tags|[[:space:]]v[0-9]+\.[0-9]+)'
if printf '%s' "$cmd" | grep -Eq "$publish_re"; then
  if [ ! -f "${CLAUDE_PROJECT_DIR:-.}/.claude/.publish-approved" ]; then
    block "publishing/deploying requires explicit user approval. Run the /publish skill, which asks the user and then creates .claude/.publish-approved."
  fi
fi

exit 0
