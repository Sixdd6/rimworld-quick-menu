---
name: test-suite
description: Run and strengthen the project's tests through the test-engineer agent for a task, milestone, full release scope, or a reported bug; reports acceptance-criteria coverage and defects.
argument-hint: "[T-n | M<n> | full | bug: <description>]"
---

# Test suite

Scope: `$ARGUMENTS` (empty = `full`)

1. Launch `test-engineer` with the scope, the paths to spec/plan/architecture/environment, and one instruction for bug scopes: "write a failing reproduction test, then report. Do not fix."
2. For a bug: after the reproduction exists, create a fix task in plan.md and run `.claude/skills/build-next/SKILL.md` for it.
3. Run the main test command yourself to confirm the reported numbers.
4. Report to the user: the verdict, the pass/fail/skip counts, coverage against the target, the acceptance criteria that are not verified, and the defects by severity.
5. Record the result in the STATUS.md log. Record defects as tasks or blockers.
