---
name: kickoff
description: Start a new project from an idea and drive it through every phase (design, architecture, environment, plan, build, verify, publish) using the agent team, stopping at approval gates. Also resumes an in-progress project from docs/project/STATUS.md.
argument-hint: '"<describe your idea>" [--autonomous]'
---

# Kickoff: idea → published project

You are the **orchestrator**. You run in the main conversation, so you are the only one who can ask the user questions and launch subagents. Delegate the specialist work to the agents in `.claude/agents/`. Keep this conversation focused on decisions, gates, and progress.

Input: `$ARGUMENTS`

## Step 0: Intake or resume
1. If `docs/project/STATUS.md` exists, **resume**: read it, give a 3-line summary of where the project stands, and continue from the current phase. Skip to that phase below.
2. Otherwise:
   - If `$ARGUMENTS` is empty, ask the user to describe the idea in a few sentences: what it is, who it is for, and what "done" looks like.
   - Create `docs/project/` and `docs/project/decisions/`.
   - Write `docs/project/idea.md` with the idea verbatim, plus the date.
   - Copy `.claude/templates/status.md` to `docs/project/STATUS.md` and fill in the project name (propose a short kebab-case name) and the date. Set Autonomy to `autonomous` if `--autonomous` was passed. Otherwise use `gated`.
   - Make sure you are on a feature branch, not `main`.
   - Show the user the phase roadmap in one short table and say which gates will need their approval.

## Phase 1: Design
Follow `.claude/skills/design-session/SKILL.md` completely. **Gate**: the user approves `spec.md`.

## Phase 2: Architecture
1. **Choose the specialists** from the spec, using the roster in `CLAUDE.md` → "When each specialist joins". Show the user the list with a one-line reason for each, and record it in STATUS.md → Specialists. The user can add or remove any of them.
2. Copy the templates each chosen specialist needs into `docs/project/`: `design.md` + `assets.md` (visual/audio designers), `game-design.md` (game-designer), `data-ml.md` (data-engineer), `operations.md` (sre), `performance.md` (performance-engineer).
3. In **one message**, launch `architect` (with the paths to the spec and idea, and any stack preferences from the design session) and every chosen specialist in **direction** mode (with the paths to spec, idea, and its own document). Tell each specialist that the architect is choosing the stack at the same time, and that it should state its technical needs and constraints in its output (for example "needs a WebGL renderer", "needs fixed-timestep physics", "iOS builds need macOS CI", "p95 < 200 ms rules out a cold-start serverless function"), not assume a stack.
4. **Reconcile**: pass all the specialists' needs to the architect, and the chosen stack to the specialists. Continue each agent so they resolve conflicts (for example, a font license, an unsupported audio format, a hosting cost over budget, or an engine that cannot meet the frame budget).
5. Present to the user: the stack table, the key decisions with rationale, each specialist's direction in a few lines (link mockups, the game-design core loop, the hosting cost estimate, the performance budgets), all ongoing costs, the risks, and all the agents' questions. Ask the questions with AskUserQuestion.
6. If the user changes something, send it back to the right agent (continue the same agent if possible) and repeat.
7. **Gate**: the user approves the architecture and every direction document. Mark the ADRs `accepted`, set each document's Status to approved, and update STATUS.md.

## Phase 3: Environment
Follow `.claude/skills/env-setup/SKILL.md`. **Gate**: only for MCP servers, secrets, or paid services the agent proposes.

## Phase 4: Plan
Follow `.claude/skills/plan-project/SKILL.md`. **Gate**: the user approves `plan.md`.

## Phase 5: Build
Repeat `.claude/skills/build-next/SKILL.md` in milestone mode until every milestone is done.
- `gated`: after each milestone, demo what now works (commands and output, screenshots, and the sounds or visuals the user should check), and ask for approval to continue.
- `autonomous`: continue automatically. Stop only for blockers, questions that change product behaviour, or anything that costs money or needs credentials.

## Phase 6: Verify
1. Run `.claude/skills/test-suite/SKILL.md` with scope `full`.
2. Run `.claude/skills/review-changes/SKILL.md` with scope `release` (code review + security audit on the full codebase).
3. In **one message**, launch `docs-writer` for the complete docs pass and every specialist in STATUS.md → Specialists in **review** mode (full product scope). For the `sre`, this is the **readiness review**. Make sure `assets.md` is complete, with the attribution text shown where the licenses require it, and that every budget in `performance.md` passes.
4. Fix loop: turn each blocking finding into a fix task in `plan.md` (next free T-number, under a "Hardening" milestone). Implement it with the right owner agent, then re-verify. Repeat until tests pass, there are no blocker/major review findings, no critical/high security findings, no failed performance budgets, and no failed readiness checks.

## Phase 7: Publish
Follow `.claude/skills/publish/SKILL.md`. This gate is **always** explicit, in every autonomy mode.

## Orchestration rules
- **Self-contained delegation**: subagents do not see this conversation. Every prompt must include the file paths to read, the exact scope (task IDs), the decisions made in the conversation that are not yet in files, and the expected output.
- **Parallelism**: launch independent agents in one message, for example `code-reviewer` + `security-auditor`, or `developer` agents on `parallel-ok` tasks with disjoint files.
- **Do not trust, verify**: after an agent reports success, spot-check it. Run the test command yourself, and read the key files it changed.
- **State**: update `docs/project/STATUS.md` at every phase change, gate decision, milestone completion, blocker, and open question. Commit artifacts at each gate (`docs: approve spec`, and so on).
- **Questions**: use AskUserQuestion with at most 4 focused questions per round, the recommended option first. Never ask what you can find out from the files or the code.
- **Scope changes** after approval: update spec.md (and mark the change), then ask the architect or planner to assess the impact, and get approval before you build it.
- **Honesty**: report failures, skipped steps, and open risks plainly at every gate.
