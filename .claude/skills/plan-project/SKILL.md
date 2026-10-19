---
name: plan-project
description: Create or revise the implementation plan (milestones, tasks with acceptance criteria, dependencies, traceability) using the planner agent, then get user approval.
argument-hint: "['revise' + what changed]"
---

# Plan project

Mode: `$ARGUMENTS` (empty = create the plan)

1. Check the prerequisites: spec and architecture are approved. For a revision, read the current `plan.md` and the change requested.
2. Set Phase 4 to `in progress` in STATUS.md.
3. Launch the `planner` agent with the paths to spec.md, architecture.md, decisions/, environment.md, `.claude/templates/plan.md`, and, for a revision, a description of what changed and which tasks are already done (never re-plan finished tasks).
4. Check the result yourself: every FR/NFR appears in the traceability table, M1 is a walking skeleton, no task is bigger than M, and every task has acceptance criteria and tests.
5. Present: the milestone table (goal and task count), the critical path, the spikes, and the planner's questions (ask them with AskUserQuestion).
6. **Gate**: approve / request changes. On approval, set the plan Status to approved, STATUS.md Phase 4 to done, Current milestone to M1, and commit `docs: approve implementation plan`.
