# CLAUDE.md

This repository is built by an **agent team**. An idea goes in, and a tested, documented, published project comes out. The team works in phases. Each phase leaves durable artifacts in `docs/project/`, so any session can pick up where the last one stopped.

## Start here in every session

0. If `git remote get-url origin` points to `Sixdd6/claude-agents-template`, this is the template itself, not a project: read `.claude/template-maintenance.md` and follow it instead of the steps below.
1. Read `docs/project/STATUS.md`. If it does not exist, the project has not started: run `/kickoff <idea>`.
2. Continue from the current phase. Do not redo approved phases unless the user asks.
3. Keep `STATUS.md` up to date whenever a phase, milestone, or task changes state.

## The team (`.claude/agents/`)

| Agent | Role | Writes |
|---|---|---|
| `requirements-analyst` | Finds gaps, ambiguities and contradictions in the idea/spec and proposes the next interview questions | recommendations only |
| `architect` | Picks the stack, designs the system, records decisions | `architecture.md`, `decisions/` |
| `environment-engineer` | Toolchain, dependencies, MCP servers, dev container, CI, setup scripts | `environment.md`, `.mcp.json`, `scripts/`, CI config |
| `visual-designer` | UX flows, design system, accessibility, icons/vector art, graphics code, image assets, screenshot-based visual review | `design.md`, UI/graphics code, image assets |
| `audio-designer` | Audio direction, sound list, synthesized audio, sourced/generated audio, processing pipeline, measured audio QA | `design.md` (audio), audio code and assets |
| `game-designer` | Core loop, mechanics, controls and feel, progression and balance, levels, balance simulations, playtest bots | `game-design.md`, tuning data, simulations |
| `data-engineer` | Data model, migrations, pipelines, search/analytics, ML models, LLM features and evals | `data-ml.md`, schema/migrations, pipelines, eval harness |
| `mobile-developer` | iOS/Android/cross-platform apps, platform conventions, device testing, store readiness | mobile code and tests |
| `sre` | Hosting, infrastructure as code, deploys, observability, SLOs, backups, runbooks, cost | `operations.md`, infra code, deploy workflows |
| `performance-engineer` | Performance budgets, benchmarks, load tests, profiling, optimisation, CI performance gates | `performance.md`, benchmarks, `perf:` fixes |
| `planner` | Breaks the spec into milestones and small, testable tasks, each with an owner agent | `plan.md` |
| `developer` | Implements one task at a time, test first | source + tests |
| `test-engineer` | Test strategy, integration/e2e tests, coverage, bug reproduction | tests, test reports |
| `code-reviewer` | Reviews diffs for correctness, design and maintainability (read-only) | review findings |
| `security-auditor` | Threat model, dependency and secret audit, OWASP review (read-only) | audit findings |
| `docs-writer` | README, user guide, API docs, CHANGELOG | docs |
| `release-engineer` | Versioning, packaging, release pipeline, publishing | release config, tags |

### When each specialist joins

The core team (analyst, architect, environment-engineer, planner, developer, test-engineer, code-reviewer, security-auditor, docs-writer, release-engineer) works on every project. Specialists join when the spec calls for them. The choice is recorded in `STATUS.md` → Specialists, and the user can change it.

| Specialist | Joins when the spec has… |
|---|---|
| `visual-designer` | any user interface, graphics, or visual assets (almost every product except pure libraries/back-end services) |
| `audio-designer` | any sound: effects, music, voice, audio feedback |
| `game-designer` | a game, or game mechanics (points, levels, progression) as a core feature |
| `data-engineer` | persistent data beyond simple config, data pipelines, search, analytics, or any ML/LLM feature |
| `mobile-developer` | an iOS or Android target (native or cross-platform) |
| `sre` | anything that runs as a hosted service (web app, API, bot, worker), or deploys to cloud infrastructure |
| `performance-engineer` | measurable performance NFRs, real-time graphics (games, animation), high load, or strict size/startup limits |

A joining specialist works in three modes: **direction** (Phase 2, with the architect), **task** (owns plan tasks in Phase 5), and **review** (at the end of each milestone and in Phase 6).

Subagents cannot talk to the user or spawn other subagents. The **main conversation is the orchestrator**. It runs the workflow skills below, asks the user questions, and delegates to agents with complete, self-contained prompts. A subagent does not see this conversation: give it file paths, the task ID, and the acceptance criteria.

## Workflow (`.claude/skills/`)

