# Performance: rimworld-quick-menu

- **Status**: approved (2026-10-19). Targets approved; first in-game baseline (UC-1, setups a and b) logged in section 5.

Source: spec NFR-1 (performance) and NFR-8 (testability). All numeric targets are **proposed**; they are my reading of NFR-1 and need user confirmation (section 7).

**Honesty note.** The game is RimWorld 1.7.4982 on the Waylith engine and a patched CoreCLR (.NET 10). There is no Unity or Mono. In-game numbers depend on the player's CPU, the other mods installed, UI scale and the game's own load. Numbers from the dev machine (Fedora Linux) show this mod's relative cost and allocation behaviour, not a promise for every player. Out-of-game benchmarks run on a normal .NET 10 runtime, not the game's patched one, so they give trends and allocation counts, not in-game milliseconds. The repo is not scaffolded yet, so every command below is the intended command and has not been run.

## 1. Budgets

Reference frame: 60 fps = 16.7 ms. "Median" and "p99" are over at least 600 consecutive frames after a 120-frame warm-up. "Closed" means the menu window is not open.

Architecture facts that shape the budgets (from architecture.md section 3, 6, 7 and ADR 0003, 0005, 0006):
- Closed state is **one Harmony postfix per frame** (`UIRoot_Play.UIRootUpdate`) doing an early return when unbound or disabled, then **one `Input` call** on the bound key or button.
- Pin-by-click prefixes on every `Designator.ProcessInput` override run **only when a gizmo is clicked or a hotkey activates a designator**, not on every gizmo draw. My first draft budgeted a per-gizmo-per-frame hook. That risk is gone (see R-1), so P-3/P-4 are replaced by click-path budgets and an "idle invocation count must be 0" check.
- Layout is cached until settings or screen size change. The catalog is built once at startup (ADR 0006; there is no search index since the picker was dropped on 2026-10-19). Availability is evaluated on open and on hover.
- **Picker removed (user decision 2026-10-19, spec FR-7 Won't).** The search and picker budgets P-11a, P-11b, P-13, P-14 and P-15, scenario S-6, workload W-1x, the synthetic search-entry generator, risk R-8 and the matching CI gates are dropped. **P-11c and P-12 stay** for the catalog build.

| ID | Metric | Target | From NFR | Method / tool | Environment | Latest | Status |
|---|---|---|---|---|---|---|---|
| P-1 | Closed: total mod cost per frame (trigger poller postfix; nothing else runs per frame), median | <= 10 us (<= 2 us goal; the work is a null/flag check plus one `Input` call) | NFR-1 | In game: Waylith Frame Profiler, filter `QuickMenu!*`; cross-check with the dev-mode Stopwatch scope around the postfix body (T-6). | In game, dev mode, reference save | not measured | pending |
| P-2 | Closed: managed allocations per frame attributable to the mod | 0 bytes | NFR-1 | Out of game: `GC.GetAllocatedBytesForCurrentThread()` assertion on the pure trigger path in Core. In game: dev-mode scope records `GC.GetAllocatedBytesForCurrentThread()` before and after the postfix body, summed over 10,000 closed frames. | xUnit (CI) plus in game | not measured | pending |
| P-3 | Pin-click prefix, fast path (modifier not held, or not a left click), per `ProcessInput` call | <= 100 ns and 0 bytes | NFR-1 | Out of game: BenchmarkDotNet on the pure decision function (`ModifierPolicy` and the event classification). In game: count and Stopwatch around the prefix (dev mode). | CI plus in game | not measured | pending |
| P-4 | Pin-click prefix invocations while idle (architect menu open, mouse moving over gizmos, no click, no hotkey) | exactly 0 invocations over 600 frames, with vanilla and with BAM (when BAM loads on 1.7) | NFR-1 | In game: dev-mode counter in the prefix, reported by the self-test. This verifies the architecture's assumption that `ProcessInput` is click-only. If non-zero, the per-gizmo budget returns. | In game, spike S-3 | not measured | pending |
| P-5 | Pin-click action (modifier held, left click): toggle, message, mark dirty. Excludes the settings file write | <= 1 ms; allocations bounded (<= 2 KB per click) | NFR-1 | Out of game: benchmark of `PinBoard.Toggle` and `PinKey` build. In game: Stopwatch (dev mode). | CI plus in game | not measured | pending |
| P-6 | Open latency: trigger input to menu window added | Same frame as the input (0 extra frames) | NFR-1 | In game: log `Time.frameCount` (via UnityStubs) in the poller and in the window's first `DoWindowContents`; they must be equal. Spike S-6 verifies this. Also check with the game limited to 30 fps. | In game | not measured | pending |
| P-7a | Warm open: extra time on the opening frame (layout lookup, pin resolution, availability for all pins, window add), 40 pins, 2 rings, catalog already built | <= 3 ms above the closed median (no dropped frame at 60 fps) | NFR-1 | In game: Frame Profiler plus Stopwatch around the open path; p99 frame time over 20 open/close cycles. | In game | not measured | pending |
| P-7b | Cold open: first open of the session, if it triggers the lazy catalog build | <= 200 ms total (the NFR index limit). See conflict C-1: this is one slow frame, which is not "within one frame". | NFR-1 | In game: Stopwatch (self-test report). | In game | not measured | pending |
| P-7c | Ideology designator refresh done on each open (architecture: "refreshed when the menu opens") | <= 0.5 ms | NFR-1 | In game Stopwatch. | In game | not measured | pending |
| P-8 | Open: draw plus input cost per frame, 40 pins, wheel with one sub-ring open, median | <= 1.0 ms (about 6% of a 60 fps frame) | NFR-1 | In game: Frame Profiler on `QuickMenuWindow.DoWindowContents` and `WindowUpdate`. Repeat for grid and for the max size setting. | In game | not measured | pending |
| P-9 | Open: managed allocations per frame, steady state (after the first frame of the open) | 0 bytes in mod-owned code (Core layout, hit test, state machine). Whole-window total: report it, expected small; target <= 512 bytes per frame, to be re-based on the control run (below). First frame after open <= 128 KB per open (re-based 2026-10-19 by user decision after UC-1), recorded, no growth with catalog size; source to be found in M2. | NFR-1 | Out of game: zero-allocation assertions on Core layout/hit-test/state machine. In game: allocated-bytes delta around `DoWindowContents` over 10,000 frames, mouse sweeping all slots. **Control run:** same measurement on an empty full-screen transparent `Window` that makes the same `Widgets`/`GUI` calls, so engine/IMGUI allocations are not blamed on the mod. | xUnit (CI) plus in game | not measured | pending |
| P-10 | Layout computation (wheel/grid placement, edge dodge) for 40 slots plus one sub-ring | <= 50 us; runs on open or on change only, never per frame | NFR-1, NFR-8 | Out of game: BenchmarkDotNet on `WheelLayout`, `GridLayout`, `SubMenuLayout`, `EdgeDodger`. | CI | not measured | pending |
| P-11a | ~~**Core search index build**, 20,000 synthetic entries (the architect's chosen stress size, 10x W-1)~~ **Dropped** (user decision 2026-10-19: no picker, no search index, FR-7 Won't) | <= 200 ms hard (goal <= 100 ms) | NFR-1 | xUnit timing test in `QuickMenu.Core.Tests` (Release, category `Perf`, median of 5) plus BenchmarkDotNet for the real number. | CI | not measured | dropped |
| P-11b | ~~Core search index build, W-1 (2,000 entries)~~ **Dropped** (user decision 2026-10-19: no picker, no search index, FR-7 Won't) | <= 20 ms | NFR-1 | Same. | CI | not measured | dropped |
| P-11c | **Whole in-game catalog build** (enumerate all `DesignationCategoryDef`s, build `PinKey`s, labels, mod names and the key map; no search index since 2026-10-19) on the stress mod and on the user's real list | <= 200 ms hard (the NFR), goal <= 100 ms | NFR-1 | In game: Stopwatch inside the catalog build, reported by the self-test (architecture already lists "index build time" in the self-test). Record the real designator and mod counts with the result. | In game | not measured | pending |
| P-12 | Catalog builds per session without a def reload | 1 | NFR-1 | Build counter in the self-test; unit test that a second request reuses the cache. | xUnit plus in game | not measured | pending |
| P-13 | ~~Retained memory of catalog plus index, W-1~~ **Dropped** (user decision 2026-10-19: no picker, no search index, FR-7 Won't) | <= 5 MB (about <= 2 KB per entry) | NFR-1 | Out of game: `GC.GetTotalMemory(true)` before and after build for the Core index. In game: same around the catalog build (dev mode). | CI plus in game | not measured | dropped |
| P-14 | ~~Search per keystroke, W-1, median~~ **Dropped** (user decision 2026-10-19: no picker, no search index, FR-7 Won't) | <= 5 ms; no per-entry allocation | NFR-1 | Out of game: BenchmarkDotNet with 20 fixed realistic queries and `GC.GetAllocatedBytesForCurrentThread` assertion (the result list is reused). | CI | not measured | dropped |
| P-15 | ~~Picker window draw per frame, 2,000 entries, scrolled list~~ **Dropped** (user decision 2026-10-19: no picker, no search index, FR-7 Won't) | <= 2 ms median; only visible rows drawn | NFR-1 | In game Frame Profiler on `PickerWindow`. Needs a virtualised list. | In game | not measured | dropped |
| P-16 | Pin lines parse (`PinSerializer.FromLines`), 200 pins | <= 5 ms | NFR-1 | Out of game: BenchmarkDotNet. | CI | not measured | pending |
| P-17 | Settings save: `PinSerializer.ToLines` for 200 pins | <= 5 ms; plus `WriteSettings` on the main thread <= 20 ms (in game). Written on change only, never per frame | NFR-1 | CI for the pure part. In game: Stopwatch around `WriteSettings`, and a counter of writes per edit session (see R-7). | CI plus in game | not measured | pending |
| P-18 | Mod startup cost: settings load, Mint/BAM/MP detection, Harmony patching (postfix plus every `ProcessInput` override across all loaded assemblies) | <= 150 ms with the vanilla set, <= 300 ms on the user's large list | NFR-1 | In game: Stopwatch in the `Mod` constructor (self-test also reports the patch count). I raised this from my first draft because patching N overrides by reflection scales with the mod list. | In game | not measured | pending |

Notes:
- P-1 and P-3 are floors of "no measurable cost". If measured values are far below target, tighten them.
- Frame Profiler overhead is unknown to me. Treat its numbers as attribution (who costs what) and use the Stopwatch scopes for absolute values until the profiler's own overhead is known (see question 4).

## 2. Workload model

### Scenarios
| ID | Scenario | Purpose |
|---|---|---|
| S-1 | Closed idle on a colony map, nothing open | P-1, P-2 |
| S-2 | Architect menu open on the largest category, mouse moving over gizmos, no clicks (vanilla; BAM when available) | P-4 |
| S-3 | Ctrl+click pin and unpin 20 times | P-3, P-5 |
| S-4 | Open/close 20 times, mouse near centre and near a screen edge; first open of the session recorded separately | P-6, P-7a/b/c |
| S-5 | Menu held open 10,000 frames, mouse sweeping all slots, sub-ring open | P-8, P-9 |
| S-6 | ~~Open picker, type 20 realistic queries, scroll the whole list, pin 5 items~~ **Dropped** (no picker, 2026-10-19). P-11c is covered by S-8 (game start) | — |
| S-7 | Edit session: drag 20 swaps, close, restart the game | P-16, P-17 |
| S-8 | Game start with the mod | P-18 |

### Data volumes
| ID | Workload | Size | Notes |
|---|---|---|---|
| W-0 | Vanilla plus all DLC | about 400 to 600 designators | Baseline; far below budgets |
| W-1 | Large modded list ("large mod list" in NFR-1) | **500 mods, 2,000 designators** | The budget workload |
| W-1x | ~~Core-only stress (architect's choice)~~ **Dropped** with P-11a (no search index, 2026-10-19) | — | — |
| W-2 | Extreme in game | 1,000 mods, 8,000 designators | Not a budget; run once per milestone to check scaling |
| W-3 | Pins | 40 pinned in the menu, 10 of them dropdowns of 8 children; 200 pin lines in settings for load/save | Realistic heavy user plus stress |

### Building the synthetic workload
1. ~~**Pure data (CI and benchmarks).**~~ **Dropped with the search index (2026-10-19)**; `tests/Shared/SyntheticCatalog.cs` keeps only the other seeded test data. Original plan, historical: a seeded generator `SyntheticCatalog.Generate(seed, mods, designators)` in a source file shared by the test and benchmark projects (linked with `<Compile Include>`; see layout addition L-1). It produces the plain entry type that `SearchIndex.Build(entries)` consumes (label, category, mod name, plus any other fields the architect adds). Rules: labels of 1 to 5 words from a word list of about 300 words with some numbers and non-ASCII letters; mod names with a long tail (a few mods with 200 designators, most with 1 to 5); about 10% dropdown children; about 5% repeated labels across mods; fixed seed. W-1 is `Generate(1, 500, 2000)` and W-1x is `Generate(1, 2000, 20000)`.
2. **In-game stress mod (manual).** A test-only mod generated by a script (`scripts/make-stress-mod.sh` or similar; checked in, output gitignored) containing about 2,000 buildable `ThingDef`s spread over vanilla and a few fake `DesignationCategoryDef`s. It exercises the real def traversal, `PinKey` building, and Harmony patching. It cannot fake 500 separate content packs; the report states the real mod count.
3. **Real list (manual, most credible).** The user's own installed set. Spike S-4 already plans a self-test on the full list (about 980 workshop items installed, though many will not load on 1.7). Record the loaded mod count and designator count with every result.

## 3. Tooling

| Purpose | Tool | Notes |
|---|---|---|
| Micro-benchmarks | BenchmarkDotNet (`MemoryDiagnoser`) on `net10.0` | Project `tests/QuickMenu.Core.Benchmarks` (layout addition L-1) |
| Time gates | xUnit v3 tests with trait `Category=Perf` in `QuickMenu.Core.Tests` | Loose thresholds (section 4) |
| Zero-allocation gates | xUnit v3 tests calling the hot function in a loop after warm-up and asserting `GC.GetAllocatedBytesForCurrentThread()` delta is 0 | Deterministic. Warm up first so JIT/tiering allocations are excluded |
| In-game timing | Waylith Frame Profiler (dev mode), instrumentation filter `QuickMenu!*` and `QuickMenu.Core!*` | Primary attribution tool. Dubs Performance Analyzer is not expected on 1.7 and is not used |
| In-game absolute timing and allocations | Dev-mode-only `MeasureScope` helper (T-6): `Stopwatch.GetTimestamp()` and `GC.GetAllocatedBytesForCurrentThread()` before/after, aggregated into preallocated counters, printed by the self-test | Gives per-component bytes on CoreCLR, which Mono could not. Guarded by one cached `static readonly`/bool flag |
| In-game integration | Debug action "Quick Menu: self-test" (already in the architecture): reports catalog build time, patch count, round-trip results; I ask it to also print the budget counters (P-4, P-12, P-18) | |
| Scaling | Stress mod plus real list; results to the log in section 5 | Manual |

### How to run (verified 2026-10-19 on .NET SDK 10.0.402, xunit.v3 4.0.2, paths follow ADR 0008)
```
# Build everything
dotnet build QuickMenu.slnx -c Release

# Functional tests + coverage gate (Perf-trait tests excluded: coverage instrumentation distorts time and allocation)
scripts/test.sh
#   = dotnet run --project tests/QuickMenu.Core.Tests -c Release -- --filter-not-trait "Category=Perf" --coverage ...

# Performance gates (time + zero-allocation), Release, no coverage
scripts/perf.sh
#   = dotnet run --project tests/QuickMenu.Core.Tests -c Release -- --filter-trait "Category=Perf"

# Full benchmarks (writes BenchmarkDotNet.Artifacts/, gitignored)
scripts/perf.sh --bench
#   = dotnet run --project tests/QuickMenu.Core.Benchmarks -c Release -- --filter '*' --memory

# In-game
scripts/link-mod.sh                      # once per machine: symlink mod/ into $RIMWORLD_DIR/Mods/QuickMenu
dotnet build QuickMenu.slnx -c Release   # DLLs land in mod/1.7/Assemblies
# launch: -mod=brrainz.harmony,sixdd.QuickMenu -quicktest
# in game: enable dev mode, enable the Frame Profiler with filter "QuickMenu!*", run scenarios S-1..S-8,
#          run debug action "Quick Menu: self-test", copy the log block into section 5
```
Confirmed: xUnit v3 on Microsoft Testing Platform accepts `--filter-trait "Name=Value"` and `--filter-not-trait "Name=Value"` (also `--filter-class/-method/-namespace`). `dotnet test` also works because `global.json` selects the Microsoft Testing Platform runner, but `dotnet run` is used so relative `--coverage-settings` paths are not an issue (scripts pass absolute paths). Trait attribute: `[Trait("Category", "Perf")]`. The Frame Profiler's menu path and exact filter syntax are taken from the coordinator's note and are not yet used; the exact steps will be recorded here after first use.

## 4. CI gates

Principle: gate on what is deterministic; keep time gates loose because shared runners are noisy. CI runs only out-of-game checks. Absolute thresholds only (no baseline comparison across different runners).

| Budget | Gate | Threshold | Notes |
|---|---|---|---|
| P-2, P-3, P-9 | Zero-allocation tests (Core hot paths: trigger decision, modifier policy, layout/hit-test/state-machine per-frame calls; the search gate P-14 is dropped) | exactly 0 bytes | Strict, every PR |
| P-5 | Allocation test on `PinBoard.Toggle` | <= 2 KB per call | Strict |
| ~~P-11a, P-11b, P-13~~ | **Dropped** (no search index, 2026-10-19) | — | |
| P-10, P-16, P-17 (pure part) | Perf-trait tests | fail > 5x the budget | Catches accidental O(n^2), not small drift |
| ~~Scaling~~ | ~~W-1x / W-1 ratio of build time~~ **Dropped** with W-1x (2026-10-19) | — | |
| Benchmarks | BenchmarkDotNet suite compiles in CI (not run for gating); a manual workflow can run it | | Keeps it from rotting |

The coverage gate (>= 90% of Core, architecture section 9) and the Perf gates must be **separate CI steps**: coverage instrumentation slows code and adds allocations.

**Manual only** (need the real game): P-1, P-2 (in-game half), P-4, P-6, P-7a/b/c, P-8, P-9 (in-game half), P-11c, P-12, P-17 (in-game half), P-18. Run per milestone and before release (section 5).

## 5. Results log
| Date | Build / commit | Metric | Value | Change vs baseline | Notes |
|---|---|---|---|---|---|
| 2026-10-19 | f721375 (UC-1 skeleton) | P-1 closed poller, per frame | median 0.15 us (self-test), 0.12 us (perf report, 35,844 frames); p99 0.54 / 0.27 us; max 281 / 311 us | baseline | **PASS** (limit 50% of 10 us = 5 us; goal 2 us also met). Setup (a), 8 mods, 454 entries. 10,000-frame sample, single run. Scope covers the postfix body only, not the Harmony wrapper. **Frame Profiler cross-check NOT done** (still open). Max outliers: see note 3. |
| 2026-10-19 | f721375 | P-2 closed allocations | 0 bytes over 10,000 frames | baseline | **PASS** (target 0). Setup (a). |
| 2026-10-19 | f721375 | P-4 idle `ProcessInput` calls | 0 over 4,042 frames of hovering | baseline | **PASS** (target 0). Setup (a), 8 mods, 454 entries; BAM clicks do reach our prefix (setup b). **R-1 stays closed.** Idle count with BAM loaded: see note 4. |
| 2026-10-19 | f721375 | P-6 open latency | 21/21 opens drawn in the trigger frame (`same=True`) | baseline | **PASS** (target same frame). Setup (a). The 30 fps variant of the check was not run. |
| 2026-10-19 | f721375 | P-7a warm open path | median 158.8 us; first open 1,213 us; max 1,213 us (21 opens); bytes median 57.7 KB, max 72.0 KB | baseline | **PASS** (limit 50% of 3 ms = 1.5 ms; first open at 81% of that limit). Skeleton, no pins, catalog already built; re-measure with 40 pins at UC-2. Allocation: note 1. |
| 2026-10-19 | f721375 | P-8 open draw + input per frame | median 3.03 us; p99 11.27 us; max 386.7 us (4,054 steady frames) | baseline | **PASS** (limit 0.5 ms) but **not representative**: skeleton wheel has 2 slots. Control window (2 slots, same calls): median 2.80 us. Re-measure with the real wheel (40 pins, sub-ring, grid, max size) at UC-2. |
| 2026-10-19 | f721375 | P-9 open allocations per frame | steady: 32 B/frame (control 32 B/frame, +0); first frame after open: max 65,672 B (control 328 B) | baseline | Steady state **PASS** (limit 50% of 512 B over control = 256 B; +0). First-frame-after-open 65.7 KB: was FAIL vs the old 8 KB guess; **PASS** vs the re-based 128 KB budget (user decision 2026-10-19); see note 1. Same 2-slot caveat as P-8. |
| 2026-10-19 | f721375 | P-11c catalog build (then still called "catalog + index"; no index was built) | (a) 5.19 ms, 454 entries, 8 mods; (b) 5.33 ms, 718 entries, 10 mods | baseline | **PASS** (limit 50% of 200 ms = 100 ms; 5% of it). One build per setup, so no spread. Not yet run on the 2,000-designator stress workload (W-1). |
| 2026-10-19 | f721375 | P-12 builds per session | 1 (both setups) | baseline | **PASS** (target 1). |
| 2026-10-19 | f721375 | P-18 mod startup | (a) mod ctor 29.05 ms, all of it patching (scan + patch of 8 overrides 23.50 ms); (b) patching 19.00 ms, 8 patched, 0 failed | baseline | **PASS** (limit 50% of 150 ms = 75 ms for the vanilla/DLC set; 39% of it). Settings load and Mint/BAM/MP detection are not implemented yet and not included. Setup (b) total ctor not logged. |
| 2026-10-19 | T-24 | CI gates v1 (Perf trait, `scripts/perf.sh`) | 0 bytes over 10,000 calls: `PinClickClassifier`+`ModifierPolicy`, `PinClickDeduper`, `TriggerStateMachine.Handle`, `HitTester.Hit`+`WheelHitTest.SlotAt`, `EdgeDodger.Fit` (`ZeroAllocTests`); `PinBoard.Toggle` <= 2 KB (`PinBoardPerfTests`); P-10 gate 250 us (`LayoutTimingTests`, 60 slots); P-16/P-17 gate 25 ms (`SerializerPerfTests`, 200 pins) | gates | **PASS** (all gates green). Gates, not measurements: real numbers come from `scripts/perf.sh --bench` (`LayoutBenchmarks`, `PinBenchmarks`, `MaskBenchmarks`; compiled in CI, not yet run). Search (P-14) gate waited for the Core search type; dropped on 2026-10-19 with the picker. Linux container, Release. |
| 2026-10-19 | T-53 | CI gates v2 (grid, Auto, sub-menu) | 0 bytes over 10,000 calls: `HitTester.HitGrid` (with plate and button, sweeping hub/slot/plate/padding/outside), `Capacity` Auto/Fixed queries, `WheelLayout.ComputeSpread`, `SubMenuLayout` satellite slots; P-10 gates 250 us (median of 5 runs) for grid `Place` 7x7 + Auto queries + one sub-grid, and Auto wheel + one 12-slot satellite (`LayoutTimingTests`); benchmarks added to `LayoutBenchmarks` (compiled, not run) | gates | **PASS** (22 Perf tests green). Gates, not measurements. Linux container, Release. |
| 2026-10-19 | T-28 | Sized texture rebuild (rings, rims, glyphs), Core rasterization only | worst case B 96, 8 rings (Auto limit), UI scale 2, rim capped at 2048 px; gate 25 ms (`TextureRebuildPerfTests`) | gate | **PASS** (gate). **Rebuild including the upload (`SetPixelData` + `Apply(false, true)`) is not measured**: measure at UC-2 from the dev-mode log lines "Textures rebuilt … (with upload)" and "Satellite rim rebuilt …" against the 5 ms budget. |
| 2026-10-19 | UC-2 (setup b, user PC) | P-16 settings load | 92.6 ms | **FAIL** (limit 50% of 20 ms = 10 ms) | Whole mod ctor was 124.6 ms (patching 30.5). The old scope wrapped `GetSettings` (the game's file read, Scribe and first-use JIT) plus our code, so it was not an honest measure of our part. Our Core parsing is warm-gated at 25 ms for 200 pins and is not the cost. |
| 2026-10-19 | UC-2 | Texture rebuild incl. upload (T-28) | 6.46 ms first open, 5.08 ms later (338 px rim, B 56, 2 rings; 16 textures) | **FAIL (marginal)** (budget 5 ms) | Rasterizing is small (Core gate); the time is `new Texture2D` + `SetPixelData` + `Apply` for about 16 textures, the 338 px rim being the largest upload. |
| 2026-10-19 | UC-2 | P-9 window allocations | median 64 B/frame (control 32, +32); p99 1,912 B; max 2,328 B | median PASS; p99 over 512 B | Not attributed in-game yet; see T-35 below. |
| 2026-10-19 | T-35 (changes, not yet re-measured in game) | P-16 / texture / P-9 follow-up | see notes | pending UC-3 | **P-16**: (1) timing split: `GetSettings` total, scribe pass, our `PostLoad`, our code after, plus `JitInfo` ms and methods during `GetSettings`, all in the self-test/perf report line "settings breakdown"; (2) removed the per-value-type generic JIT in `QuickMenuSettings` (`LookText<T>`, `Reset<T>`, `Enum.IsDefined<T>`, 7+ instantiations, each a separate JIT compile) in favour of non-generic helpers. Expected: our part under 10 ms; the game's own file read and Scribe stay in `GetSettings` and are reported separately, so the verdict on P-16 should use "our" = PostLoad + after. If JIT-during-GetSettings is still large, the rest is the game's. **Texture**: sized textures are now built at startup (loading screen) from the saved settings (`Textures.Prewarm`, Wheel layout), so the first open rebuilds nothing (6.46 ms -> about 0 ms when sizes match); a later rebuild after a settings change is unchanged (about 5 ms) and now logs the create/upload share separately. **P-9**: new dev sub-buckets under the window stat (hover sound, `TipRegion`, label plate) so UC-3 shows which of them carries the p99. Suspects, both game code: `PlayOneShotOnCamera` on a hover change and `ActiveTip` creation in `TipRegion` at the start of a hover; neither can be removed without dropping a feature (hover sound, tooltip). |
| 2026-10-19 | UC-3 (38bd131 recheck; setup b + stress mod, 11 mods, 2,850 catalog entries, 1,702 top-level designators) | P-11c catalog build | 10.9 ms; builds=1, complete, 0 duplicates, round trip 100 % | vs UC-1: 5.3 ms at 718 entries | **PASS** (limit 50% of 200 ms = 100 ms; 11% of it, about 4 us per entry). One build, so no spread. Setup (R) and W-0/W-1 counts were not recorded separately: the stress mod run is the largest workload measured (2,000 generated defs on top of the real set). Not the 8,000-def run. |
| 2026-10-19 | UC-3 | P-12 builds per session | 1 | | **PASS** (target 1). |
| 2026-10-19 | UC-3 | P-18 mod constructor | 87.3 ms (settings incl. the game's read 56.1, patching 30.3) | UC-2: 124.6 ms | **PASS** (stress setup counts as the large list: 50% of 300 ms = 150 ms; measured 87.3 ms is 58% of that line, 29% of the budget). Against the vanilla tier (50% = 75 ms) it would be over, but that tier does not apply to an 11-mod, 2,850-entry setup. Lower than UC-2, consistent with the T-35 JIT removals (not isolated). |
| 2026-10-19 | UC-3 | P-16 settings load, our part | 4.2 ms + 0.17 ms | UC-2: 92.6 ms (whole scope) | **PASS** (limit 50% of 20 ms = 10 ms; the earlier FAIL measured the game's read, now split out at 56.1 ms and not ours). |
| 2026-10-19 | UC-3 | P-7a warm open path | median 78 us, max 101 us, 4.7 KB | UC-1: 158.8 us, 57.7 KB | **PASS** (limit 1.5 ms; 5% of it). Open allocation down about 12x from UC-1. |
| 2026-10-19 | UC-3 | P-8 draw + input per frame | median 39.7 us, p99 75 us | UC-1: 3.03 us (2-slot skeleton) | **PASS** (limit 0.5 ms; 8% of it; p99 15%). First measurement with the real pin set. Sub-ring draw is **not** isolated, see not-measured list. |
| 2026-10-19 | UC-3 | P-9 allocations per frame | median 96 B/frame (no control run) | UC-2: 64 B (control 32) | **PASS provisional** (limit 50% of 512 B = 256 B over control). Against the UC-1/UC-2 control of 32 B, the delta is about +64 B, inside the limit; a same-session control was not run, so the delta is inferred. |
| 2026-10-19 | UC-3 | Satellite rim rebuild (first sub-ring open) | 3.1 ms (210 px rim, with upload) | | **PASS** (budget 5 ms; 50% = 2.5 ms, so **over the 50% line at 62% of budget**; the 5 ms figure is already the hard texture budget, so treated as PASS, watch item). |
| 2026-10-19 | UC-3 | Texture prewarm at startup | 14.0 ms rebuild (10.2 ms create + upload); 15 masks uploaded in 24.6 ms | UC-2: 6.5 ms on first open | Off the play path (loading screen). First open no longer pays the 5 to 6 ms rebuild (UC-2 FAIL is closed by the prewarm). No budget row; informational. |

**M3 not measured at UC-3 (carry-over):**
- **P-7c Ideology refresh time in game**: the refresh crashed (`NullReferenceException` in the fingerprint), disabled itself, and the recheck (38bd131) confirmed no error and 4/4 opens in the same frame, but did not log refresh time. **Must re-measure at UC-4** (add a Stopwatch around `RefreshIdeology`, with Ideology active, on a colony with an ideoligion; target 0.5 ms, pass at 0.25 ms). The fingerprint short-circuit makes this likely cheap, but that is an unmeasured guess.
- **P-9 control run in the stress setup**: re-run the empty-window control in the same session at UC-4 so the delta is measured, not inferred.
- **Sub-ring per-frame cost separately**: P-8 was reported for the whole window, not split into wheel and sub-ring. Report P-8 with a sub-ring open (S-5) at UC-4, and also with grid layout and maximum size once M4 adds them.
- **8,000-def stress run**: only the 2,000-def stress mod ran (2,850 entries). The 8,000-def size was not run. 11% of budget at 2,850 entries suggests linear scaling would stay under the 100 ms line at about 25,000 entries, so a run is low priority; do it at the M6 final pass.
- Run-to-run spread: single session, single run. Pin counts per ring and W-0/W-1/W-2 workload IDs are not logged separately from the setup description.
- Not re-measured since UC-2 and still pending: P-3 (pin prefix), P-5, P-17 in game, texture rebuild after a settings change.

No M3 row fails the 50% line except the satellite rim watch item. No `perf:` task is required.

Machine: user's main Linux PC (Fedora, 2560x1440, UI scale 1.0); RimWorld 1.7.4982 (Waylith, CoreCLR .NET 10); Harmony mod 3.0.1.0 (lib 3.0.0-rc.3); dev-mode Stopwatch scopes (`PerfCounters`), not the Frame Profiler. Single session, single run per setup, medians over the frame counts shown; no run-to-run spread yet.

### UC-1 baseline notes (2026-10-19)
Pass/fail uses 50% of each budget (architecture section 7 hardware margin), except the zero/equality budgets (P-2, P-4, P-6, P-12), which need no margin. Setup (a): Harmony + 5 DLC + Quick Menu, 8 mods, 454 entries. Setup (b): (a) + Better Architect Menu, 10 mods, 718 entries.

1. **Open-path allocation (~58 KB per open, 72 KB max; first frame of the window up to 65.7 KB).** Time is fine (first open 1.2 ms). The allocation is per user action (one open), not per frame, so at a few opens per minute it is negligible for the GC (a gen0 budget is far larger). It does exceed the written P-9 first-frame limit (8 KB), which I set before the open path existed, so that number was a guess, not a requirement. Recommendation: do not add a hard per-open byte budget now. Instead (a) re-measure at UC-2 with the real wheel and 40 pins, (b) set a regression guard for the open path of about 2x the UC-2 measurement and require that it does not grow with the number of catalog entries, (c) re-base P-9 first-frame to "<= 128 KB per open, recorded, no growth with catalog size". Find the source with the `MeasureScope` split (Window construction, `WindowStack.Add`, first layout, first IMGUI call) before changing code; this cost may be engine-side (the control window allocates only 328 B in its first frame, so mod-side `Window` setup or text/GUIContent caches are suspects). Do not optimise yet: no budget requires it.
2. **P-8 and P-9 were measured with the 2-slot skeleton.** They prove the harness and the no-regression floor (+0 B over control), not the cost of 40 pins. Re-measure at UC-2.
3. **Poller max 281 us (10,000-frame sample) and 311 us (35,844-frame report).** One outlier per run against a p99 of 0.27 to 0.54 us, so it is not a steady cost. The log shows the 10,000-frame report is printed from inside the postfix (`PrintPendingReport`), and the poller's own max bytes of 59,648 in one frame matches report-string building, so the max most likely includes the report printing frame, or a GC pause or scheduler preemption. This is a hypothesis; I did not isolate it. Impact: 0.3 ms is 1.9% of a 16.7 ms frame, once per 10,000 frames. Action: exclude the report-printing frame from the sample (record the sample before printing), then check whether max stays under about 50 us. No fix on the mod's hot path is warranted.
4. **P-4 with BAM loaded**: setup (b) confirmed BAM calls `ProcessInput` on clicks, but the log does not record an idle-hover count with BAM; the 0 over 4,042 frames is setup (a). Re-run the idle count with BAM at the next in-game session.
5. **Frame Profiler was not used.** The P-1 cross-check (Waylith Frame Profiler, filter `QuickMenu!*`) and the Frame Profiler overhead question (section 7, question 4) are still open. All numbers above are Stopwatch/allocation scopes inside the mod.
6. **P-1 floor**: measured 0.15 us against a 2 us goal, so per the note under section 1 the target can be tightened (proposal: <= 2 us hard, 0.5 us goal) once the Frame Profiler cross-check confirms the wrapper cost.

Every entry states: machine (CPU, RAM, OS), game build (`1.7.4982 rev1280` or later), runtime, workload ID, loaded mod count and designator count, number of runs, median and spread.

## 6. Known bottlenecks and risks

All are hypotheses until measured.

| ID | Risk | Status | Mitigation / check |
|---|---|---|---|
| R-1 | *(revised)* Pin hook cost multiplied by gizmo count | Largely **removed** by ADR 0005 (prefix is on `ProcessInput`, click-only). Residual: the assumption must hold with BAM and Mint | P-4 idle-invocation counter (spike S-3). Fast path stays allocation-free and cheap anyway: classify the event first, read modifier state second |
| R-2 | Closed-state poll allocates or is slow | Low risk by design (ADR 0003) | P-1, P-2. Check that `Input.GetKeyDown` on UnityStubs (`GetKeyDownRaw -> WasPressed -> TryGetKeyControl`) does no allocation or dictionary-heavy lookup per call; measure it once. The Harmony postfix wrapper and try/catch also have a fixed cost; included in P-1 |
| R-3 | First open pays for the lazy catalog build, which delays that frame | Open, conflict C-1 | P-7b. Resolve pins without the full catalog, or build during loading |
| R-4 | Ideology refresh on every open | Possible per-open cost | P-7c. Refresh only if the ideology/precepts changed, if the game offers a cheap change signal |
| R-5 | Availability for all pins evaluated on open; `Designator_Build.Visible` walks research and prerequisites | Possible | Included in P-7a. Cache per open; recheck only on hover |
| R-6 | IMGUI and `Widgets` allocate in the engine (strings, `GUIContent`), blamed on the mod | Possible | P-9 control run on an empty window. Keep labels and tooltips cached per open |
| R-7 | Settings written to disk too often (`PinService` "writes settings on change") | Open question to the architect | Write when a pin changes by click, and for edit mode on edit end or menu close, not per drag step. Counter in the self-test |
| R-8 | ~~Picker list drawing all rows~~ | **Dropped** (no picker, 2026-10-19) | — |
| R-9 | Startup patching of every `ProcessInput` override scales with the mod list; the reflection scan over all types is not free | Possible | P-18. Scan once, restrict to subclasses of `Designator` via `GenTypes.AllSubclasses` as designed |
| R-10 | Effective modifier computed per click via `ModsConfig.IsActive("Dubwise.DubsMintMenus")` | Possible small cost per click | Cache the effective modifier at startup and on settings change (`ModifierPolicy.Effective` takes `mintActive` as an input, so the adapter can cache it) |
| R-11 | Waylith IMGUI cost per control unknown | Unknown | Measured by P-8; fall back to fewer controls per slot (draw icon plus one hit area) |
| R-12 | BenchmarkDotNet and coverage runs on shared CI are noisy | Known | Loose gates, separate steps (section 4) |

## 7. Questions for the user (with recommendations)
1. **Are the targets acceptable** (closed <= 10 us per frame, open draw <= 1 ms, catalog build <= 200 ms hard with a 100 ms goal, warm open <= 3 ms)? Recommendation: yes. They are tight enough to mean something and I tighten them once measured.
2. **What is your main machine, and is there a slower one that the 200 ms index limit must hold on?** Recommendation: measure on the main machine and treat roughly 2x slower as the "must still pass" case, which the 100 ms goal covers.
3. **First open of the session: is one slow frame (up to 200 ms) acceptable, or should the menu always open instantly?** Recommendation: instant. Build the catalog during the loading screen or on the first idle frames after load, so the first open is as fast as every other open. The cost, under 200 ms on a loading screen that is already tens of seconds, is not noticeable. This reverses ADR 0006's "lazy on first menu open". If you prefer to keep lazy, the first open is a one-time hitch and P-7b becomes the budget.
4. **Do you want the Frame Profiler's overhead measured first?** Recommendation: yes, in the first perf task, by comparing a frame with and without the filter. Until then, absolute numbers come from the Stopwatch scopes.
5. **Which mod list is your real workload?** You have about 980 workshop items installed but most will not load on 1.7. Recommendation: define a named list of the mods that do load on 1.7, and use that list plus the stress mod for all in-game numbers.

Resolved by the architecture (no longer open): lazy index build (ADR 0006, but see C-1); Dubs Performance Analyzer availability (not used; Frame Profiler instead); dev-mode probe (replaced by Frame Profiler plus the small `MeasureScope`); runtime and tooling (net10.0, CoreCLR, xUnit v3, BenchmarkDotNet).

## 8. Technical needs from the architecture (status after reconciliation)

| ID | Need | Status |
|---|---|---|
| T-1 | Pure logic in a separate assembly with no game/Unity references | **Resolved**: `QuickMenu.Core` (ADR 0002) |
| T-2 | Plain-data input type for the indexer, so a synthetic generator needs no game | **Obsolete** (2026-10-19): there is no indexer since the picker was dropped |
| T-3 | Hook early-out, no allocation, no reflection | **Resolved and relaxed**: prefix on `ProcessInput`, click-only (ADR 0005). Keep: classify event first (key event or non-left-click returns immediately), then modifier check. Decision logic in Core (`ModifierPolicy`) |
| T-4 | Closed-state poll is one cheap check | **Resolved**: ADR 0003 postfix on `UIRootUpdate` |
| T-5 | No per-frame allocation in the open-state draw loop; layout cached | **Partly resolved**: layout cache and no-LINQ rules are in section 7 of the architecture. "Availability on open and hover" still needs to be in the UI task acceptance criteria. (The virtualised picker is dropped with the picker, 2026-10-19.) |
| T-6 | Switchable timing/allocation probe | **Changed**: Frame Profiler replaces a custom probe. I still need a tiny dev-mode `MeasureScope` (Stopwatch plus `GC.GetAllocatedBytesForCurrentThread` around the poller, the window, and the catalog build), reported by the self-test, gated by one cached flag |
| T-7 | Index build is a single callable unit with cached reflection | **Resolved**: catalog build in the adapter (the Core `SearchIndex` was dropped on 2026-10-19). Keep icons, tooltips and translations out of the build |
| T-8 | Settings saved only on change, not per drag step | **Open**: architecture says "writes settings on change" (PinService). See R-7 |
| T-9 | Do not assume runtime/tooling | **Resolved**: net10.0, CoreCLR, Harmony 3.0.0-rc.3 |

## 9. Asks for the architect

Previously open items, now:
1. Assembly split and test framework: **resolved** (ADR 0002, 0008).
2. Hook patch target and early-out: **resolved** (ADR 0005); verify idle invocation count is 0 (P-4).
3. When the catalog and index are built: **open, conflict C-1** (below).
4. Settings write cadence: **open** (R-7, T-8).
5. New asks: (a) add `tests/QuickMenu.Core.Benchmarks/` and a shared `SyntheticCatalog` source file to the layout and `QuickMenu.slnx` (L-1; ADR 0008 does not list them); (b) ask the environment-engineer for `scripts/perf.sh`, separate CI steps for coverage and Perf-trait tests, and a `.gitignore` entry for `BenchmarkDotNet.Artifacts/`; (c) add the dev-mode `MeasureScope` and the extra self-test counters (T-6); (d) cache the effective pin modifier (R-10).

### Conflicts with the architecture
- **C-1 (real)**: NFR-1 says the menu "opens within one frame". Architecture section 7 and ADR 0006 build the catalog lazily on first menu open and refresh the Ideology part on every open. The window can be added in the same frame, but the first open then also runs the catalog build (up to 200 ms), which is a long frame, and every open pays the Ideology refresh. Recommended: resolve pinned entries without the full catalog (or build the catalog after load), and make the Ideology refresh conditional (P-7a, P-7b, P-7c, R-3, R-4).
- **C-2 (small)**: the architecture's performance row says "Core micro-benchmarks as xUnit timing tests". I agree for gates, but real numbers need BenchmarkDotNet, which adds a project not in ADR 0008 (L-1).
- **C-3 (small)**: the architecture runs coverage on Core tests; timing and zero-allocation tests must not run under coverage (section 4).
- **C-4 (informational)**: spec NFR-1 and the task brief assumed the hook runs on every gizmo draw. The architecture removes that cost. I replaced P-3/P-4 accordingly. The risk returns only if S-3 finds `ProcessInput` called outside clicks.
