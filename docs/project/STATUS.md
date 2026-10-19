# Project Status

- **Project**: rimworld-quick-menu
- **Autonomy**: gated   <!-- gated | autonomous -->
- **Specialists**: visual-designer (all-UI product), performance-engineer (NFR-1 per-frame budget)
- **Current phase**: 5 Build
- **Current milestone / task**: M1 — all 15 tasks done; milestone review
- **Last updated**: 2026-10-19

## Phases

| # | Phase | State | Artifact | Approved |
|---|---|---|---|---|
| 0 | Intake | done | idea.md | — |
| 1 | Design | done | spec.md | ☑ 2026-10-18 |
| 2 | Architecture + Direction | done | architecture.md, decisions/, specialist docs | ☑ 2026-10-19 |
| 3 | Environment | done | environment.md, .mcp.json, CI | ☐ |
| 4 | Plan | done | plan.md | ☑ 2026-10-19 |
| 5 | Build | in progress | source, tests | — |
| 6 | Verify | not started | test/review/security reports | — |
| 7 | Publish | not started | release | ☐ |

States: `not started` → `in progress` → `awaiting approval` → `done` (or `blocked`).

## Open questions
<!-- Q-n: question — who must answer — raised in phase -->
- ~~Q-1~~: resolved by ADR-0004 (typed pin keys with fallbacks)
- ~~Q-2~~: resolved by ADR-0003 (no; custom capture dialog + polling)
- ~~U-1..U-5, Q-D1..Q-D6~~: decided 2026-10-19 (see architecture.md §11, design.md decisions)

## Blockers
<!-- What is blocked, why, what is needed to unblock -->

## Log
<!-- YYYY-MM-DD — short entry per significant event (phase approved, milestone done, release) -->
- 2026-10-18 — Project created from claude-agents-template; intake started.
- 2026-10-18 — Spec approved (16 Must, 2 Should).
- 2026-10-19 — Architecture drafted (8 ADRs, proposed): RimWorld 1.7 is Waylith/.NET 10, Harmony 3. Design + performance direction drafted; reconciling.
- 2026-10-19 — Architecture, design and performance direction approved; ADRs 0001–0008 accepted. Name: Quick Menu (sixdd.QuickMenu).
- 2026-10-19 — Environment set up: .NET SDK 10.0.402 in ~/.dotnet (no sudo), solution scaffolded, CI workflow written (not yet run on GitHub). Build, format and tests pass locally.
- 2026-10-19 — Plan approved: 6 milestones, 80 tasks, 6 user in-game sessions (UC-1..UC-6). Release version 0.9.0.