| Phase | Skill | Gate (needs user approval) |
|---|---|---|
| 0 Intake | `/kickoff <idea>` runs every phase in order | — |
| 1 Design | `/design-session` | spec approved |
| 2 Architecture + specialist direction | (part of `/kickoff`: `architect` + the chosen specialists in parallel) | architecture and direction documents approved |
| 3 Environment | `/env-setup` | new MCP servers / secrets / paid services |
| 4 Plan | `/plan-project` | plan approved |
| 5 Build | `/build-next [task-id or "milestone"]` | — (autonomous mode) or each milestone (gated mode) |
| 6 Verify | `/test-suite`, `/review-changes` | — |
| 7 Publish | `/publish` | **always** explicit confirmation |

`/project-status` summarizes where things stand at any time.

The autonomy level is stored in `STATUS.md`:
- `gated` (default): stop for approval at each gate and at the end of each milestone.
- `autonomous`: stop only at the Design, Architecture and Publish gates, and for any action that costs money, needs a credential, or is hard to reverse.

## Project artifacts (`docs/project/`)

Templates are in `.claude/templates/`. Copy a template, then fill it in. Never leave template placeholder text in a finished artifact.

- `STATUS.md`: phase tracker, autonomy level, specialists on the team, current task, open questions, blockers
- `idea.md`: the idea exactly as the user gave it
- `spec.md`: product spec with numbered requirements (`FR-n`, `NFR-n`) and acceptance criteria
- `architecture.md` + `decisions/NNNN-title.md`: design and ADRs
- `design.md`: UX, design system, art and audio direction, asset strategy (projects with UI, graphics or sound)
- `assets.md`: every non-code asset with its source, license and attribution
- `game-design.md`: pillars, core loop, mechanics, tuning, balance evidence (games)
- `data-ml.md`: data model, privacy classification, pipelines, ML/LLM design, evals, cost
- `operations.md`: hosting, environments, deploys, SLOs and alerts, backups, runbooks
- `performance.md`: performance budgets, workload model, results log
- `environment.md`: toolchain, commands, MCP servers, secrets needed (names only)
- `plan.md`: milestones and tasks (`T-n`) with owner, acceptance criteria and traceability to requirements

## Engineering rules

- **Traceability**: each task maps to requirements, and each requirement has at least one test. Commit messages reference task IDs.
- **Test first**: write a failing test, then make it pass. A task is not done until its tests and the full fast suite pass.
- **Small steps**: one task is one focused commit (or a few). Do not mix refactors with features.
- **Conventional Commits**: `feat(scope): … [T-12]`, `fix:`, `test:`, `docs:`, `chore:`, `ci:`, `refactor:`.
- **Branches**: never commit directly to `main`. Work on a feature branch and open a PR when the user asks for one.
- **Secrets**: never commit secrets. Read them from environment variables. Document the names in `environment.md` and `.env.example`.
- **Assets**: every image, font, sound, video or model is listed in `assets.md` with its source and license before it is committed. Never use assets of unknown origin. Placeholders are labelled as placeholders.
- **Media limits**: agents cannot hear, and their visual judgement needs confirmation. Measure what can be measured, and ask the user to check anything subjective.
- **Dependencies**: prefer well-maintained, widely used packages. Pin versions with a lockfile. Record why each notable dependency was added.
- **Do not guess**: if a requirement is ambiguous, record the question in `STATUS.md` → Open questions and ask the user. Do not invent product behavior.
- **Honest reporting**: report failing tests, skipped steps and known issues plainly.

## Project-specific notes

<!-- The architect and environment-engineer fill this section in during phases 2–3:
     language/framework, how to build/run/test/lint, directory layout, conventions. -->

### Stack (see docs/project/architecture.md and decisions/)
- **Mod**: "Quick Menu", packageId `sixdd.QuickMenu`, MIT.
- **Game**: RimWorld **1.7** (build 1.7.4982) runs on Ludeon's **Waylith** engine on a patched **CoreCLR .NET 10**. It is **not Unity/Mono and not net472**. Unity APIs come from `UnityStubs.dll` (`using UnityEngine;` still works).
- **Language / target**: C# (default for net10.0), SDK-style projects, `net10.0`, .NET SDK 10.0.1xx (pinned in `global.json`).
- **Game references**: `Krafs.Rimworld.Ref` `1.7.4982-unstable` (compile-only). Fallback `-p:UseLocalGameRefs=true` uses `$(RIMWORLD_DIR)/{Assembly-CSharp,UnityStubs,Waylith}.dll`.
- **Patching**: Harmony 3, `Lib.Harmony` `3.0.0-rc.3` compile-only (`ExcludeAssets=runtime`). At runtime it comes from the `brrainz.harmony` mod. Never ship `0Harmony.dll`.
- **Tests**: xUnit v3 (`xunit.v3` 4.0.x) on `src/QuickMenu.Core` only. Coverage gate is at least 90 % lines.

