---
name: build-next
description: Implement the next planned task(s) with the developer agent, verify with test-engineer, review with code-reviewer, fix findings, and commit. Supports a single task ID, the next task, or a whole milestone.
argument-hint: "[T-n | milestone | M<n>]"
---

# Build next

Scope: `$ARGUMENTS` (empty = the next ready task. `milestone` = all remaining tasks in the current milestone. `M<n>` = that milestone.)

## Select
1. Read `docs/project/plan.md` and `STATUS.md`. A task is **ready** when it is `[ ]` and every dependency is `[x]`.
2. Build the work queue for the scope. Group the ready tasks marked `parallel-ok` whose file lists do not overlap. These can run at the same time.
3. Set Phase 5 to `in progress` and update the current milestone/task in STATUS.md.

## Per task (or parallel group)
1. **Implement**: launch the task's **Owner** agent: `developer`, or the named specialist in **task** mode. Launch one per task, and put a parallel group in a single message. Prompt: the task ID, the paths to plan/spec/architecture/environment (plus the owner's own document for specialist tasks, for example design.md, game-design.md, data-ml.md, operations.md or performance.md), any conversation decisions not in files, and "commit when done, do not push".
2. **Check**: read each report. If an agent stopped on ambiguity, or needs a paid service or credentials, ask the user (AskUserQuestion), record the answer in spec.md or plan.md, and relaunch.
3. **Verify and review**: in one message, launch `test-engineer` (scope: the task IDs) and `code-reviewer` (scope: the commits for these tasks).
4. **Fix loop**: if the tests FAIL, or the review has blocker/major findings, launch the Owner agent again with the exact findings to fix (the same task ID). Re-verify. After 3 failed rounds on the same task, mark it `[!]`, record the blocker in STATUS.md, and ask the user how to proceed.
5. **Confirm yourself**: run the fast test suite. The task is done only when it is green and `[x]` in plan.md.

## End of milestone
When every task in the milestone is `[x]`:
1. Launch in parallel: `test-engineer` (scope: the milestone, with integration/e2e), `security-auditor` (scope: the milestone diff), `docs-writer` (update the docs for the new behaviour), and every specialist in STATUS.md → Specialists in **review** mode (scope: the milestone). The `sre` does its readiness review before the first production deploy and a lighter check otherwise.
2. Turn blocking findings into new tasks in the same milestone and build them.
3. Demo for the user: what now works, how to try it (exact commands or a URL), screenshots from the visual review, the test counts and coverage, known issues, and performance budgets (pass/fail), the staging URL if there is one, and what the user needs to judge themselves: sounds, visuals, and game feel (the agents cannot hear, and their visual and gameplay judgement needs confirmation). Point out any placeholder assets that are still in use.
4. Update STATUS.md (milestone done, log entry). In `gated` mode, ask for approval before the next milestone.

Optional: if the user wants PRs, push the branch and open one PR per milestone when they ask (check for `.github/pull_request_template.md`).
