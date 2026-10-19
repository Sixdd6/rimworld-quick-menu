---
name: review-changes
description: Run code review and security audit in parallel on a diff, task, milestone, or the whole codebase, then consolidate findings and optionally fix them.
argument-hint: "[T-n | M<n> | release | <git range>] [--fix]"
---

# Review changes

Scope: `$ARGUMENTS` (empty = `main...HEAD`. `release` = the whole codebase.)

1. Work out the commit range or files for the scope. For task/milestone scopes, find the commits by `[T-n]` in `git log`.
2. In **one message**, launch `code-reviewer` and `security-auditor` with the same scope and the paths to spec/architecture/plan. Also launch, in **review** mode, each specialist from STATUS.md → Specialists whose area the scope touches: UI/graphics → `visual-designer`; audio → `audio-designer`; game rules or tuning → `game-designer`; schema, queries, pipelines or ML/LLM code → `data-engineer`; mobile code → `mobile-developer`; infrastructure, deploy or observability → `sre`; hot paths or anything with a performance budget → `performance-engineer`. For `release`, launch all of them.
3. Combine their findings into one table ordered by severity. Remove duplicates. Check each blocker/critical finding by reading the cited code yourself before you report it.
4. With `--fix`, or when running inside `/kickoff` or `/build-next`: turn blocker/major/critical/high findings into fix tasks, implement them with `developer`, and re-run this review on the fix commits. Otherwise, present the findings and ask which ones to fix.
5. Record the summary in the STATUS.md log.