### Layout
- `mod/`: exactly the shipped mod (About, LoadFolders.xml, `1.7/Assemblies` = build output, Languages, Textures). Symlinked into the game's `Mods/` for development.
- `src/QuickMenu.Core/`: pure logic (layout, edge dodge, sizing, PinKey/PinBoard/serializer, trigger state machine). **Must not reference any game or Unity assembly.**
- `src/QuickMenu/`: thin game adapter (Mod + settings, Harmony patches, catalog, activation, IMGUI windows, diagnostics).
- `tests/QuickMenu.Core.Tests/`: unit tests and Perf-trait gates. `tests/QuickMenu.Core.Benchmarks/`: BenchmarkDotNet. `tests/Shared/SyntheticCatalog.cs`: shared seeded test data, linked into both projects.
- `scripts/`: setup, test, perf, link-mod, package (the ADR/architecture name `link-mod.sh` is `link-mod.sh` in practice).
- `reference/`: Dubs Mint Menus, gitignored. **Clean-room: study behaviour only. Never copy its code, textures or strings.**

### Commands (verified; see docs/project/environment.md)
| Purpose | Command |
|---|---|
| Setup (idempotent; installs .NET 10 SDK to `~/.dotnet` if missing, restores) | `scripts/setup.sh` |
| Build (outputs to `mod/1.7/Assemblies/`) | `dotnet build QuickMenu.slnx -c Release` |
| Format check / fix | `dotnet format QuickMenu.slnx --verify-no-changes` / `dotnet format QuickMenu.slnx` |
| Functional tests + coverage gate (Core >= 90 %) | `scripts/test.sh` |
| Perf gates (Release, no coverage) | `scripts/perf.sh` |
| Benchmarks (manual) | `scripts/perf.sh --bench` |
| Dev install (once per machine, symlink) | `scripts/link-mod.sh` |
| Package | `scripts/package.sh` -> `dist/QuickMenu-<version>.zip` |
| Run in game | launch RimWorld with `-mod=brrainz.harmony,sixdd.QuickMenu -quicktest`; in dev mode run the debug action "Quick Menu: self-test" |
- Perf tests carry `[Trait("Category", "Perf")]`; xUnit v3 filter is `--filter-trait "Category=Perf"` / `--filter-not-trait`.
- If `dotnet` is not on PATH, `. scripts/_env.sh` adds `~/.dotnet`.
- Local game DLLs instead of Krafs: `dotnet build -p:UseLocalGameRefs=true` (rewrites `packages.lock.json`; run `git checkout -- '**/packages.lock.json'` afterwards). CI uses `restore --locked-mode`.
- Test names may use underscores (CA1707 is off under `tests/`). Warnings are errors.

### Conventions
- Namespaces `QuickMenu.Core.*` and `QuickMenu.*`. Keyed string keys are prefixed `QuickMenu.`, and every player-facing string is Keyed.
- Harmony patches exist only at the sites listed in architecture.md §6 (three, including the `Designator_Dropdown.GetDesignatorSelectAction` postfix). No transpilers. Every patch body has try/catch → `LogOnce` → disable that patch and run the original.
- Patch parameters use exact types (`Designator __instance`, `Event __0`), because Harmony 3 type-checks injections. Bind by index (`__0`), not by name: overrides name the parameter differently (`ev`, `e`), and a named parameter that does not match fails to bind.
- In the `Designator.ProcessInput` prefix the `Event` is already `used` (UC-1 S-3): read click-vs-hotkey and modifiers from `UnityEngine.Input` (mouse button state, `GetKey`), not from the `Event`. `ev.control`/`ev.alt` are reliable only for direct clicks.
- No GameComponent, WorldComponent or MapComponent, and nothing written into save files. All state lives in `QuickMenuSettings : ModSettings`.
- No per-frame allocations in the trigger poller or in OnGUI drawing: no LINQ, no string building, no closures in hot paths. Cache labels and layout per open.
- IMGUI only (no native Waylith UI, no custom shaders, no rotated GUI drawing). Use designator icons through `Command.DrawIcon`.
- Logic that can be pure goes in Core, test-first. Adapter code stays thin.
