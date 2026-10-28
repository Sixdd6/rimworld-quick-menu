# Project Status

- **Project**: rimworld-quick-menu
- **Autonomy**: gated   <!-- gated | autonomous -->
- **Specialists**: visual-designer (all-UI product), performance-engineer (NFR-1 per-frame budget)
- **Current phase**: 7 Publish (manual Workshop upload by the user)
- **Current milestone / task**: 1.0.0 published on the Steam Workshop (item 3824091158). Post-release full-mod-list debugging (859 mods) on branch `fix/order-pin-copies`, not merged or pushed yet: BAM copies of order designators and def dropdowns (pin, resolve, marker), marker on mod `GizmoOnGUIInt` overrides (ADR 0009 amendment), More Planning icons and unpinnable colour squares, live slot labels, catalog rebuild when RIMMSqol re-resolves categories (ADR 0006 amendment, no cap), self-test written to `QuickMenu-selftest.txt`. Verified in game by the user. Known and left as is (user, 2026-10-27): More Planning's Planning Visibility has no icon after a RIMMSqol apply until toggled (More Planning only sets it on load or toggle); Open The Windows + ReBuild: Doors and Corners error spam is a third-party conflict.
- **Last updated**: 2026-10-27

## Phases

| # | Phase | State | Artifact | Approved |
|---|---|---|---|---|
| 0 | Intake | done | idea.md | — |
| 1 | Design | done | spec.md | ☑ 2026-10-18 |
| 2 | Architecture + Direction | done | architecture.md, decisions/, specialist docs | ☑ 2026-10-19 |
| 3 | Environment | done | environment.md, .mcp.json, CI | ☐ |
| 4 | Plan | done | plan.md | ☑ 2026-10-19 |
| 5 | Build | done | source, tests | — |
| 6 | Verify | done (fast-tracked) | security audit PASS 2026-10-21; UC-6 Parts 1–9 | — |
| 7 | Publish | in progress (user, Workshop) | dist/, docs/release/ | ☑ 2026-10-21 (user: release files for Workshop; no GitHub release) |

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
- 2026-10-19 — M1 done: walking skeleton + spikes S-1..S-7 all passed in UC-1 (vanilla + BAM); perf baseline within 50% budgets (P-9 first-frame re-based to 128 KB per open by user decision); security audit PASS (fixes applied); 322 tests, Core 99.4%.
- 2026-10-19 — M1 approved by user; M2 started.
- 2026-10-19 — M2 done: pin by click (vanilla + BAM, dropdowns and entries), styled wheel, binding + settings page, persistence; UC-2 + UC-2b passed. User decisions: picker removed (FR-7 Won't), centre = Edit button, Ctrl+click unpins on the wheel, unavailable pins always shown greyed, no hover delay, keep vanilla tooltip.
- 2026-10-19 — M2 approved by user (test review PASS, security audit PASS with lows fixed); M3 started.
- 2026-10-19 — M3 approved: sub-rings, material ring (fixed order, highlight), material-variant icons, Ideology refresh, edit mode shell; UC-3 passed; security + test reviews passed. T-86 (satellite underlay) declined by user.
- 2026-10-19 — M4 approved: drag rearrange, grid, Auto sizing, settings v2 + preview, empty hint; UC-4 passed; reviews passed. User decisions: hold mode dropped; FR-19 pin marker added for M6; UI scale check at 1.75.
- 2026-10-19 — M5 approved: availability reasons, missing-mod pins, Mint/BAM detection + notices, Multiplayer audit, settings polish, hold mode removed, slider rebuild fix; pinning only in architect menus (vanilla, BAM, Mint). UC-5 passed; reviews passed.
