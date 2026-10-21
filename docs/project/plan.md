# Implementation Plan: rimworld-quick-menu (Quick Menu)

- **Status**: approved (2026-10-19); revised 2026-10-19 after UC-2 (user decision: the in-menu picker, the "+"/Add slots and the edit-mode insert cursor are removed, FR-7 → Won't; UC-2b added to M2); revised 2026-10-19 for FR-19 (pin marker on pinned architect buttons and dropdown entries, M6 tasks T-87 … T-92, last feature before release prep; user request); revised 2026-10-19 after UC-4 (user decision: hold-and-release mode dropped, FR-2 is press-to-toggle only; T-63/T-64 removed, cleanup T-94 and slider-rebuild perf fix T-95 added to M5)
- **Spec**: [spec.md](spec.md) · **Architecture**: [architecture.md](architecture.md) · **ADRs**: [decisions/](decisions/) · **Design**: [design.md](design.md) · **Performance**: [performance.md](performance.md) · **Environment**: [environment.md](environment.md)

## Planning notes

- **Agents cannot run the game.** Every in-game check is done by the user in a few grouped **user sessions** (UC-1 … UC-6, plus the short follow-up UC-2b), one at the end of each milestone. A task that needs the game says **In-game check: UC-n (user)**. Its acceptance criteria marked *(UC-n)* are ticked only after the user reports back. Everything else is verified by `scripts/test.sh`, `scripts/perf.sh`, `dotnet build` and `dotnet format --verify-no-changes`.
- **Each user session has a guide** (written by the test-engineer in `docs/project/test-checklist.md`, section per session). It gives exact steps, the setups to use, what to look at, and what to send back. Every in-game output the session needs is written to the game log between `[QuickMenu] BEGIN <name>` and `[QuickMenu] END <name>` markers, so the user can copy blocks without reading them.
- **Setups** (architecture §9): (a) vanilla + all DLC; (b) + Better Architect Menu; (c) + Dubs Mint Menus; (d) + BAM + Mint; (e) + Multiplayer when it supports 1.7 (A-2); (R) the user's real mod list that loads on 1.7.
- **Done for every task**: tests written first; `scripts/test.sh` (Core ≥ 90 % lines) and `scripts/perf.sh` pass; build has 0 warnings; format check passes; every new player-facing string is Keyed (NFR-4); every patch body and OnGUI entry is wrapped by `PatchGuard` (NFR-6) once T-5 exists; no LINQ, string building or closures in per-frame paths.
- **Keyed files**: strings go in `mod/Languages/English/Keyed/`, split per area (`QuickMenu.Menu.xml`, `QuickMenu.Settings.xml`, `QuickMenu.Messages.xml`, `QuickMenu.Notices.xml`, `QuickMenu.Debug.xml`) so parallel tasks do not edit the same file. The game loads every file in the folder. Keys stay prefixed `QuickMenu.` (architecture §6 names one file; see Question 6).
- **`parallel-ok`** on a task means it has no file overlap with the other `parallel-ok` tasks of the same milestone, and it can start as soon as its listed dependencies are done.
- **Gated mode**: each milestone ends with its user session and the user's approval before the next milestone starts.
- **Scope change (user, 2026-10-19, after UC-2)**: pins are added only by modifier+click in the architect menu (vanilla or BAM, FR-8). There is no picker window, no search index or search UI, no "+"/Add slot and no insert cursor; empty slots stay empty. New pins go into the first free slot (Fixed) or at the end (Auto); the user rearranges by dragging in edit mode. Edit mode stays (centre button = Edit/Done; drag to swap; modifier+click on a slot unpins, also in use mode; right-click and click-outside close). The designator catalog stays (it resolves pins), including its Ideology part (T-40). Removed tasks keep their IDs and are marked `[-]`; nothing was renumbered.
- **Release fast-track (user, 2026-10-21)**: release as **1.0.0** instead of 0.9.0 (T-80). UC-6 (T-75) stopped after Part 9; Parts 10–16 (Mint with/without BAM, 8,000-def stress run, preview/T-70 screenshots, Frame Profiler, NFR-3 Harmony-only load test, Forget missing pins, removing the mod from a save) are waived by the user as an accepted exception. Fixed during UC-6: pin-message ellipsis capitalisation; P-19 and P-5 measurement scopes.
- **Scope change (user, 2026-10-19, FR-19)**: a small pin marker is drawn on every pinned architect button (a dropdown button when the whole dropdown is pinned) and on pinned single entries inside an open dropdown list, in any architect UI that pinning works in (vanilla, BAM, …), updating on the next frame after a pin or unpin. It is the last feature of M6, before release prep: an offline hook spike and ADR 0009 (T-87), the P-19 budget (T-88), the marker glyph (T-89), a Core pinned-set cache with a zero-allocation O(1) lookup (T-90), then the two adapter hooks (T-91 buttons, T-92 dropdown entries). UC-6 (T-75) checks it in game. Existing tasks keep their IDs; only T-75 (dependencies, scope), T-78 (README line) and the M4 count (T-86 is removed) changed.
- **Scope change (user, 2026-10-19, after UC-4)**: hold-and-release mode is dropped (spec FR-2: press-to-toggle only, no trigger-mode setting). T-63 (Core hold mode) and T-64 (hold mode in the window, trigger-mode setting) are removed and marked `[-]`. The partial hold-mode code already in Core (`TriggerStateMachine.HoldMode`, the `TriggerReleased` event, the release-over-hub rule), the `TriggerMode` enum and setting, their tests and the hold wording in design.md and architecture.md are removed by T-94; old settings files that contain `triggerMode` still load (the value is ignored). The UC-4 settings-slider texture rebuild FAIL (performance.md UC-4 record) becomes T-95. Finished tasks (`[x]`) that mention hold mode (T-11, T-44, T-81) are history and are not edited.

## Milestones
| ID | Milestone | Goal / demoable outcome | Tasks | Count |
|---|---|---|---|---|
| M1 | Walking skeleton + spike build | The mod loads on 1.7.4982 with Harmony 3. Mouse 4 on a colony map opens a crude wheel with two hard-coded pins (Mine, Wall); clicking one selects the designator. A dev-mode spike build answers S-1 … S-7 in one user session (UC-1); results go into the ADRs. CI is green on GitHub | T-1 … T-15 | 15 |
| M2 | Pin by click, use the wheel | Ctrl+click any designator in vanilla or BAM pins it (message; full; toggle). The bindable trigger opens the styled Fixed wheel centred on the mouse, dodging edges; clicking a slot activates it like the architect. Unavailable pins (e.g. Ideology morbid slabs) always show greyed with the reason. The centre is the Edit button (it does nothing until M3 and never closes the menu); modifier+click on a slot unpins it. Pins and settings persist across saves and restarts. A short follow-up session (UC-2b) checks the post-UC-2 fixes and re-measures settings load, texture prewarm and window allocations | T-16 … T-35, T-81, T-82 | 22 |
| M3 | Sub-menus, materials, edit mode shell | Whole dropdowns pinned from the architect fan out as a sub-ring; stuff builds open a material ring (last-used first); Ideology designators resolve and refresh; edit mode shell (the centre Edit button enters it and shows Done; Done/Esc leave; modifier+click unpins; plain clicks do nothing); design, performance and architecture documents aligned with the picker removal | T-36, T-37, T-40 … T-44, T-46 … T-49, T-83 … T-85 | 14 |
| M4 | Edit mode, grid, Auto sizing | Drag to swap (Fixed: onto an empty slot moves the pin there), help panel, full state, one settings write per edit session; grid layout with sub-grids; Auto sizing (new pins at the end); full layout settings with live preview; empty-menu hint (the satellite underlay T-86 was removed by the user) | T-50 … T-62 | 13 |
| M5 | Availability, compatibility, settings cleanup | Greyed slots with the game's (or composed) reason for every cause; missing-mod pins kept and restored; Mint detection with Alt default and notices; first-run notice; Multiplayer-safe audit; settings page polish; the partial hold-mode code and the trigger-mode setting removed (hold mode dropped by the user; old settings files still load); dragging the button-size slider no longer rebuilds textures on every step | T-65 … T-71, T-93 … T-95 (T-63, T-64 removed) | 10 |
| M6 | Pin marker, hardening and release prep | Pinned designators show a small pin marker on their architect button and pinned single entries show it in an open dropdown list, in vanilla and BAM, updating the next frame after pin/unpin at no measurable per-frame cost (FR-19, last feature, before release prep); fault isolation verified, string and clean-room audits, final performance pass, final regression on all setups (marker included), Preview.png, README, CHANGELOG, Workshop description, release package (publishing is the separate gated Phase 7) | T-72 … T-74, T-87 … T-92, T-75 … T-80 | 15 |
| | | | **Total** | **89** active (95 IDs; T-38, T-39, T-45, T-63, T-64, T-86 removed) |

**Critical path** (milestone gates included): T-3 → T-6 → T-7 / T-9 → T-13 **[UC-1]** → T-14 → T-28 → T-29 → T-32 → T-33 **[UC-2]** → T-34 → T-81 → T-82 **[UC-2b]** → T-41 → T-43 → T-47 **[UC-3]** → T-56 → T-57 → T-60 **[UC-4]** → T-93 → T-95 → T-69 **[UC-5]** → T-87 → T-90 → T-91 → T-92 → T-75 **[UC-6]** → T-77 → T-80. In M6, T-72 (fault-injection list), T-88 (P-19 budget) and T-89 (marker glyph) run beside T-87/T-90 and must finish before T-91; T-73 and T-74 run beside the whole marker chain. In M5, T-65 (M) and T-67 (M) are near-critical: each is one M task before T-69, against the two S tasks T-93 → T-95 (same file `SettingsPage.cs`, so they run in sequence); T-66, T-68 and T-94 run beside them. Each user session sits on the path, so how soon you can run a session decides the schedule more than agent work does.

## User in-game sessions

| Session | When | Setups | What the user does (details in the session guide) | Feeds |
|---|---|---|---|---|
| **UC-1** Spike session | End of M1 (T-13) | (a), then (b) or (R) | Load the spike build, run 5 dev-mode debug actions, try the skeleton wheel, take 4 screenshots, paste the log blocks back. About 45–60 min | T-14, T-15 (ADRs 0001/0003/0005/0006/0007, design.md §6, performance.md §5) |
| **UC-2** | End of M2 (T-33) | (a), (b) | Bind the trigger, pin by Ctrl+click in vanilla and BAM, use the wheel, edge-dodge at corners, restart and load a second save, measurement actions, screenshots | T-34, T-35 |
| **UC-2b** | End of M2 (T-82), after T-81 | (a) with Ideology; (b) for one step | Short follow-up (about 15–20 min): the centred Edit button (does nothing, never closes), modifier+click unpin on the wheel, greyed unavailable pins (morbid slabs, a research-locked build), one screenshot of a mostly empty wheel, and the T-35 re-measure (settings load breakdown, texture prewarm at first open, P-9 window allocation sub-buckets) | T-35, T-81 (visual check) |
| **UC-3** | End of M3 (T-47) | (a), (b), (R), stress mod | Sub-rings and material rings from pins made in the architect (vanilla and BAM chair dropdown), Ideology designators and refresh, edit mode shell (enter, Done, Esc, modifier+click unpin, plain click does nothing), self-test on (R) and the stress mod, measurement actions | T-48, T-49 |
| **UC-4** | End of M4 (T-60) | (a) | Edit mode drag swap (wheel and grid, Fixed and Auto), help panel and full, grid, Auto, settings preview, empty-menu hint, UI scale 1.0 and 2.0 screenshots | T-61, T-62 |
| **UC-5** | End of M5 (T-69) | (a), (c), (d); remove-mod test | Greyed slots, missing-mod pins, Mint Alt default and notices, first-run notice, removing the mod from a save; the existing settings file (it contains `triggerMode`) loads without errors and the menu still toggles; settings page screenshot (T-93 polish, no Trigger mode row); drag the button-size slider at UI scale 1.5, 5 rings, up to B 96 and read the texture rebuild count and time (T-95), plus one rebuild run at UI 2.0 if the game allows it; pin-click P-3 and P-4 with BAM and Mint | T-70, T-71, T-95 |
| **UC-6** | M6 (T-75), after T-92 | (a)–(d), (R), (e) if available | Full regression checklist; pin marker (FR-19): marker on pinned buttons in vanilla (a), BAM (b) and Mint's architect (c/d), on a pinned whole dropdown button, on a pinned single entry in an open dropdown list, gone the next frame after unpin and shown the next frame after pin (architect menu open), readable at UI scale 1.0 and 2.0, P-19 marker-hook measurement with the architect menu open on a full category and with a pawn's command bar shown; fault-injection action (marker sites included); final measurements; Preview screenshots (wheel with a sub-ring, grid, edit mode) | T-76, T-77, release |

## Tasks

Task states: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked · `[-]` removed (kept for traceability; never built)

### M1: Walking skeleton + spike build

- [x] **T-1**: Put the repository on GitHub and get CI green · `parallel-ok`
  - **Owner**: release-engineer
  - **Requirements**: NFR-8 (CI runs the unit suite), NFR-3
  - **Depends on**: —
  - **Files**: `.gitignore` (check `reference/`, `mod/1.7/Assemblies/`, `dist/`, `BenchmarkDotNet.Artifacts/`), `.github/workflows/ci.yml` (only if the first run needs a fix)
  - **Needs the user**: create the public repo `sixdd6/rimworld-quick-menu` (or authorise the agent's `gh repo create`) and allow the first push. No game needed.
  - **Acceptance criteria**:
    - [ ] Given the repo is initialised with `main` and a feature branch, when the branch is pushed and a PR is opened, then the `build-test` workflow passes (restore locked, format, build, `scripts/test.sh`, `scripts/perf.sh`, package artifact).
    - [ ] Given `git ls-files`, then nothing under `reference/`, no `0Harmony.dll` and no build output is tracked.
  - **Tests**: the CI run itself; `git ls-files | grep -E 'reference/|0Harmony|Assemblies/.*dll'` is empty.
  - **Size**: S

- [x] **T-2**: Core geometry, ring capacity and Fixed wheel layout · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-4, FR-6, NFR-8 (design.md §1.1)
  - **Depends on**: —
  - **Files**: `src/QuickMenu.Core/Geometry/{Vec2,Rect2}.cs`, `src/QuickMenu.Core/Layout/{LayoutConstants,RingCapacity,WheelParams,WheelLayout,SlotPlacement}.cs`, `tests/QuickMenu.Core.Tests/Layout/{RingCapacityTests,WheelLayoutTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given k = 1…8, when `RingCapacity.Of(k)` is called, then it returns 6, 12, 18, 25, 31, 37, 43, 50 (with the 1e-9 epsilon).
    - [ ] Given B = 56, 2 rings, Fixed, when `WheelLayout.Compute` runs, then ring 1 has 6 slots at radius 64 at −90°, −30°, 30°, 90°, 150°, 210°, ring 2 has 12 slots at radius 128 every 30° from −90°, and disc radius D = 168.
    - [ ] Given any B in 36…96 (step 4) and rings 1…5, then adjacent slot centres on a ring are ≥ P apart and the capacity is 6/18/36/61/92.
    - [ ] Given e and ey formulas, then at B = 56 e = 20 and ey = 16; at B = 36 and B = 96 the clamps apply.
  - **Tests**: unit `RingCapacityTests`, `WheelLayoutTests` (worked defaults, property test over all B and rings).
  - **Size**: M

- [x] **T-3**: Core `PinKey` grammar, format and parse · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-10, FR-15, FR-17, NFR-8 (ADR 0004)
  - **Depends on**: —
  - **Files**: `src/QuickMenu.Core/Pins/{PinKey,PinKeyKind}.cs`, `tests/QuickMenu.Core.Tests/Pins/PinKeyTests.cs`
  - **Acceptance criteria**:
    - [ ] Given each kind (`build:thing/`, `build:terrain/`, `dropdown:`, `order:` with and without `#n`, `moddrop:type@cat~a,b`), when it is formatted and parsed, then the round-trip is identical and equality is by the canonical string.
    - [ ] Given `moddrop` children in any order, when the key is built, then children are sorted and the key is identical.
    - [ ] Given a malformed or unknown-prefix string, when `TryParse` runs, then it returns false and does not throw.
  - **Tests**: unit `PinKeyTests` (round-trip table, sorting, malformed inputs, characters `|`, `\`, `,`, `~`, `@` in def or type names).
  - **Size**: M

- [x] **T-4**: Core `MaskRasterizer` v0 and PNG export (for spike S-7) · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-7 (own assets), NFR-8 (design.md §3, §6; ADR 0007)
  - **Depends on**: —
  - **Files**: `src/QuickMenu.Core/Render/{MaskRasterizer,PngWriter}.cs`, `tests/QuickMenu.Core.Tests/Render/{MaskRasterizerTests,PngWriterTests}.cs`, `mod/Textures/QuickMenu/Spike/SpikeCircle.png` (spike-only), `docs/project/assets.md` (row for the spike PNG, marked placeholder)
  - **Acceptance criteria**:
    - [ ] Given `Disc(256)` / `Circle(256)`, then centre alpha = 255, corner alpha = 0, and the edge is anti-aliased (alpha strictly between 0 and 255 over ~1 px).
    - [ ] Given `Ring(128, stroke 4.6)`, then the alpha profile across the stroke matches the stroke width within 0.5 px, and the inside is 0.
    - [ ] Given `RoundedRect(w, h, r)`, then the corners follow radius r and the edges are full alpha.
    - [ ] Given the same inputs twice, then the byte arrays are identical (deterministic).
    - [ ] Given `PngWriter.WriteGrayAlpha`, when the spike PNG is written, then it decodes (8-bit gray+alpha, zlib via `System.IO.Compression`) and its row is in assets.md.
  - **Tests**: unit `MaskRasterizerTests` (pixel checks), `PngWriterTests` (header, CRC, decode with `System.IO.Compression` round-trip).
  - **Size**: M

- [x] **T-5**: Adapter foundation: LogOnce, PatchGuard, Harmony bootstrap, skeleton trigger poller
  - **Owner**: developer
  - **Requirements**: FR-1 (skeleton), NFR-1, NFR-3, NFR-6 (ADR 0001, 0003)
  - **Depends on**: —
  - **Files**: `src/QuickMenu/QuickMenuMod.cs`, `src/QuickMenu/Diagnostics/{Log,PatchGuard}.cs`, `src/QuickMenu/Trigger/TriggerPoller.cs`, `src/QuickMenu/Diagnostics/Spike/SkeletonTrigger.cs`, `mod/Languages/English/Keyed/QuickMenu.Debug.xml`
  - **Acceptance criteria**:
    - [ ] Given the mod ctor, when it runs, then Harmony id `sixdd.QuickMenu` applies the postfix on `RimWorld.UIRoot_Play.UIRootUpdate` inside try/catch, and a failure is logged once with `[QuickMenu]` and does not stop loading.
    - [ ] Given a `PatchSite` guarding a patch body (`if (site.Disabled) return; try {…} catch (e) { site.Fail(e); }`, or `PatchGuard.Run(site, body)` on non-hot paths), when the body throws, then the error is logged once per site, the site is disabled for the session, and later calls return without running the body (unit-testable part kept in the adapter, verified in game by T-72).
    - [ ] Given the skeleton trigger (Mouse index 3; debug action "Quick Menu: cycle skeleton trigger" switches Mouse 4 / Mouse 5 / Middle), when the button is pressed on a colony map with no modal window and not in world view, then `MenuController.Open` is called once; otherwise nothing happens. The postfix body does no allocation (early return, one `Input` call).
    - [ ] *(UC-1)* Given setup (a), when the game loads, then the log shows no `[QuickMenu]` errors and the mod appears in the mod list.
  - **Tests**: build; in-game UC-1 (load, trigger).
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [x] **T-6**: `DesignatorCatalog` v1 (static part) and Core `PinKeyFactory`
  - **Owner**: developer
  - **Requirements**: FR-7, FR-10, FR-13, NFR-1, NFR-2 (ADR 0004, 0006)
  - **Depends on**: T-3
  - **Files**: `src/QuickMenu.Core/Pins/{PinKeyFactory,DesignatorDescriptor}.cs`, `tests/QuickMenu.Core.Tests/Pins/PinKeyFactoryTests.cs`, `src/QuickMenu/Catalog/{DesignatorCatalog,CatalogEntry}.cs`
  - **Acceptance criteria**:
    - [ ] Given plain descriptors (kind, type full name, def name, category, child keys, dropdown group), when `PinKeyFactory.Assign` runs, then builds get `build:`, def dropdowns whose children share one group get `dropdown:`, other dropdowns get `moddrop:`, other designators get `order:`, and `#n` is added only when a type appears more than once (n by category order then def order).
    - [ ] Given the game finished loading defs, when the `[StaticConstructorOnStartup]` build runs, then it enumerates every `DesignationCategoryDef`'s resolved designators (Ideology part excluded, added in T-40), flattens dropdown children, builds the key → entry map, records `packageId` per entry, and times the build with a `Stopwatch` (exposed to the self-test).
    - [ ] Given a key, when `Resolve(key)` runs, then it returns the exact match, or for `build:` keys falls back to `BuildCopyCommandUtility.FindAllowedDesignator(def)`, or returns unresolved without throwing.
  - **Tests**: unit `PinKeyFactoryTests` (kind rules, ordinals, duplicate types); in-game self-test round-trip (T-9, UC-1).
  - **In-game check**: UC-1 (user, via T-9)
  - **Size**: M

- [x] **T-7**: Skeleton menu window (spike S-6)
  - **Owner**: developer
  - **Requirements**: FR-3 (centred), FR-13, NFR-1 (P-6) (ADR 0007)
  - **Depends on**: T-2, T-5, T-6
  - **Files**: `src/QuickMenu/Menu/{MenuController,QuickMenuWindow}.cs`, `src/QuickMenu/Diagnostics/Spike/SkeletonPins.cs`
  - **Time box**: one session. Output: window flags and layer recorded for T-14.
  - **Acceptance criteria**:
    - [ ] Given the trigger fires, when `MenuController.Open` runs, then a full-screen transparent `Window` (no background, no shadow, `forcePause=false`, `closeOnCancel`, layer per ADR 0007) is added in the same frame, and dev mode logs `Time.frameCount` at the trigger and at the first `DoWindowContents`.
    - [ ] Given the window, then it draws a crude 1-ring wheel (flat boxes, designator icons) from `WheelLayout` centred on the mouse, with two hard-coded pins `order:RimWorld.Designator_Mine` and `build:thing/Wall` resolved through the catalog.
    - [ ] Given a slot click, then `designator.ProcessInput(Event.current)` runs and the window closes. Given Esc, a click outside the slots, the trigger again, a map change or the world view, then it closes.
    - [ ] *(UC-1)* Given the menu is open, when the user clicks the map outside the slots, then no designation or selection happens on the map, the game is not paused, and the two frame numbers are equal.
  - **Tests**: build; in-game UC-1.
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [x] **T-8**: Spike S-1 and S-3: patch every `ProcessInput` override in observe mode · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-8, FR-10, NFR-1 (P-4), NFR-2 (ADR 0005)
  - **Depends on**: T-5
  - **Files**: `src/QuickMenu/Pinning/PinClickPatch.cs` (enumeration and patching, production), `src/QuickMenu/Diagnostics/Spike/ProcessInputProbe.cs`
  - **Time box**: one session. Output: patch count, event field table, idle count, dropdown-entry path → T-14.
  - **Acceptance criteria**:
    - [ ] Given all loaded assemblies, when the mod starts, then every declared, non-abstract `ProcessInput(Event)` in `Designator` and its subclasses gets the shared prefix `static bool Prefix(Designator __instance, Event __0)`, each in its own try/catch, and the count of patched / failed methods (with type and assembly) is stored. The prefix always returns `true` in this build.
    - [ ] Given debug action "Quick Menu: ProcessInput probe" is on, when any patched method runs, then it logs instance type, `ev.type`, `ev.rawType`, `ev.button`, `ev.isKey`, `ev.control/alt/shift`, `Input.GetKey` for both Ctrl/Alt/Shift keys, and whether the event is already used; and it counts invocations per frame.
    - [ ] *(UC-1)* The report answers: patched count including at least one mod-assembly override (setup b or R); idle invocations over 600 frames with the architect open (P-4, target 0); which fields reliably show Ctrl/Alt and the left button; whether clicking an entry inside an open dropdown menu calls the element's `ProcessInput` (decides T-27); whether hotkeys arrive with `isKey`.
  - **Tests**: build; in-game UC-1 log.
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [x] **T-9**: Spike S-4 and S-5: "Quick Menu: self-test" debug action
  - **Owner**: developer
  - **Requirements**: FR-7, FR-10, FR-14, FR-17, NFR-1 (P-11c, P-12, P-18), NFR-2 (ADR 0004, 0006)
  - **Depends on**: T-6, T-8
  - **Files**: `src/QuickMenu/Diagnostics/SelfTest.cs`, `src/QuickMenu/Diagnostics/Spike/CatalogDump.cs`
  - **Time box**: one session. Output: resolve rate, duplicates, dev-only rule, reason sources → T-14.
  - **Acceptance criteria**:
    - [ ] Given dev mode, when "Quick Menu: self-test" runs, then it logs between markers: loaded mod count, designator count by key kind, catalog build ms, build counter, startup patch ms and patch count, every key that is duplicated, and every entry whose `Format → TryParse → Resolve` does not return the same designator.
    - [ ] Given "Quick Menu: catalog dump" (spike-only), then it logs every entry: key, type, category defName, mod, `Visible`, `Disabled`, `disabledReason`, and for builds which `Visible` cause applies (research, tech level, prerequisites, monolith, grav engine, place worker), plus every designator or category that only appears with dev mode or god mode on.
    - [ ] *(UC-1)* Given setup (a) and (R), the report shows 0 duplicate keys and a 100 % round-trip, or lists the failures for T-14 to resolve.
  - **Tests**: build; in-game UC-1 log.
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [x] **T-10**: Dev-mode `MeasureScope`, closed-state and window counters
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-1, P-2, P-6, P-7a, P-8, P-9, P-18; performance.md T-6)
  - **Depends on**: T-5, T-7, T-9
  - **Files**: `src/QuickMenu/Diagnostics/{MeasureScope,PerfCounters,ControlRunWindow}.cs`, `src/QuickMenu/Trigger/TriggerPoller.cs` (scope around the body), `src/QuickMenu/Menu/QuickMenuWindow.cs` (scopes around the open path, `DoWindowContents`, `WindowUpdate`), `src/QuickMenu/Diagnostics/SelfTest.cs` (print counters)
  - **Acceptance criteria**:
    - [ ] Given dev mode is off, then the poller and the window pay only one cached bool check for measurement.
    - [ ] Given the window scopes and debug action "Quick Menu: control window" (an empty full-screen transparent window making the same `Widgets`/`GUI` calls, for the P-9 control run), then open-path ms, per-frame draw ms and allocated bytes are printed by "Quick Menu: perf report". T-32 keeps these scopes when it replaces the skeleton window.
    - [ ] Given debug action "Quick Menu: measure closed frames", when it runs for 10,000 frames, then it logs the median, p99 and max µs of the poller body and the total allocated bytes (`GC.GetAllocatedBytesForCurrentThread`), into preallocated counters (the scope itself allocates nothing).
    - [ ] Given the self-test, then it also prints mod ctor time (settings, detection, patching).
  - **Tests**: build; in-game UC-1.
  - **In-game check**: UC-1 (user)
  - **Size**: S

- [x] **T-11**: Spike S-2: input probe · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-1, FR-2 (ADR 0003)
  - **Depends on**: T-5
  - **Files**: `src/QuickMenu/Diagnostics/Spike/InputProbe.cs`
  - **Time box**: half a session. Output: physical button → index table → T-14.
  - **Acceptance criteria**:
    - [ ] Given debug action "Quick Menu: input probe (15 s)", while it runs, then each `Input.GetMouseButtonDown/Up(0..4)` and each `KeyDown` key is logged with the frame number, and every IMGUI mouse event seen in a probe window is logged with its button.
    - [ ] *(UC-1)* The report shows which physical side button is index 3 and 4, that IMGUI never sees buttons 3 and 4, and that up events arrive for hold mode.
  - **Tests**: build; in-game UC-1 log.
  - **In-game check**: UC-1 (user)
  - **Size**: S

- [x] **T-12**: Spike S-7: texture probe window · `parallel-ok`
  - **Owner**: developer (visual-designer reviews the screenshots in T-14)
  - **Requirements**: NFR-5, NFR-7 (ADR 0007, design.md §6)
  - **Depends on**: T-4, T-5
  - **Files**: `src/QuickMenu/Diagnostics/Spike/TextureProbeWindow.cs`
  - **Time box**: one session. Output: chosen texture level (runtime / PNG / baker).
  - **Acceptance criteria**:
    - [ ] Given debug action "Quick Menu: texture probe", then a window shows side by side: (1) a runtime `Texture2D(256,256,RGBA32,false)` made from `MaskRasterizer.Circle` with `SetPixels32` + `Apply`, drawn with `GUI.DrawTexture` at 36, 56 and 96 px, white and tinted cyan at α 0.5; (2) `SpikeCircle.png` loaded with `ContentFinder<Texture2D>`; (3) a generated 9-slice rounded rect with `Widgets.DrawAtlas` at two sizes. Each step is in try/catch and logs success or the exception.
    - [ ] *(UC-1)* The user saves screenshots at UI scale 1.0 and 2.0 to `docs/project/design/spikes/`.
  - **Tests**: build; in-game UC-1 screenshots and log.
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [x] **T-13**: UC-1 spike session guide and run
  - **Owner**: test-engineer
  - **Requirements**: NFR-2, NFR-3 (load with Harmony only)
  - **Depends on**: T-5, T-7, T-8, T-9, T-10, T-11, T-12
  - **Files**: `docs/project/test-checklist.md` (section UC-1), `docs/project/spikes/uc-1-results.md` (user's pasted log blocks, filled after the session)
  - **Acceptance criteria**:
    - [ ] Given the guide, then it lists every step below in order, with the expected result and the log block to copy, and the agent has built Release and checked `mod/1.7/Assemblies` has only `QuickMenu.dll` and `QuickMenu.Core.dll`.
    - [ ] *(UC-1)* Given the user ran the session, then every log block and screenshot is stored in `uc-1-results.md` and `docs/project/design/spikes/`.
  - **Tests**: the session itself.
  - **In-game check**: UC-1 (user). Steps for the user:
    1. Once: run `scripts/link-mod.sh` (symlinks `mod/` into `RimWorld/Mods/QuickMenu`).
    2. Launch with launch options `-mod=brrainz.harmony,sixdd.QuickMenu -quicktest` (setup a: also enable all DLC). Options → Dev mode on.
    3. Debug actions → Quick Menu → **self-test**, then **catalog dump**.
    4. **input probe (15 s)**: press left, right, middle, the front side button, the back side button, and one key, in that order, about one second apart.
    5. **ProcessInput probe** on: open Architect → Structure and move the mouse over the buttons for 10 s without clicking; then click Wall; Ctrl+click Wall; Alt+click Wall; open a dropdown button (one with a corner triangle) and Ctrl+click one entry in its list; press the hotkey of one order if it has one. Probe off.
    6. **measure closed frames** (about 3 min at 60 fps; leave the map idle). Later, after step 7, open and close the skeleton wheel 20 times, open **control window** for 10 s, then run **perf report**.
    7. Skeleton wheel: press Mouse 4 on the map (if nothing happens, **cycle skeleton trigger** to Mouse 5, then Middle). Check: the game keeps running; clicking the map away from the two slots does nothing on the map and closes the menu; Mine slot selects Mine; Wall slot shows the vanilla material menu; Esc closes; opening the world view while it is open closes it.
    8. **texture probe**: screenshot (F12 or the game's screenshot key) at UI scale 1.0, then Options → Interface → UI scale 2.0, screenshot again.
    9. Restart with Better Architect Menu (setup b) or the real list (R); repeat steps 3 and 5 only.
    10. Copy every `[QuickMenu] BEGIN … END` block from the log and paste them back, with the screenshots.
  - **Size**: S

- [x] **T-14**: Record spike results; remove spike-only code
  - **Owner**: developer (visual-designer contributes the S-7 verdict)
  - **Requirements**: FR-1, FR-7, FR-8, FR-10, FR-14, NFR-2
  - **Depends on**: T-13
  - **Files**: `docs/project/decisions/{0001,0003,0005,0006,0007}-*.md`, `docs/project/design.md` §6, `docs/project/architecture.md` §6 and §11, `docs/project/assets.md`, `src/QuickMenu/Diagnostics/Spike/*` (delete), `mod/Textures/QuickMenu/Spike/` (delete unless the PNG path won)
  - **Acceptance criteria**:
    - [ ] Given the UC-1 results, then each ADR has a "Spike result (date)" note: S-1 patch count and any failed overrides (0001/0005); S-2 index mapping and the default label (0003); S-3 reliable event fields, idle count and whether the third patch site is needed (0005, architecture §6); S-4 resolve rate and any key-scheme fixes (0004/0006); S-5 the dev-only rule and the reason sources (0006); S-6 window flags and layer (0007); S-7 the texture level chosen (0007, design.md §6).
    - [ ] Given any S-4 failure, then a follow-up fix is added to this plan as a new task before T-25 (or the existing task's criteria are amended), and the user is told.
    - [ ] Given probe code is no longer needed, then `Diagnostics/Spike/` and the spike PNG are deleted (the assets.md row moves or is removed), the skeleton trigger and pins stay until T-32 replaces them, and the build and tests pass.
  - **Tests**: build; `scripts/test.sh`.
  - **Size**: S

- [x] **T-15**: Performance baseline from UC-1 · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1
  - **Depends on**: T-13
  - **Files**: `docs/project/performance.md` (§5 results log, §3 Frame Profiler steps)
  - **Acceptance criteria**:
    - [ ] Given the UC-1 logs, then §5 has rows for P-1, P-2, P-4, P-6, P-11c, P-12 and P-18 with machine, build, mod and designator counts, and each is marked pass/fail against 50 % of its budget (architecture §7 hardware margin).
    - [ ] Given P-4 is not 0, then the user and the developer are told, and R-1 in performance.md is reopened.
  - **Tests**: none (documentation of measurements).
  - **Size**: S

### M2: Pin by click, use the wheel

- [x] **T-16**: Core `EdgeDodger` and wheel bounding box · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-3, NFR-5, NFR-8
  - **Depends on**: T-2
  - **Files**: `src/QuickMenu.Core/Layout/{EdgeDodger,MenuBounds}.cs`, `tests/QuickMenu.Core.Tests/Layout/{EdgeDodgerTests,MenuBoundsTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given default wheel geometry, then the bounding box is 336 wide, top −168, bottom +206 (plate included).
    - [ ] Given the mouse near any edge or corner, when `Fit` runs, then the shift is the minimum that keeps the box inside `[margin, screen − margin]`.
    - [ ] Given a box wider or taller than the allowed area on an axis, then it is centred on that axis only.
  - **Tests**: unit `EdgeDodgerTests` (four edges, four corners, oversize per axis, UI screen 960 × 540), `MenuBoundsTests`.
  - **Size**: S

- [x] **T-17**: Core `HitTester` for the wheel and hub · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-2, FR-4, FR-9, NFR-8 (design.md §1.1 hit testing, S4a)
  - **Depends on**: T-2
  - **Files**: `src/QuickMenu.Core/Layout/{HitTester,Hit}.cs`, `tests/QuickMenu.Core.Tests/Layout/HitTesterTests.cs`
  - **Acceptance criteria**:
    - [ ] Given a point within P/2 of a slot centre, then `Slot(i)`; on an exact boundary the lower index wins.
    - [ ] Given a point within e/2 of `(cx, cy + ey)`, then `HubEdit`, before anything else; else within P/2 of the centre `HubClose`; the hub is never returned as a slot.
    - [ ] Given a point on the label plate, then `Plate`; inside the disc but not on a slot, then inside (not `Outside`); else `Outside`.
    - [ ] At B = 36 the sub-button reaches 2 px past the hub and wins there.
  - **Tests**: unit `HitTesterTests` (defaults, every B, priority cases).
  - **Size**: M

- [x] **T-18**: Core `PinBoard` (Fixed sizing) · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-5, FR-9, FR-14, FR-15, NFR-8
  - **Depends on**: T-3
  - **Files**: `src/QuickMenu.Core/Pins/{PinBoard,PinEntry,PinResult,IPinResolver}.cs`, `tests/QuickMenu.Core.Tests/Pins/PinBoardFixedTests.cs`
  - **Acceptance criteria**:
    - [ ] Given a key not pinned and a free slot, when `Toggle` runs, then `Pinned` into the first free slot (or the insert cursor slot). Given the key is pinned, then `Unpinned` and the slot becomes empty in place. Given all slots occupied (reserved count as occupied), then `Full` and nothing changes.
    - [ ] Given `Swap(a, b)` and `Clear(i)`, then positions change as named and other slots keep their place.
    - [ ] Given unresolved entries, then `VisibleSlots(resolver)` reports them as reserved and `Toggle` skips them when looking for a free slot.
    - [ ] Given capacity shrinks, then pins beyond it are kept (hidden) and come back when it grows.
  - **Tests**: unit `PinBoardFixedTests`.
  - **Size**: M

- [x] **T-19**: Core `PinSerializer` v1 · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-14, FR-17, NFR-6, NFR-8
  - **Depends on**: T-3
  - **Files**: `src/QuickMenu.Core/Pins/PinSerializer.cs`, `tests/QuickMenu.Core.Tests/Pins/PinSerializerTests.cs`
  - **Acceptance criteria**:
    - [ ] Given a crafted or corrupted settings file, then each pin line longer than 4 KB or a moddrop key with more than 256 children is skipped (logged once) rather than loaded, and one bad entry never fails the whole load (M1 security review).
    - [ ] Given a board, when `ToLines` then `FromLines` runs, then slot, key, packageId and label round-trip, with `|` and `\` escaped.
    - [ ] Given a line with an unknown key prefix, then it is kept verbatim and written back unchanged. Given a malformed line, then it is skipped and reported once (count returned), never thrown.
  - **Tests**: unit `PinSerializerTests`.
  - **Size**: S

- [x] **T-20**: Core `TriggerBinding`, `ModifierPolicy` and `PinClickClassifier` · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-1, FR-8, NFR-1, NFR-8 (ADR 0003, 0005)
  - **Depends on**: — (use the T-14 field results when available)
  - **Files**: `src/QuickMenu.Core/Input/{TriggerBinding,ModifierPolicy,PinModifier,PinClickClassifier}.cs`, `tests/QuickMenu.Core.Tests/Input/{TriggerBindingTests,ModifierPolicyTests,PinClickClassifierTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given `None`, `Key(code)`, `Mouse(2..4)`, then labels are "Unbound", the key name, "Middle mouse", "Mouse 4", "Mouse 5" (as Keyed key ids, not text); serialise and parse round-trip; `Mouse(0)` and `Mouse(1)` are rejected.
    - [ ] Given `pinModifierUserSet` false and Mint active, then Alt; not active, then Ctrl; user set, then the user's choice.
    - [ ] Given plain event flags (is key, raw type, button, modifier flags, raw modifier keys), when classified with the effective modifier, then key events and non-left-button events pass through; a left click with the modifier held is `Pin`.
  - **Tests**: unit tests above.
  - **Size**: S

- [x] **T-21**: Core `TriggerStateMachine` (toggle mode) · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-2, FR-9 (enter/exit), FR-11 (back), NFR-8 (design.md close and cancel rules)
  - **Depends on**: T-17
  - **Files**: `src/QuickMenu.Core/Input/{TriggerStateMachine,TriggerEvent,MenuCommand,MenuState}.cs`, `tests/QuickMenu.Core.Tests/Input/TriggerStateMachineToggleTests.cs`
  - **Acceptance criteria**:
    - [ ] Given closed, when the trigger is pressed, then `Open`; given open, when pressed again, then `Close`.
    - [ ] Given the main level, then Esc, right-click, left-click outside and left-click `HubClose` give `Close`; left-click (down and up) on `HubEdit` gives `EnterEdit`; a click on `Slot(i)` gives `Activate(i)` or `OpenSub(i)` per slot kind.
    - [ ] Given a satellite open, then Esc, right-click, click outside and click on its hub give `Back`.
    - [ ] Given edit mode, then Esc and `HubClose` (Done) give `ExitEdit`, a left-click outside closes (superseded by user decision 2026-10-19), and a second Esc closes. Given map lost, then `Close` in every state.
  - **Tests**: unit `TriggerStateMachineToggleTests` (one test per row of the close and cancel table).
  - **Note (2026-10-19)**: the hub rows are superseded by T-81: there is no `HubClose`, a hub click never closes, and right-click in edit mode closes.
  - **Size**: M

- [x] **T-22**: `MaskRasterizer` full shape set · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7, NFR-8 (design.md §2 components, §6)
  - **Depends on**: T-4
  - **Files**: `src/QuickMenu.Core/Render/{MaskRasterizer,MaskSet}.cs`, `tests/QuickMenu.Core.Tests/Render/MaskSetTests.cs`
  - **Acceptance criteria**:
    - [ ] Given the mask set, then it produces `Circle`, `Disc` (radial gradient α 0.85 → 0.55 as grayscale), `Ring1/2/3` at 128 px with 2.3 / 4.6 / 6.9 px strokes, `RingDashed`, and 9-slice `Panel`, `PanelRim` (radius 10) and `Plate` (radius 3) with documented slice borders.
    - [ ] Pixel tests check centre/edge alpha, stroke widths within 0.5 px, dash count and 9-slice border regions.
    - [ ] Generating the whole set takes ≤ 5 ms in the benchmark (performance.md, design.md §6).
  - **Tests**: unit `MaskSetTests`; benchmark entry added to `tests/QuickMenu.Core.Benchmarks/Program.cs` by T-24.
  - **Size**: M

- [x] **T-23**: `MaskRasterizer` badges and hub icons
  - **Owner**: visual-designer
  - **Requirements**: NFR-7, NFR-8 (design.md S4, S4a, §3)
  - **Depends on**: T-22
  - **Files**: `src/QuickMenu.Core/Render/Glyphs.cs`, `tests/QuickMenu.Core.Tests/Render/GlyphsTests.cs`
  - **Acceptance criteria**:
    - [ ] Given the glyph set, then `SubMenuBadge`, `Caret`, `Lock`, `IconClose`, `IconEdit`, `IconDone`, `IconAdd` are rasterized in their final orientation on a 64 px grid with a 4 px margin and 6 px strokes (no runtime rotation needed).
    - [ ] Each glyph has a coverage test (alpha mass in the expected region, empty margin) and is deterministic.
  - **Tests**: unit `GlyphsTests`.
  - **Size**: M

- [x] **T-24**: Perf gates v1 and benchmarks for the hot paths
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-2, P-3, P-5, P-9 Core part, P-10, P-16, P-17 pure part)
  - **Depends on**: T-16, T-17, T-18, T-19, T-20, T-21, T-22
  - **Files**: `tests/QuickMenu.Core.Tests/Perf/{ZeroAllocTests,LayoutTimingTests,PinBoardPerfTests,SerializerPerfTests}.cs`, `tests/QuickMenu.Core.Benchmarks/{LayoutBenchmarks,PinBenchmarks,MaskBenchmarks}.cs`, `tests/QuickMenu.Core.Benchmarks/Program.cs`
  - **Acceptance criteria**:
    - [ ] Given warm-up, then 0 bytes are allocated by: `PinClickClassifier` + `ModifierPolicy` (P-3), `TriggerStateMachine.Handle`, `HitTester.Hit`, and `EdgeDodger.Fit` over 10,000 calls (trait `Category=Perf`).
    - [ ] `PinBoard.Toggle` allocates ≤ 2 KB per call (P-5); `WheelLayout` + `EdgeDodger` for 40 slots fail above 5 × 50 µs (P-10); `FromLines`/`ToLines` for 200 pins fail above 5 × 5 ms (P-16/P-17).
    - [ ] BenchmarkDotNet suites compile in CI and run with `scripts/perf.sh --bench`.
  - **Tests**: `scripts/perf.sh`.
  - **Size**: M

- [x] **T-25**: Settings model and `PinService`
  - **Owner**: developer
  - **Requirements**: FR-5, FR-15, FR-17, NFR-4, NFR-6 (architecture §4; ADR 0004)
  - **Depends on**: T-6, T-14, T-18, T-19, T-20
  - **Files**: `src/QuickMenu/Settings/QuickMenuSettings.cs`, `src/QuickMenu/Pinning/PinService.cs`, `src/QuickMenu/QuickMenuMod.cs`, `mod/Languages/English/Keyed/QuickMenu.Messages.xml`
  - **Acceptance criteria**:
    - [ ] Given unknown-prefix or unparseable pin lines, then `PinSerializer.FromLines` loads them as Opaque board entries (slot kept, never resolved) and `ToLines` writes them back unchanged on every save, so a new pin never lands on them (T-19 follow-up; no separate preserved list).
    - [ ] Given `ExposeData`, then every field of architecture §4 is saved with its default, `schemaVersion` = 1, pins as `PinSerializer` lines; a missing or corrupt file loads defaults with one warning.
    - [ ] Given startup after the catalog build, then stored pins are resolved against the key map once; opening the menu does no catalog work.
    - [ ] Given `PinService.Toggle(entry)`, then the result shows the design.md S9 message with its sound (`Tick_High`, `Tick_Low`, or `ClickReject` + full text), and settings are written once.
    - [ ] Given no GameComponent, WorldComponent, MapComponent or def is added, then nothing is written into saves (code check: grep for those base types is empty).
  - **Tests**: unit coverage is in T-18/T-19; build; grep check in CI script `scripts/test.sh` stays green; in-game UC-2 (persist across restart and saves).
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [x] **T-26**: Pin by modifier+click (production prefix)
  - **Owner**: developer
  - **Requirements**: FR-8, FR-10, FR-15, NFR-1, NFR-2, NFR-6 (ADR 0005)
  - **Depends on**: T-8, T-14, T-25
  - **Files**: `src/QuickMenu/Pinning/PinClickPatch.cs`, `src/QuickMenu/QuickMenuMod.cs` (cached effective modifier)
  - **Acceptance criteria**:
    - [ ] Given `PinBoard.Toggle` returns `PinResult.Rejected` (key too large to save), then a Keyed message explains the item cannot be pinned and nothing changes (T-18/T-19 fix follow-up).
    - [ ] Given a click, then the prefix calls the classifier, then `PinClickDeduper`; on a suppressed (duplicate) frame of a pinned click it still returns `false` so the designator does not activate after being pinned (M2 Core review finding 2).
    - [ ] Given the prefix, then it follows the patch-guard convention: `if (Site.Disabled || MenuActivation.Depth > 0) return true; try { … } catch (Exception ex) { Site.Fail(ex); } return true;`, plus the re-entrancy guard for overrides that call `base.ProcessInput` (M1 security review L-4).
    - [ ] Given the effective modifier is cached at startup and on settings change, when the prefix runs, then it reads only the cached value and the event (fields chosen in T-14), never `ModsConfig`.
    - [ ] Given a left click with the modifier held on any designator (button of a dropdown included), when the prefix runs, then `PinService.Toggle(key of __instance)` runs and the prefix returns `false` (no float menu, no selection). A re-entrancy guard stops a base call from toggling twice.
    - [ ] Given a hotkey (key event) or a click without the modifier, then the original runs unchanged.
    - [ ] Given the prefix throws, then `PatchGuard` logs once, disables the prefix for the session, and the original runs.
    - [ ] *(UC-2)* Given setups (a) and (b), when the user Ctrl+clicks Wall, Mine and a dropdown button, then each is pinned with the message; Ctrl+click again unpins; the 19th pin with 2 rings shows "The quick menu is full …".
  - **Tests**: classifier unit tests (T-20); in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [x] **T-27**: Pin a single entry inside an open dropdown list (needed: S-3 confirmed)
  - **Owner**: developer
  - **Requirements**: FR-8, FR-10 (user decision U-3; ADR 0005)
  - **Depends on**: T-26
  - **Files**: `src/QuickMenu/Pinning/DropdownEntryPatch.cs`, `docs/project/architecture.md` §6 (third patch site)
  - **Acceptance criteria**:
    - [ ] Given the `GetDesignatorSelectAction` postfix replaces an entry's action, then a modifier+click on an entry inside an open dropdown never also reaches the dropdown's `ProcessInput` prefix, so only the entry is toggled, never the dropdown too (M2 Core review finding 11).
    - [ ] S-3 (2026-10-19) showed that entry clicks reach `Designator_Dropdown.ProcessInput` with the dropdown as instance and `ev.control=False`, so this patch is required. Given a postfix on `RimWorld.Designator_Dropdown.GetDesignatorSelectAction`, when the user holds the modifier and clicks an entry, then the entry's own key is toggled and the original action does not run; without the modifier, the action is unchanged. The modifier is read from `UnityEngine.Input.GetKey`, not from the event.
    - [ ] Given a quick-menu activation with a modifier held, then the pin prefix does not intercept it (`MenuActivation.Depth > 0`).
    - [ ] *(UC-2)* Ctrl+click on an entry in an open dropdown pins only that entry; Ctrl+click on the dropdown button pins the whole dropdown.
  - **Tests**: in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: S

- [x] **T-28**: Runtime textures and style tokens
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7 (design.md §2, §6; ADR 0007)
  - **Depends on**: T-14, T-22, T-23
  - **Files**: `src/QuickMenu/Menu/{Textures,QMStyle}.cs`; only if S-7 chose the PNG level: `scripts/export-masks.sh`, `mod/Textures/QuickMenu/UI/*.png`, `docs/project/assets.md`
  - **Acceptance criteria**:
    - [ ] Given the disc rim must be 2 px (design §2) at any disc size, then the rim is rasterized at the actual disc diameter via `MaskRasterizer.Ring(size, stroke)` and rebuilt with the layout cache on button-size, ring-count or UI-scale change; the stretched 128 px `Ring2` is not used for the disc rim (M2 Core review finding 5; update design.md's mask table row).
    - [ ] Given startup, then every mask is uploaded once at the level chosen in T-14 (bilinear, no mipmaps, clamp), and rebuilt only when button size or UI scale changes (≤ 5 ms), never per frame.
    - [ ] Given `QMStyle`, then every colour token of design.md §2 exists with its exact value, and `danger` uses `ColorLibrary.RedReadable` only under the stated 3:1 rule. No drawing code uses colour literals.
    - [ ] If the PNG level was chosen: the PNGs are exported by our rasterizer, committed, and each has an assets.md row (MIT, made by the project).
    - [ ] Given badges drawn at 0.18–0.25 B (≈ 8–14 px at B 36–56) and hub icons at 0.22 B / 0.6 e, then badge and hub-icon glyphs are rasterized at their actual draw size in screen px (`Glyphs.MinSize` lowered to 8, with tests) and rebuilt with the disc rim on button-size or UI-scale change within the ≤ 5 ms budget; a 64 px glyph is not minified without mipmaps (T-23 review).
  - **Tests**: build; in-game UC-2 visual review.
  - **In-game check**: UC-2 (user). Also check (T-28 review): with uploads now `SetPixelData` + `Apply(false, true)` (non-readable), the rim, slot rings and lock/caret glyphs still draw and are upright; the self-test section "textures (T-28)" shows `live` = `expected`, `Color32 size=4 … ok` and the round trip `ok`; record the "Textures rebuilt … (with upload)" time in performance.md.
  - **Size**: S

- [x] **T-29**: Wheel renderer: disc, base slot states, hub, label plate
  - **Owner**: visual-designer
  - **Requirements**: FR-4, FR-13 (look), NFR-5 (design.md S1, S4 states 1/2/3/6, S4a)
  - **Depends on**: T-16, T-17, T-28
  - **Files**: `src/QuickMenu/Menu/{MenuRenderer,SlotRenderer,HubRenderer,PlateRenderer}.cs`
  - **Acceptance criteria**:
    - [ ] Given a placement array, origin and scale, then the renderer draws disc + rim + outlines, slots, hub, label plate in that order, with no allocation per frame (labels cached per open).
    - [ ] Slot states Normal, Hover, Pressed and Empty (Fixed, use mode) match design.md S4 exactly; icons are drawn with `Command.DrawIcon` in a 0.75 B square.
    - [ ] Hub: Close main area and Edit sub-button per S4a; hover highlight at once, also on open (user decision UC-2b, 2026-10-19: the 4 px hover delay was removed); the Edit sub-button is drawn only when a flag says edit mode exists (off until T-44).
    - Note (2026-10-19): the hub drawing is superseded by T-81 (one centred pencil Edit button, design.md S4a).
    - [ ] The renderer accepts a scale for the settings preview (used in T-59).
  - **Tests**: build; visual review T-34 on UC-2 screenshots.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [x] **T-30**: Trigger binding: capture dialog, binding row, poller reads the setting
  - **Owner**: developer
  - **Requirements**: FR-1, NFR-1, NFR-4 (ADR 0003; design.md S7 binding, capture dialog)
  - **Depends on**: T-14, T-20, T-25
  - **Files**: `src/QuickMenu/Settings/{BindingCaptureWindow,BindingRow}.cs`, `src/QuickMenu/Trigger/TriggerPoller.cs`, `mod/Languages/English/Keyed/QuickMenu.Settings.xml`
  - **Acceptance criteria**:
    - [ ] Given the capture dialog, when a key is pressed (`KeyDown`) or a middle/side button (polled `GetMouseButtonDown(2..4)` in `WindowUpdate`), then it is stored; Esc cancels; left or right shows "Left and right click can't open the menu." with `ClickReject`.
    - [ ] Given the key is also a game key binding, then it is accepted and the row shows "{key} is also used for: {binding}." in `danger`.
    - [ ] Given [Clear], then the row shows "Unbound" and the poller returns early; given [Use middle mouse instead], then the binding is `Mouse(2)` and the button is hidden.
    - [ ] Given the poller, then it uses the stored binding (skeleton trigger removed) and still allocates nothing.
    - [ ] *(UC-2)* Mouse 4, Mouse 5, middle and a key each open the menu after binding; unbound opens nothing.
  - **Tests**: `TriggerBindingTests` (T-20); in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [x] **T-31**: Settings page v1
  - **Owner**: developer
  - **Requirements**: FR-6, FR-8, NFR-4, NFR-5 (design.md S7)
  - **Depends on**: T-30
  - **Files**: `src/QuickMenu/Settings/SettingsPage.cs`, `src/QuickMenu/QuickMenuMod.cs` (`DoSettingsWindowContents`, `SettingsCategory`), `mod/Languages/English/Keyed/QuickMenu.Settings.xml`
  - **Acceptance criteria**:
    - [ ] Given the mod settings window, then the left column has the binding row and note, "Pin with" (Ctrl/Alt/Shift; sets `pinModifierUserSet`), and [Clear all pins…] with a vanilla confirmation; the right column has Rings [−] n [+] "N slots" (1–5) and Button size (36–96, step 4); [Reset to defaults] does not clear pins.
    - [ ] Given a change, then the cached effective modifier and the cached layout are invalidated, and settings are written when the window closes.
    - [ ] Shape, Size, grid steppers and preview are added in T-59; trigger mode in T-64.
    - [ ] *(UC-2)* Changing rings or button size is used on the next open (FR-6).
  - **Tests**: build; in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [x] **T-32**: Production menu: controller, toggle window, plain activation
  - **Owner**: developer
  - **Requirements**: FR-2 (toggle), FR-3, FR-5 (Fixed), FR-6, FR-13, FR-17, FR-18, NFR-1, NFR-5 (ADR 0006, 0007)
  - **Depends on**: T-21, T-25, T-29, T-31
  - **Files**: `src/QuickMenu/Menu/{MenuController,QuickMenuWindow}.cs`, `src/QuickMenu/Activation/DesignatorActivator.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`; delete `SkeletonPins`
  - **Acceptance criteria**:
    - [ ] Given the state machine returns `Close` from Edit (trigger press or map lost), then the controller saves settings once, the same as on `ExitEdit` (M2 Core review finding 9).
    - [ ] Given open, then the layout comes from a cache rebuilt only on settings or UI screen size change, is edge-dodged, and the window is added in the trigger frame.
    - [ ] Given input, then every IMGUI event is mapped to a `TriggerEvent`, hit-tested by Core, and the `MenuCommand` is executed; the window holds no menu logic of its own.
    - [ ] Given `Activate(i)` on a plain designator (order or build), then `DesignatorActivator` calls `ProcessInput(Event.current)` and the window closes (stuff builds and dropdowns use the vanilla menus until T-41/T-43).
    - [ ] Given hover, then `Mouseover_Command` plays once on enter and the tooltip is the designator's own; labels and tooltips are cached per open; `Disabled` designators use the base disabled state until T-65.
    - [ ] Given middle mouse is the trigger, then camera drag while the menu is open is prevented if T-14 found it disruptive (`preventCameraMotion`).
    - [x] *(UC-2 fix, FR-14)* Given a pinned build whose designator is hidden or not in the catalog (Ideology morbid slabs), then the pin resolves with `FindAllowedDesignator(def, mustBeVisible: false)` (ad-hoc entry), or a lightweight `Designator_Build` forced greyed when the game offers none; the slot is Disabled (dimmed icon, lock badge, reason in tooltip, reject message + sound on click), never Empty. Reason selection is Core `UnavailableReason`; composition (research, required buildings, tech level) is `UnavailableReasons`. Covers part of T-40 (Ideology pins resolve while the catalog lacks the Ideology part) and part of T-65 (greyed use-mode slot, composed reasons; the picker rows, monolith/grav-engine/Biotech/precept causes stay open).
    - [ ] *(UC-2)* Given setup (a), the user opens the wheel near each corner (fully on screen), places a wall and designates mining from it, and pins are the same after a restart and in a second save.
  - **Tests**: Core tests cover logic; in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [x] **T-33**: UC-2 checklist, session guide and run
  - **Owner**: test-engineer
  - **Requirements**: FR-1, FR-3, FR-6, FR-8, FR-13, FR-15, FR-17, NFR-2, NFR-5
  - **Depends on**: T-26, T-27, T-28, T-29, T-30, T-31, T-32
  - **Files**: `docs/project/test-checklist.md` (FR checklist items for M2, section UC-2), `docs/project/test-reports/uc-2.md`
  - **Acceptance criteria**:
    - [ ] Given the checklist, then every FR acceptance criterion delivered in M2 has a numbered item with setup, steps and expected result, plus performance steps (measure closed frames; open/close 20 times; hold open with mouse sweep) and the screenshot list for T-34 (states 1, 2, 3, 6, hub hover, corners).
    - [ ] *(UC-2)* Given the user's results, then pass/fail per item is recorded and every failure has a bug entry with reproduction steps in this plan or STATUS.md.
  - **Tests**: the session itself.
  - **In-game check**: UC-2 (user)
  - **Size**: S

- [x] **T-34**: M2 visual review · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7
  - **Depends on**: T-33
  - **Files**: `docs/project/design/screens/m2/*` (user screenshots), `docs/project/design.md` (review notes)
  - **Acceptance criteria**:
    - [ ] Given the UC-2 screenshots, then each drawn state is compared with design.md S1/S4/S4a and the mockups; differences are fixed or listed as follow-ups; the user confirms the look.
  - **Tests**: screenshot review.
  - **Size**: S

- [x] **T-81**: Visual fixes from the M2 review; the hub becomes the centred Edit button; modifier+click on a slot unpins · `parallel-ok`
  - **Owner**: developer (visual-designer checks the result in UC-2b, T-82)
  - **Requirements**: FR-4, FR-9 (centre = Edit button, never closes; modifier+click on a slot unpins), FR-15, NFR-5 (design.md §1.1 hub, S4 state 6, S4a, close and cancel rules, §8 UC-2 review rows 6 and 7; user decisions 2026-10-19 after UC-2)
  - **Depends on**: T-34
  - **Files**: `src/QuickMenu.Core/Layout/{Hit,HitTester,LayoutConstants}.cs`, `src/QuickMenu.Core/Input/TriggerStateMachine.cs`, `src/QuickMenu.Core/Render/{TextureSizing,Glyphs,MaskSet}.cs`, `src/QuickMenu/Menu/{SlotRenderer,HubRenderer,PlateRenderer,WheelVisuals,QMStyle,MenuController,QuickMenuWindow}.cs`, `src/QuickMenu/Pinning/PinService.cs`, `src/QuickMenu.Core/Render/StyleTokens.cs` (if alphas are tokens there), `mod/Languages/English/Keyed/QuickMenu.Menu.xml`, and the matching tests under `tests/QuickMenu.Core.Tests/{Layout,Input,Render}/`
  - **Acceptance criteria**:
    - [ ] Given an empty Fixed slot in use mode, then it is drawn with `slotBg` at 50 % and a 1 px `slotBorder` ring at 70 % (both as named tokens, no literals); edit-mode empty and reserved slots keep `emptyBorder`.
    - [ ] Given a point in the hub area (wheel: distance ≤ P/2; grid: the centre cell), then `HitTester` returns `HubEdit`; `HitKind.HubClose`, `HasHubSub`, `HubSubDiameter`/`HubSubOffset` (`e`, `ey`) are removed with their tests; new constants `HubDiameter(B) = round(0.64·B)` and `HubGlyphSize(B) = round(0.5·HubDiameter)` are unit-tested (rounding half away from zero: B 36 → 23/12, B 56 → 36/18, B 96 → 61/31).
    - [ ] Given the main level, when the hub gets a deliberate click (down and up on it), then the state machine returns `None` while `EditEnabled` is false (the default until T-44) and the menu stays open; it never returns `Close` for a hub click. Given hold mode, when the trigger is released over the hub, then `Close` (cancel). Given a satellite, a click on its centre still gives `Back`. Given edit mode (tests drive `EditEnabled = true`), then a hub click gives `ExitEdit` and a right-click gives `CloseFromEdit` (no longer `None`). The T-21 tests are updated to these rows.
    - [ ] Given use mode, then the hub is drawn per S4a: a circle of diameter `H` at `(cx, cy)` in `slotBg` with a 1 px `accent` ring and the pencil (`IconEdit`) at `Hg` in `text`, centred at `(cx, cy)`; hover (immediate, also on open per user decision UC-2b; never while a hold trigger is held, T-63) = `slotBgHover` + 8 % white + 2 px `accent`, plate "Edit"; pressed = `slotBgPressed` + 2 px `accent`. Hub rings are rasterized at `H` (`TextureSizing.HubRing` uses `H`, not `e`). No × is drawn; `IconClose` is removed from the mask set and its tests. No tooltip yet.
    - [ ] Keyed: `QuickMenu.Menu.Close` is removed (no other use); `QuickMenu.Menu.Edit` ("Edit") stays as the hub plate label.
    - [ ] Given use mode, when the user holds the effective pin modifier (`ModifierPolicy`; read from the menu window's own click `Event`, which is not pre-used here) and left-clicks an occupied or greyed slot, then the pin is removed through `PinService` (`Tick_Low`, the `QuickMenu.Pin.Unpinned` message, settings written once), the designator is **not** activated, and in Fixed the slot becomes empty in place while the other slots keep their place. A modifier+click on an empty slot or the hub does nothing extra. Right-click never unpins. (Whether the menu stays open after an unpin: Question 8; until answered, it stays open.)
    - [ ] Given a pin was unpinned on the wheel, when the user Ctrl+clicks a designator in the architect, then it goes into the first free slot (Fixed; existing `PinBoard.Toggle`).
    - [ ] *(UC-2b)* A screenshot of a mostly empty Fixed wheel shows the filled and greyed slots reading stronger than the empty ones, and the centre reads as a pencil button; clicking it does nothing and the menu stays open; Ctrl+click on a slot unpins it with the message; the user confirms.
  - **Tests**: `HitTesterTests`/`WheelHitTestTests` (hub rows), `LayoutConstants` tests, `TriggerStateMachineToggleTests` (hub, edit and modifier-click rows), `PinBoardFixedTests` (unpin in place), mask-set tests; build; in-game UC-2b (T-82).
  - **In-game check**: UC-2b (user)
  - **Size**: M

- [x] **T-82**: UC-2b follow-up in-game check: guide and run
  - **Owner**: test-engineer (visual-designer checks the screenshot; performance-engineer reads the perf blocks in T-35)
  - **Requirements**: FR-9 (centre Edit button, modifier+click unpin), FR-14 (greyed, never empty), FR-15, NFR-1 (P-9, P-16, P-18, texture rebuild), NFR-5
  - **Depends on**: T-81 (and the T-35 instrumentation changes already in the tree: settings breakdown, `Textures.Prewarm`, P-9 sub-buckets)
  - **Files**: `docs/project/test-checklist.md` (section UC-2b), `docs/project/test-reports/uc-2b.md`, `docs/project/design/screens/m2/*` (the new screenshot)
  - **Acceptance criteria**:
    - [ ] Given the guide, then it lists the steps below with the expected result for each and the log blocks to copy, and the agent has built Release and checked `mod/1.7/Assemblies` holds only `QuickMenu.dll` and `QuickMenu.Core.dll`.
    - [ ] *(UC-2b)* Given the user's results, then pass/fail per step is recorded in `uc-2b.md`, the perf blocks are handed to T-35, the screenshot is stored and checked by the visual-designer against design.md S4 state 6 and S4a, and every failure has a bug task in this plan.
  - **Tests**: the session itself.
  - **In-game check**: UC-2b (user). Steps for the user (setup a with Ideology, dev mode on; about 15–20 min):
    1. Start the game cold (fresh launch), load a colony whose ideoligion does not allow morbid slabs; keep the two morbid slab pins from UC-2 (re-pin them in Architect → Ideology if they are gone).
    2. Open the wheel. The centre shows a pencil. Move the mouse onto it: the plate says "Edit". Click it: nothing happens and the menu stays open. Then close with Esc, right-click, a click outside and the trigger, one at a time.
    3. Morbid slab slots and one research-locked build (e.g. an unresearched power building): each is greyed with a lock, the tooltip gives the reason; clicking plays the reject sound, shows the reason, and selects nothing. No slot is empty where a pin exists.
    4. Ctrl+click a pinned slot (e.g. Wall): "unpinned" message and tick, no wall tool; the slot is empty, other slots did not move. Ctrl+click an empty slot and the centre: nothing. Ctrl+click Wall in the architect: it comes back in the first free slot.
    5. Screenshot a mostly empty wheel with the mouse over a filled slot, and one with the mouse over the centre.
    6. Debug actions → Quick Menu → **self-test**, then **perf report** (copy the "settings breakdown" and "Textures …" lines).
    7. Open the wheel once (first open after start: no "Textures rebuilt" line expected). Change Button size in settings, open again (one rebuild line with the create/upload split).
    8. Open the wheel, sweep the mouse across all slots for 30 s, close; run **perf report** again (window stat with the hover-sound, `TipRegion` and label-plate sub-buckets).
    9. Optional (setup b): repeat step 4 once with a BAM-only pin.
    10. Copy every `[QuickMenu] BEGIN … END` block and send it back with the screenshots.
  - **Size**: S

- [x] **T-35**: M2 performance record and the UC-2 follow-up fixes · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-1, P-2, P-5, P-6, P-7a, P-8, P-9 with control run, P-16, P-18, texture rebuild)
  - **Depends on**: T-33, T-82
  - **Files**: `docs/project/performance.md` §5; the instrumentation and fixes (already in the tree before UC-2b): `src/QuickMenu/Settings/QuickMenuSettings.cs`, `src/QuickMenu/Menu/Textures.cs`, `src/QuickMenu/Diagnostics/{PerfCounters,SelfTest}.cs`
  - **Acceptance criteria**:
    - [ ] Given the UC-2 numbers, then each metric has a row and a pass/fail at 50 % of budget.
    - [ ] Given the UC-2 failures (P-16 92.6 ms, texture rebuild 6.46 ms, P-9 p99 1,912 B), then: the settings load is split ("settings breakdown": `GetSettings` total, scribe pass, our `PostLoad`, our code after, JIT ms and methods); the per-value-type generic helpers in `QuickMenuSettings` are replaced by non-generic ones; sized textures are prewarmed at startup from the saved settings (`Textures.Prewarm`) and a later rebuild logs its create/upload share; the window stat has dev-only sub-buckets (hover sound, `TipRegion`, label plate). Build, `scripts/test.sh` and `scripts/perf.sh` pass; nothing allocates per frame when dev mode is off.
    - [ ] *(UC-2b)* Given the re-measure, then P-16 is judged on our part (PostLoad + after) against 10 ms, the first open after start rebuilds no textures when the sizes match, the settings-change rebuild time is recorded, and the P-9 p99 is attributed to a sub-bucket. Any metric that still fails becomes a `perf:` task in M3 or a documented user decision.
  - **Tests**: `scripts/perf.sh`; in-game UC-2b numbers.
  - **In-game check**: UC-2b (user, via T-82)
  - **Size**: M

### M3: Sub-menus, materials, edit mode shell

- [x] **T-83**: Align design.md with the picker and insert-cursor removal · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-7 (Won't), FR-9, FR-16, NFR-4 (user decision 2026-10-19 after UC-2; spec FR-7/FR-9 and §7)
  - **Depends on**: —
  - **Files**: `docs/project/design.md`
  - **Acceptance criteria**:
    - [ ] Given design.md, then S6 (picker window), S4 state 7 (Empty/Add "+"), state 10 (insert cursor), the S5 Add rows, "Add placement" and the picker status line, the flow-diagram edges to the picker, the "Click to add here." tooltip, the "Add" plate, the picker row component, the caret badge (10:30) and the picker keyboard sentence are removed or replaced by one dated removal note; the mockup reference to the picker half is marked obsolete.
    - [ ] Given edit mode, then S5 says: new pins come only from the architect menu and go into the first free slot (Fixed) or at the end (Auto); the user rearranges by dragging (Fixed: dropping on an empty slot moves the pin there); a plain click on a slot does nothing; empty slots in edit mode use `emptyBorder` with no glyph; the help-panel texts for Fixed and Auto are written out (with the modifier as {0}); the Done/Edit tooltip text drops "add".
    - [ ] Given use mode, then the "Use mode and the modifier" rule and the input table row say modifier+click on a slot unpins it (T-81), and the empty-menu hint (FR-16) explains Ctrl/Alt+click in the architect.
    - [ ] `grep -niE 'picker|insert cursor|IconAdd|"\+" slot' docs/project/design.md` matches only the removal note.
  - **Tests**: the grep check; read-through by the orchestrator.
  - **Size**: S

- [x] **T-84**: Retire the picker and search budgets in performance.md · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-11a, P-11b, P-11c, P-13, P-14, P-15)
  - **Depends on**: —
  - **Files**: `docs/project/performance.md`, `tests/Shared/SyntheticCatalog.cs` (doc comment only: the search catalog generator will not be added)
  - **Acceptance criteria**:
    - [ ] Given §1, then P-11a, P-11b (Core search index build), P-13 (catalog plus index memory), P-14 (search per keystroke) and P-15 (picker draw) are marked "Retired 2026-10-19: picker removed by the user" (rows kept, not deleted), and P-11c is redefined as the in-game catalog build only (no index) with its 200 ms NFR limit and the UC-1 baseline unchanged.
    - [ ] Given §2–§4 and the risks, then W-1x, scenario S-6 (picker queries), the search zero-alloc gate, R-8 and the open review items about the index (T-2, T-5, T-7) are closed with the same note; nothing else changes.
  - **Tests**: read-through; `scripts/perf.sh` still passes (no gate depended on the index).
  - **Size**: S

- [x] **T-85**: Align architecture.md and the ADRs with the picker removal · `parallel-ok`
  - **Owner**: architect
  - **Requirements**: FR-7 (Won't), FR-9, FR-14, NFR-1, NFR-8
  - **Depends on**: —
  - **Files**: `docs/project/architecture.md`, `docs/project/decisions/0006-designator-catalog-and-activation.md` (dated note only), any other ADR that names the picker (dated note only)
  - **Acceptance criteria**:
    - [ ] Given architecture.md, then `Core.Search.SearchIndex`, `UI.PickerWindow`, `PinBoard.SetCursor`/insert cursor, the index build in the startup sequence and §7, the 20k-entry index perf gate, the `SearchEntry` synthetic data line and the "picker via + slots" wording in the window row and traceability are removed; FR-7 is listed as Won't; FR-9 names centre Edit/Done, drag swap and modifier+click unpin (use and edit mode).
    - [ ] Given the S-1 risk row ("the picker always works as a fallback"), then the mitigation is rewritten: there is no fallback, so the self-test patch count and the per-method failure log are the safety net, and an unpatched override is reported as a known limit (see Risks in this plan).
    - [ ] ADRs keep their decisions; each one that mentions the picker gets a dated note pointing to the spec change.
  - **Tests**: `grep -niE 'picker|SearchIndex|insert cursor' docs/project/architecture.md` matches only the removal notes.
  - **Size**: S

- [x] **T-36**: Core `SubMenuLayout` (satellite and sub-grid) · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-11, FR-12, FR-4, FR-3, NFR-8 (design.md §1.1 sub-menu placement)
  - **Depends on**: T-16
  - **Files**: `src/QuickMenu.Core/Layout/SubMenuLayout.cs`, `tests/QuickMenu.Core.Tests/Layout/SubMenuLayoutTests.cs`
  - **Acceptance criteria**:
    - [ ] Given n options around a parent slot, then the satellite uses the smallest m with Σ cap ≥ n, inner rings full, the outer ring spread evenly, `D_s = m·P + B/2 + rimPad`, and it is edge-dodged on its own with the hub moving with it.
    - [ ] Given the grid, then `scols = min(ceil(√n), 6)`, `srows = ceil(n/scols)`, side right if `px ≥ cx`, first row centre y = py, the other side if the first overflows, then EdgeDodge.
    - [ ] Given a material ring from a sub-menu option, then it replaces the sub-ring at the original anchor; n = 1 returns "skip", n = 0 returns "none".
  - **Tests**: unit `SubMenuLayoutTests` (counts 1…60, all screen edges, both sides).
  - **Size**: M

- [x] **T-37**: Core `StuffOrdering` and `ModDropdownMatcher` · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-10, FR-12, FR-14, NFR-8 (ADR 0004, 0006)
  - **Depends on**: T-3
  - **Files**: `src/QuickMenu.Core/Materials/StuffOrdering.cs`, `src/QuickMenu.Core/Pins/ModDropdownMatcher.cs`, `tests/QuickMenu.Core.Tests/{Materials/StuffOrderingTests,Pins/ModDropdownMatcherTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given options and a last-used id, then last-used is index 0 and the rest keep the input order; 1 option → skip; 0 → none.
    - [ ] Given a stored `moddrop` key and candidates, then only the same type matches, Jaccard of child keys ≥ 0.5, same category breaks ties, then the first; no match → none.
  - **Tests**: unit tests above.
  - **Size**: S

- [-] **T-38**: Core `SearchIndex` (removed: picker dropped by user 2026-10-19)

- [-] **T-39**: Synthetic catalog, search perf gates and benchmarks for P-11a, P-11b, P-13, P-14 (removed: picker dropped by user 2026-10-19; budgets retired in T-84)

- [x] **T-40**: Catalog v2: Ideology part and refresh, mod-dropdown resolve
  - **Owner**: developer
  - **Requirements**: FR-10, FR-14, NFR-1 (P-7c, P-11c), NFR-2 (ADR 0006)
  - **Depends on**: T-14, T-37
  - **Files**: `src/QuickMenu/Catalog/{DesignatorCatalog,CatalogEntry,IdeoSignature}.cs`, `src/QuickMenu/Pinning/PinService.cs`, `src/QuickMenu/Diagnostics/SelfTest.cs`
  - **Acceptance criteria**:
    - [ ] Given the player's ideoligion changes during play (e.g. reform adopts a precept), then the Ideology refresh calls `DesignatorCatalog.NotifyChanged()` so forced-unavailable (greyed) pins re-resolve without reloading the save (T-81 review finding 4).
    - [ ] Given a loaded game with Ideology, then the catalog adds the Ideology category's designators (excluded in T-6) with their keys, so a pinned Ideology build (e.g. morbid slabs) resolves to its catalog entry; the T-32 ad-hoc fallback stays only for designators the game does not offer.
    - [ ] Given the Ideology part of the catalog is refreshed (or the catalog is rebuilt), then `PinService` re-resolves stored pins, so an Ideology pin resolved as missing at startup becomes available once the game with that ideoligion loads (T-25 review M1).
    - [ ] Given a menu open, then the signature (game, primary ideo, precept count) is compared in O(1) and the Ideology entries are rebuilt only when it changed.
    - [ ] Given a `moddrop:` key with no exact match, then `ModDropdownMatcher` resolves it; the stored key is not rewritten.
    - [ ] Given the startup build, then the self-test reports its total ms and the number of Ideology rebuilds; no search index or dev-only flag is built.
    - [ ] *(UC-3)* The self-test on (R) and the stress mod shows build ≤ 100 ms (50 % of 200 ms) and 1 build per session.
  - **Tests**: Core tests (T-37); in-game UC-3 self-test.
  - **In-game check**: UC-3 (user)
  - **Size**: M

- [x] **T-41**: `StuffOptions` and full `DesignatorActivator`
  - **Owner**: developer
  - **Requirements**: FR-12, FR-13, FR-18 (ADR 0006)
  - **Depends on**: T-32, T-37
  - **Files**: `src/QuickMenu/Catalog/StuffOptions.cs`, `src/QuickMenu/Activation/DesignatorActivator.cs`
  - **Acceptance criteria**:
    - [ ] Given a stuff build, then the options are the counted stuffs on the map that `IsStuff` and `CanMake` the def (all in god mode), in the architect's fixed order (`StuffOrdering` never re-sorts); the designator's current stuff is only reported as the last-used index (highlight).
    - [ ] Given `Activate(entry, stuff)`, then `SetStuffDef(stuff)`, the interaction check, `CurActivateSound`, `DesignatorManager.Select`; Ideology style variants keep the game's default style.
    - [ ] Known gap from T-7: the architect fires on mouse-up over the same slot and never activates `Disabled` designators; the T-7 skeleton fires on mouse-down. Align here.
    - [ ] Given a dropdown element, then it is activated the same way and `SetActiveDesignator` is called on the dropdown so the architect shows it as last used.
    - [ ] Given no options, then the slot reports "none" with the vanilla "NoStuffsToBuildWith" text.
    - [ ] The activator calls no `Designate*` method itself (FR-18).
  - **Tests**: `StuffOrderingTests`; in-game UC-3.
  - **In-game check**: UC-3 (user)
  - **Size**: M

- [x] **T-42**: Slot states for sub-menus, materials, greyed, open parent; dimmed menu · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-10, FR-11, FR-12, FR-14 (look) (design.md S3, S4 states 4, 5, 12, 13, 14)
  - **Depends on**: T-29
  - **Files**: `src/QuickMenu/Menu/{SlotRenderer,SatelliteRenderer}.cs`
  - **Acceptance criteria**:
    - [ ] States 4 (last-used), 5 (greyed with lock), 12 (sub-menu badge at 1:30), 13 (material dot at 4:30 in the last-used stuff colour or `textMuted`) and 14 (open parent) are drawn per S4 and combine as stated; material options show the stuff icon in its colour.
    - [ ] The main menu draws at 45 % opacity while a satellite is open.
  - **Tests**: build; visual review T-48.
  - **In-game check**: UC-3 (user)
  - **Size**: S

- [x] **T-43**: Sub-ring and material ring in the window
  - **Owner**: developer
  - **Requirements**: FR-10, FR-11, FR-12, FR-13 (design.md S3)
  - **Depends on**: T-36, T-41, T-42
  - **Files**: `src/QuickMenu/Menu/{QuickMenuWindow,MenuController,SatelliteController}.cs`
  - **Acceptance criteria**:
    - [ ] Given a pinned dropdown slot (`dropdown:` or `moddrop:`), when it is clicked, then its options fan out as a satellite with the parent as its hub, `Tick_Tiny` plays, and any option can be activated.
    - [ ] Given a stuff build slot or option, then 2+ materials open the material ring (last-used first, highlighted), 1 activates directly, 0 greys the slot with the "no materials" tooltip; choosing a material activates with that stuff.
    - [ ] Given a satellite, then Esc, right-click, click on its hub or outside goes back one level (T-21 commands).
    - [ ] *(UC-3)* Journey 4: chairs dropdown → armchair → wood → designator ready to place, menu closed.
  - **Tests**: `SubMenuLayoutTests`, `TriggerStateMachineToggleTests`; in-game UC-3.
  - **In-game check**: UC-3 (user)
  - **Size**: M

- [x] **T-44**: Edit mode shell: enter, Done, Esc, modifier+click unpin
  - **Owner**: developer
  - **Requirements**: FR-9 (enter/exit, unpin in edit mode) (design.md S4a; S5 as revised by T-83)
  - **Depends on**: T-32, T-81, T-83
  - **Files**: `src/QuickMenu/Menu/{MenuController,QuickMenuWindow,HubRenderer,SlotRenderer}.cs`, `src/QuickMenu.Core/Input/TriggerStateMachine.cs` (the edit logic lives in the state machine and `MenuController`; there is no `EditModeController.cs`), `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] `EditEnabled` is set to true. Given use mode, when the hub gets a deliberate click (down and up on it), then edit mode starts and the hub shows Done per S4a (`slotBg` + 22 % `accent` tint, 2 px `accent` ring, `IconDone` at `Hg`; hover/pressed 3 px ring), plate "Done"; a hold-mode release over the hub never enters edit mode; satellites do not open in edit mode.
    - [ ] Given edit mode, then Done or the first Esc leaves it (settings written once if dirty), a second Esc closes, right-click, a left-click outside or the trigger closes the menu (user decision 2026-10-19).
    - [ ] Given edit mode, then a plain click on any slot does nothing (no activation, no sub-menu, no cursor); empty slots look the same in edit mode as in use mode (no extra border or glyph); there is no "+" and no Add target anywhere.
    - [ ] Given edit mode, when the user modifier+clicks an occupied or reserved slot, then it is unpinned by the same path as T-81 but without a settings write per pin (`PinService.Toggle(..., persist: false)` plus a dirty flag; one write when the session ends, only if dirty) (Fixed: the slot is empty in place; Auto, where later pins move up, is wired in T-56/T-57); on an empty slot or the hub nothing extra happens; right-click never unpins.
    - [ ] Keyed strings added: `QuickMenu.Menu.Done` "Done"; `QuickMenu.Menu.EditTip` "Edit the quick menu: move and remove pins."; `QuickMenu.Menu.DoneTip` "Finish editing (Esc)". Hub tooltips use the vanilla delay. No `QuickMenu.Menu.Add`/`AddTip` keys exist.
  - **Tests**: `TriggerStateMachineToggleTests` (edit rows with `EditEnabled`, modifier-click rows); in-game UC-3.
  - **In-game check**: UC-3 (user)
  - **Size**: S

- [-] **T-45**: Picker window (removed: picker dropped by user 2026-10-19)

- [x] **T-46**: In-game stress mod generator · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-11c, W-1, W-2)
  - **Depends on**: —
  - **Files**: `scripts/make-stress-mod.sh`, `.gitignore` (output folder)
  - **Acceptance criteria**:
    - [ ] Given `scripts/make-stress-mod.sh N`, then it writes a test-only mod (gitignored, outside `mod/`) with N buildable `ThingDef`s (some stuff-made, some in dropdown groups) over vanilla and 3 fake `DesignationCategoryDef`s, and an About.xml with packageId `sixdd.QuickMenuStress`; it is never packaged.
  - **Tests**: script run in CI-like check (XML well-formed via `xmllint` if present, else a `dotnet` one-liner test).
  - **Size**: S

- [x] **T-47**: UC-3 checklist, session guide and run
  - **Owner**: test-engineer
  - **Requirements**: FR-9, FR-10, FR-11, FR-12, FR-13, FR-14, NFR-2
  - **Depends on**: T-40, T-43, T-44, T-46
  - **Files**: `docs/project/test-checklist.md` (M3 items, section UC-3), `docs/project/test-reports/uc-3.md`
  - **Acceptance criteria**:
    - [ ] *(UC-3)* Carry-over M2 checks (M2 test review): Mouse 5 / middle mouse / a keyboard key open the menu; Clear leaves it unbound; capture dialog rejects left/right click with the message; key-conflict warning; "Use middle mouse instead"; Pin with Ctrl/Alt/Shift; Clear all pins (with confirmation); Reset to defaults keeps pins; rings and button size apply on next open; the full-menu message at the capacity limit; hotkey and plain clicks unaffected by the pin prefix; middle-mouse trigger blocks camera drag; hub Edit label shows immediately on open (3fe81c5).
    - [ ] Checklist items cover every M3 acceptance criterion on (a) and (b); journey 2 (Ctrl+click the chair dropdown in the architect with BAM, it fills the first free slot as a whole sub-menu) and journey 4; an Ideology designator pinned before and after loading a game whose ideoligion allows it (refresh, setup a with Ideology); the edit mode shell (enter, Done, Esc twice, right-click closes, plain click does nothing, modifier+click unpins); the self-test on (R) and on the stress mod (2,000 and 8,000 defs); and the measurement actions for P-7c, P-8 with a sub-ring, P-11c and P-12.
    - [ ] *(UC-3)* Results recorded; failures become bug tasks.
  - **Tests**: the session itself.
  - **In-game check**: UC-3 (user)
  - **Size**: S

- [x] **T-48**: M3 visual review · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-7, NFR-5
  - **Depends on**: T-47
  - **Files**: `docs/project/design/screens/m3/*`, `docs/project/design.md`
  - **Acceptance criteria**:
    - [ ] States 4, 5, 12, 13, 14, satellites, the material ring and the edit-mode hub (Done) match design.md and the mockups; the user confirms.
    - [ ] Carried over from T-34: pressed (3), hub hover, edge-dodge corners and the rim over snow or ice are captured and checked (the T-81 look was checked in UC-2b, T-82).
  - **Result (2026-10-19)**: reviewed on `design/spikes/uc3-material-ring.png` (design.md §8 "UC-3 visual review"): satellite geometry, hover, open parent (14), material dot (13) and plate pass; one change → T-86. States 4 (last-used alone), 5, 12, edit-mode hub, and the T-34 carry-overs were not captured and moved to UC-4 (T-60) / T-61.
  - **Tests**: screenshot review.
  - **Size**: S

- [x] **T-49**: M3 performance record · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-7c, P-8 with sub-ring, P-11c, P-12)
  - **Depends on**: T-47
  - **Files**: `docs/project/performance.md` §5
  - **Acceptance criteria**:
    - [ ] Each metric recorded with W-0/W-1/W-2 and (R) counts, pass/fail at 50 % of budget; failures become `perf:` tasks before M4 ends.
  - **Tests**: none.
  - **Size**: S

### M4: Edit mode, grid, Auto sizing

- [x] **T-50**: Core `GridLayout` and grid hit testing · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-4, FR-6, NFR-8 (design.md §1.1 grid)
  - **Depends on**: T-17
  - **Files**: `src/QuickMenu.Core/Layout/{GridLayout,GridParams}.cs`, `src/QuickMenu.Core/Layout/HitTester.cs`, `tests/QuickMenu.Core.Tests/Layout/{GridLayoutTests,HitTesterGridTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given 5 × 3 at B = 56, then the panel is 336 × 208, the hub is cell (1, 2), 14 slots in reading order skipping the hub; odd-only cols {3…11} and rows {1…7} (Fixed).
    - [ ] Given a point, then a slot is hit inside its P × P cell; the whole centre cell is `HubEdit` (one hub target, no sub-button); panel and plate count as inside.
  - **Tests**: unit tests above.
  - **Size**: M

- [x] **T-51**: Core `Capacity` (Auto growth and full) for wheel and grid
  - **Owner**: developer
  - **Requirements**: FR-5, FR-6, NFR-5, NFR-8
  - **Depends on**: T-16, T-50
  - **Files**: `src/QuickMenu.Core/Layout/Capacity.cs`, `src/QuickMenu.Core/Layout/WheelLayout.cs` (Auto mode), `tests/QuickMenu.Core.Tests/Layout/CapacityTests.cs`
  - **Acceptance criteria**:
    - [ ] Given Auto wheel with count c, then the smallest m with Σ cap ≥ c is used, inner rings full, outer ring spread evenly; c = 0 draws ring-1 geometry; edit mode adds no extra slot (there is no append/Add slot).
    - [ ] Given Auto wheel, then full when count = Σ cap(m) and m + 1 rings do not fit the screen minus margins, or m = 8. Given Auto grid, rows = smallest odd with rows·cols − 1 ≥ count; full when rows + 2 do not fit or rows = 9.
    - [ ] Given Fixed, then capacity = rings or rows·cols − 1.
  - **Tests**: unit `CapacityTests` (UI screen 1920 × 1080 and 960 × 540).
  - **Size**: M

- [x] **T-52**: Core `PinBoard` Auto semantics, mode conversion, forget missing; remove the insert cursor · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-5, FR-9, FR-14, FR-15, NFR-8 (architecture §4, U-2)
  - **Depends on**: T-18
  - **Files**: `src/QuickMenu.Core/Pins/PinBoard.cs`, `tests/QuickMenu.Core.Tests/Pins/{PinBoardAutoTests,PinBoardFixedTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given Auto, then pins are an ordered list, unresolved ones are hidden but keep their order, a new pin is appended at the end, and `Clear(i)` moves later pins up. Given Fixed, a new pin goes into the first free slot (unchanged from T-18).
    - [ ] Given the insert cursor was dropped (user, 2026-10-19), then `PinBoard.Cursor` and `SetCursor` and every cursor branch in `Toggle` are removed with their tests; no caller remains (build and `scripts/test.sh` green, coverage ≥ 90 %).
    - [ ] Given `Swap(a, b)` in Auto (drag in edit mode), then the two pins exchange places in the list and the others keep their order; given `Swap(a, b)` in Fixed with b empty, then the pin moves to b and a becomes empty.
    - [ ] Given Fixed ↔ Auto conversion, then order is preserved and no pin is lost.
    - [ ] Given `CountUnresolved` and `ForgetUnresolved`, then only unresolved pins are counted and removed.
  - **Tests**: unit `PinBoardAutoTests`.
  - **Size**: M

- [x] **T-53**: Perf gates v2: grid, Auto and sub-menu layout
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-9 Core part, P-10)
  - **Depends on**: T-36, T-50, T-51
  - **Files**: `tests/QuickMenu.Core.Tests/Perf/{ZeroAllocTests,LayoutTimingTests}.cs`, `tests/QuickMenu.Core.Benchmarks/LayoutBenchmarks.cs`
  - **Acceptance criteria**:
    - [ ] Grid hit test allocates 0 bytes; grid/Auto/sub-menu layout for 40 slots + one sub-ring fails above 5 × 50 µs; benchmarks added.
  - **Tests**: `scripts/perf.sh`.
  - **Size**: S

- [x] **T-54**: Grid panel and sub-grid renderer · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-4, FR-11 (look) (design.md S2, §2 grid panel)
  - **Depends on**: T-29, T-42
  - **Files**: `src/QuickMenu/Menu/{GridRenderer,PanelRenderer}.cs`
  - **Acceptance criteria**:
    - [ ] Given a grid layout, then the 9-slice panel and rim (radius 10), slots, hub in the centre cell and the label plate are drawn using the same slot and hub routines as the wheel; sub-grids and the material grid use the same panel; the main panel dims to 45 % while one is open.
  - **Tests**: build; visual review T-61.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [x] **T-55**: Edit-mode states, help panel and hint plate renderer; drop the Add and caret glyphs · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-9, FR-14, FR-16 (look) (design.md S4 states 8, 9, 11, S5, as revised by T-83)
  - **Depends on**: T-29, T-81, T-83
  - **Files**: `src/QuickMenu/Menu/{EditStateRenderer,HelpPanelRenderer,Textures}.cs`, `src/QuickMenu.Core/Render/{Glyphs,MaskSet,TextureSizing}.cs`, `tests/QuickMenu.Core.Tests/Render/{GlyphsTests,MaskSetTests}.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] States 8 (reserved-missing "?", tooltip ending "{modifier}+click to remove."), 9 (drag source ghost and dragged icon at 0.9 B, 80 %, offset +16) and 11 (drop target, an empty Fixed slot included) are drawn per S4. There is no state 7 ("+") and no state 10 (insert cursor).
    - [ ] Given the "+" and the insert cursor are gone, then `MaskId.IconAdd` and `MaskId.Caret` are removed from the mask set, `Glyphs`, `TextureSizing` and `Textures` with their tests; the glyph range checks still cover the remaining glyphs; the T-28 texture budget tests stay green.
    - [ ] The help panel is at top centre (60 px down, max 520 wide) with the Fixed or Auto text and the capacity line (`danger` when full); the hint plate wraps at 360.
    - [ ] Keyed strings (design.md §1.4, S4, Tooltips, texts per T-83): `QuickMenu.Menu.EditHelpFixed`, `QuickMenu.Menu.EditHelpAuto` (both take the modifier name as {0}, say that new pins come from the architect menu and go into the first free slot or at the end, no right-click and no "add" wording), `QuickMenu.Menu.EditSlotTip` "Drag to move. {0}+click to remove.", `QuickMenu.Menu.MissingTip` (state 8, label {0}, mod {1}, modifier {2}), `QuickMenu.Menu.CapacityFixed` "{0} of {1} slots used", `QuickMenu.Menu.CapacityAuto` "{0} pinned", `QuickMenu.Menu.Full` "The menu is full.".
  - **Tests**: build; visual review T-61.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [x] **T-56**: Grid layout and Auto sizing in the window
  - **Owner**: developer
  - **Requirements**: FR-4, FR-5, FR-11, FR-12, FR-6 (design.md S2, S3)
  - **Depends on**: T-43, T-51, T-54
  - **Files**: `src/QuickMenu/Menu/{MenuController,QuickMenuWindow,SatelliteController}.cs`
  - **Acceptance criteria**:
    - [ ] Given layout = Grid, then the menu, sub-grids and material grids use `GridLayout`/`SubMenuLayout` and every feature that works in the wheel works in the grid (FR-4).
    - [ ] Given sizing = Auto, then the menu grows by ring or row as pins are added and pinning reports full per `Capacity`.
    - [ ] *(UC-4)* Every M2/M3 checklist item passes again with Grid and with Auto.
  - **Tests**: Core layout tests; in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [x] **T-57**: Edit interactions: drag swap, help panel, full, one write per session (wheel and grid, Fixed and Auto)
  - **Owner**: developer
  - **Requirements**: FR-9, FR-5, FR-4, FR-14, NFR-1 (P-17 write count)
  - **Depends on**: T-44, T-52, T-55, T-56
  - **Files**: `src/QuickMenu/Menu/{EditModeController,QuickMenuWindow}.cs`, `src/QuickMenu/Pinning/PinService.cs`
  - **Acceptance criteria**:
    - [ ] Given edit mode, when slot A (occupied or reserved) is dragged (start after 6 px) onto slot B, then `PinBoard.Swap(A, B)` runs with `Tick_Tiny` (Fixed: B may be empty, so the pin moves there; Auto: the two pins exchange places); a press and release under 6 px does nothing; dropping on the hub or outside cancels the drag.
    - [ ] Given a right-click, then it never unpins: it cancels a drag in progress, otherwise closes the menu.
    - [ ] Given modifier+click on a slot (T-81 in use mode, T-44 in edit mode), then it also works in the grid and in Auto (Auto: later pins move up).
    - [ ] Given the help panel, then it shows the right text and capacity line; when full, pinning by Ctrl/Alt+click in the architect plays `ClickReject` with the full message (T-26 path, unchanged).
    - [ ] Given pins made in the architect while the menu is closed, then they appear in the first free slot (Fixed) or at the end (Auto) on the next open.
    - [ ] Given an edit session, then settings are written once at edit end or menu close if dirty, never per drag step (counter in the self-test).
    - [ ] *(UC-4)* Journey 5 in wheel and grid (with modifier+click to clear a slot, per FR-9; see Question 9).
  - **Tests**: `PinBoardFixedTests`, `PinBoardAutoTests`; in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [x] **T-58**: Empty-menu state
  - **Owner**: developer
  - **Requirements**: FR-16, FR-1 (journey 1)
  - **Depends on**: T-55, T-56
  - **Files**: `src/QuickMenu/Menu/QuickMenuWindow.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] Given no visible pins and not in edit mode, when the menu opens, then the label plate is replaced by the hint with the effective modifier name; Fixed draws its empty slots, Auto a ring-1 disc (or 1-row panel); the hub with Edit is shown.
    - [ ] *(UC-4)* Journey 1 after "Clear all pins".
  - **Tests**: in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: S

- [x] **T-59**: Settings page v2: shape, size, grid, preview, missing pins
  - **Owner**: developer
  - **Requirements**: FR-4, FR-5, FR-6, FR-14, NFR-5 (design.md S7 right column)
  - **Depends on**: T-31, T-51, T-52, T-54
  - **Files**: `src/QuickMenu/Settings/{SettingsPage,LayoutPreview}.cs`, `mod/Languages/English/Keyed/QuickMenu.Settings.xml`
  - **Acceptance criteria**:
    - [ ] Given `PinService.IsReady` is false (catalog not built, empty or failed), then the missing-pin count and the "Forget missing pins" button are disabled; and given it is true, then a pin counts as missing (`PinService.IsMissing`/`CountMissing`/`ForgetMissing`) only when it is unresolved and (a build pin's def is absent from the DefDatabase, or its stored packageId is not an active mod), so an unresolved but present pin (main menu, no game) is never forgotten (T-25 review M2, round 2).
    - [ ] Shape [Wheel][Grid], Size Fixed/Auto with tooltips, Rings or Columns/Rows steppers (odd steps, hidden in Auto except Columns), slot count text.
    - [ ] Preview 410 × 270 at 50 % from Core, updated live, with the "bigger than the screen" warning and the Fixed shrink note.
    - [ ] Given unresolved pins, then the count text and [Forget missing pins] with a confirmation; on confirm, `ForgetUnresolved` and one write.
    - [ ] *(UC-4)* Each setting change shows in the preview and on the next open; at UI scale 2.0 the 4-ring warning appears and the menu is centred.
  - **Tests**: `CapacityTests`; in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [x] **T-60**: UC-4 checklist, session guide and run
  - **Owner**: test-engineer
  - **Requirements**: FR-4, FR-5, FR-6, FR-9, FR-16, NFR-5
  - **Depends on**: T-56, T-57, T-58, T-59
  - **Files**: `docs/project/test-checklist.md` (M4, UC-4), `docs/project/test-reports/uc-4.md`
  - **Acceptance criteria**:
    - [ ] *(UC-4)* Carry-over M3 checks (M3 test review): material ring with 1 material skips the ring, 0 materials → greyed with "no materials"; journey 4 (chair dropdown → armchair → wood → ready to place) if a stuff dropdown exists; satellites at UI scale 2.0 (or the max allowed); grid sub-grid and material ring; pressed state, hub hover, edge-dodge corners, rim over snow (T-34 carry-over); last-used material (state 4) captured with the mouse away from it, after building the stuff build once with a chosen material, so the 3 px ring and the stuff-coloured parent dot are both visible (T-48 finding 6); T-86 underlay check.
    - [ ] *(UC-4)* Carry-over perf re-measures from M3 (T-49): P-7c Ideology refresh time on a colony with an ideoligion; P-9 control run in the stress setup; sub-ring per-frame cost with a satellite open; satellite rim rebuild (3.1 ms, watch item vs 5 ms).
    - [ ] Items for every M4 criterion, the FR-4 matrix (each feature × wheel/grid), UI scale 1.0 and 2.0 screenshots of every S4 state in both layouts, edit-session write counter, and the perf steps for P-8 (grid, max size) and P-9.
    - [ ] *(UC-4)* Results recorded; failures become bug tasks.
  - **Tests**: the session itself.
  - **In-game check**: UC-4 (user)
  - **Size**: S

- [-] **T-86**: Satellite underlay so the dimmed main menu does not show through satellites (M3 visual review, T-48 finding 4) · `parallel-ok` (removed: user keeps the see-through satellite, 2026-10-19)
  - **Owner**: developer (visual-designer checks it in UC-4 / T-61)
  - **Requirements**: NFR-7, FR-11, FR-12 (design.md §2 `satelliteUnderlay`, S1 draw order, §8 UC-3 visual review)
  - **Depends on**: T-48
  - **Files**: `src/QuickMenu.Core/Render/StyleTokens.cs`, the satellite drawing path in `src/QuickMenu/Menu/` (renderer that draws the satellite disc), `tests/QuickMenu.Core.Tests/Render/` (token test)
  - **Acceptance criteria**:
    - [ ] Given the token set, then `StyleTokens.SatelliteUnderlayAlpha` = 0.55 (black), and `DimmedMenuAlpha` stays 0.45 (user decision: the map showing through the dimmed main wheel is wanted).
    - [ ] Given an open wheel satellite, then before its disc the `Circle` mask is drawn once at diameter `2·D_s − 4`, centred on the satellite centre, tinted `satelliteUnderlay`; no new texture, no per-frame allocation.
    - [ ] Given an open grid sub-grid (when T-54 lands), then the same underlay is drawn with the `Panel` 9-slice at the sub-panel rect.
    - [ ] *(UC-4)* With a 3-option material ring over a 2-ring wheel, the main hub pencil and main slot outlines are no longer visible inside the satellite; outside the satellite the dimmed main looks unchanged.
  - **Tests**: token unit test; build; in-game UC-4 screenshot (T-61).
  - **Size**: XS

- [x] **T-61**: M4 visual review (UI scale 1.0 and 2.0) · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7
  - **Depends on**: T-60
  - **Files**: `docs/project/design/screens/m4/*`, `docs/project/design.md`
  - **Acceptance criteria**:
    - [ ] Every S4 state in wheel and grid at both UI scales matches design.md; masks are sharp at B = 96 and UI scale 2; the user confirms.
  - **Result (2026-10-19)**: reviewed on `design/spikes/uc4-material-ring-lastused.png` and `uc4-settings-page.png` (design.md §8 "UC-4 visual review"). The last-used highlight (state 4, 3 px) is clear and stays distinct from hover. The material dot shows the stuff colour. The settings page matches S7 (preview 410 × 270 at 50 %). Settings polish → T-93 (selected-segment accent outline, preview border token, label alignment, slot count on the stepper row). Grid, edit-mode help panel, drag ghost, empty hint, last-used + hover together, states 3/5/12, rim over snow and UI scale 2.0 (refused by the game at 2560 × 1440; use 1.75 or a taller display) were not captured; they move to UC-6 (T-75) / T-77. They do not block M4 (all functional checks passed in UC-4).
  - **Tests**: screenshot review.
  - **Size**: S

- [x] **T-62**: M4 performance record · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-8 grid and max size, P-9, P-17 in game)
  - **Depends on**: T-60
  - **Files**: `docs/project/performance.md` §5
  - **Acceptance criteria**:
    - [ ] Metrics recorded with pass/fail at 50 % of budget; failures become `perf:` tasks.
  - **Result (2026-10-19, 02997d3)**: performance.md "UC-4 record". All rows pass except the settings-page texture rebuild while dragging the button-size slider at UI 1.5 with 5 rings (32.5 ms at B 96, budget 5 ms) → T-95. Items still unmeasured are assigned to UC-5/UC-6 in performance.md.
  - **Tests**: none.
  - **Size**: S

### M5: Availability, compatibility, settings cleanup

- [-] **T-63**: Core `TriggerStateMachine` hold mode (removed: hold-and-release mode dropped by user 2026-10-19; leftover code removed in T-94)

- [-] **T-64**: Hold mode in the window and the trigger-mode setting (removed: hold-and-release mode dropped by user 2026-10-19; the `TriggerMode` setting is removed in T-94)

- [x] **T-94**: Remove the partial hold-mode code and the trigger-mode setting · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-2 (press-to-toggle only, no trigger-mode setting), FR-17, NFR-6, NFR-8 (spec FR-2 as revised 2026-10-19)
  - **Depends on**: — (T-21, T-81 done)
  - **Files**: `src/QuickMenu.Core/Input/{TriggerStateMachine,TriggerEvent}.cs`, `src/QuickMenu.Core/Settings/MenuEnums.cs`, `src/QuickMenu/Settings/QuickMenuSettings.cs`, the tests that use `HoldMode` or `TriggerReleased` under `tests/QuickMenu.Core.Tests/{Input,Perf}/` (e.g. `TriggerStateMachineHubAndUnpinTests`, `TriggerStateMachineToggleTests`, `ZeroAllocTests`), `docs/project/design.md` (S3 hold, S4a hover-while-held note, S7 Trigger mode row, close and cancel rules), `docs/project/architecture.md` (§4 settings field list, any hold-mode wording)
  - **Acceptance criteria**:
    - [ ] Given Core, then `TriggerStateMachine.HoldMode`, `TriggerEventKind.TriggerReleased` with both `TriggerEvent.TriggerReleased(...)` factories, the release-over-hub cancel rule and the `TriggerMode` enum are gone, and so are the tests for them; `git grep -nE 'HoldMode|TriggerReleased|TriggerMode' src tests` is empty. The remaining toggle, hub, edit-mode, satellite and drag rows still pass unchanged, and no other tests change (tests first: delete the hold rows, then the code).
    - [ ] Given `QuickMenuSettings`, then the `TriggerMode` field, its default and reset, its `Scribe` line and its validation are removed. Given a settings file with `<triggerMode>Hold</triggerMode>` (or `Toggle`), when it loads, then no error or warning is logged, the value is ignored, the menu toggles, and the next settings write leaves out `triggerMode`. The settings page has no Trigger mode row and no Keyed key for it is left (if one exists in `QuickMenu.Settings.xml`, it is removed).
    - [ ] Given design.md and architecture.md, then no text describes hold-and-release, hover-open in hold, a release over the hub, or a trigger-mode setting, except one line that says it was removed by the user on 2026-10-19 (spec FR-2).
    - [ ] `scripts/test.sh` (Core ≥ 90 % lines), `scripts/perf.sh`, the build (0 warnings) and the format check pass.
    - [ ] *(UC-5)* The user's existing settings file (it has `triggerMode` saved) loads with no `[QuickMenu]` warning, and the trigger opens and closes the menu as before.
  - **Tests**: the updated `TriggerStateMachine*Tests` and `ZeroAllocTests`; the grep above; build; in-game UC-5 (settings file load).
  - **In-game check**: UC-5 (user)
  - **Size**: S

- [x] **T-65**: `AvailabilityResolver` and greyed slots
  - **Owner**: developer
  - **Requirements**: FR-14, NFR-4, NFR-6 (ADR 0006, U-4)
  - **Depends on**: T-14, T-40, T-42
  - **Files**: `src/QuickMenu/Catalog/AvailabilityResolver.cs`, `src/QuickMenu/Menu/{QuickMenuWindow,UnavailableReasons}.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] *(partly done in T-32 UC-2 fix: use-mode slot, research / building / tech-level reasons)* Given a `Disabled` designator, then the reason is `disabledReason`; given `Visible == false`, then the reason is composed from the causes found in S-5 (research with the project label, tech level, required building, monolith, grav engine) using game strings where they exist, else "Not available right now."
    - [ ] Known gap from T-7: Disabled designators must never be activated (the skeleton does not check), and activation fires on mouse-up over the same slot.
    - [ ] Given a greyed slot, then the tooltip adds the reason in `danger`; clicking it plays `ClickReject` and shows the reason as a `RejectInput` message; availability is evaluated on open and on hover only.
    - [ ] Given an Ideology build gated by a precept or ideoligion building, or a Biotech mechanitor item, then the slot is greyed with the matching reason (the causes T-32 left open: monolith, grav engine, Biotech, precept).
    - [ ] *(UC-5)* An unresearched build (e.g. a tier-2 power building on a new colony) shows the research reason; a Biotech mech building before its research is greyed with the research reason.
  - **Tests**: in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: M

- [x] **T-66**: Missing-mod pins end to end · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-14, FR-17, NFR-6 (architecture §4, U-2)
  - **Depends on**: T-57, T-59
  - **Files**: `src/QuickMenu/Pinning/PinService.cs`, `src/QuickMenu/Menu/QuickMenuWindow.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] Given a modifier+click on a designator that is not an architect designator (not in any DesignationCategoryDef, e.g. Designator_MechControlGroup on a pawn), then the pin prefix does not intercept it and the designator behaves normally (T-68 audit risk 2).
    - [ ] Given a pin whose def or type is not loaded, then Fixed reserves its slot (empty in use mode, "?" in edit mode with the S4 state 8 tooltip and mod name), Auto hides it, and the settings count includes it; no error is logged.
    - [ ] Given the mod is added back, then the pin returns in its place.
    - [ ] *(UC-5)* Journey 6 with the stress mod (or BAM-only designator): pin, remove the mod, restart, check, re-add, check.
  - **Tests**: `PinBoardFixedTests`, `PinBoardAutoTests`, `PinSerializerTests`; in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: S

- [x] **T-67**: Mod detection, Mint Alt default, notices · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-1, FR-8, NFR-2, NFR-4 (design.md S7 Mint line, S8)
  - **Depends on**: T-31
  - **Files**: `src/QuickMenu/Compat/ModDetection.cs`, `src/QuickMenu/Menu/FirstRunNoticeWindow.cs`, `src/QuickMenu/QuickMenuMod.cs`, `mod/Languages/English/Keyed/QuickMenu.Notices.xml`
  - **Acceptance criteria**:
    - [ ] Given startup, then Mint (`Dubwise.DubsMintMenus`), BAM and Multiplayer are detected once; the effective modifier is Alt when Mint is active and the user has not chosen; the settings page shows the Mint line.
    - [ ] Given the first colony map ever, then the first-run notice (S8) appears once with [Use middle mouse] [Open settings] [OK], and includes the Mint line if Mint is active (setting both flags).
    - [ ] Given Mint becomes active after the first run, then the one-time `NeutralEvent` message appears once.
    - [ ] *(UC-5)* Setups (c) and (d): Alt+click pins, Ctrl+click is left to Mint, notices appear once.
  - **Tests**: `ModifierPolicyTests`; in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: M

- [x] **T-68**: Multiplayer readiness audit · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-18, NFR-2 (ADR 0006, A-2)
  - **Depends on**: T-41
  - **Files**: `docs/project/test-checklist.md` (FR-18 item, marked blocked until Multiplayer supports 1.7), `docs/project/decisions/0006-designator-catalog-and-activation.md` (audit note)
  - **Acceptance criteria**:
    - [ ] Given the source, then a grep shows no `Designate*` call, no Multiplayer API reference, no world/map state mutation outside `SetStuffDef`, `SetActiveDesignator` and `DesignatorManager.Select`; the result is written in ADR 0006.
    - [ ] Given Multiplayer has no 1.7 build, then the FR-18 checklist item is recorded as **blocked (not failed)** per A-2, with the exact steps for when it does.
  - **Tests**: grep audit in the checklist; in-game setup (e) when available (UC-6).
  - **Size**: S

- [x] **T-69**: UC-5 checklist, session guide and run
  - **Owner**: test-engineer
  - **Requirements**: FR-2 (toggle only), FR-6, FR-8, FR-14, FR-17, NFR-1, NFR-2, NFR-6
  - **Depends on**: T-65, T-66, T-67, T-68, T-93, T-94, T-95
  - **Files**: `docs/project/test-checklist.md` (M5, UC-5), `docs/project/test-reports/uc-5.md`
  - **Acceptance criteria**:
    - [ ] *(UC-5)* Carry-over M4 checks (M4 test review): Forget missing pins in settings after removing a mod (count, confirmation, IsReady gating, one write); one write per edit session (self-test `saves` +1 on Done, +0 without changes); Auto "menu is full" message in game; changed rings/columns/rows/button size used on next open.
    - [ ] *(UC-5)* Large-scale check at **UI scale 1.75 with button size 96** (the game refuses 2.0 on the user's 2560×1440 screen; user decision 2026-10-19): wheel, grid, sub-ring and material ring sharp and fully on screen; texture rebuild times recorded.
    - [ ] Given the guide, then it has no hold-mode or trigger-mode step; it has a step that loads the user's existing settings file (it contains `triggerMode`) and checks the log for `[QuickMenu]` warnings and that the trigger still toggles the menu (T-94), a settings page screenshot for the T-93 polish, and the T-95 slider re-measure (UI scale 1.5, 5 rings, drag the button-size slider from 36 to 96 and back, then "perf report": rebuild count and the time of each rebuild; one more run at UI 2.0 if the game allows it).
    - [ ] *(UC-5)* Carry-over M3 checks: an Ideology-gated pin resolves live after loading a game whose ideoligion allows it and after a precept change (rebuild count +1 only on change); moddrop pin with BAM resolves; carpets keep their own icons in their sub-ring.
    - [ ] Items for every M5 criterion on (a), (c), (d), journey 6, removing Quick Menu from a save and loading it (FR-17: no errors), and Ctrl/Alt+click in Mint's own architect window and BAM with Mint.
    - [ ] *(UC-5)* Results recorded; failures become bug tasks.
  - **Tests**: the session itself.
  - **In-game check**: UC-5 (user)
  - **Size**: S

- [x] **T-70**: M5 visual review · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7
  - **Depends on**: T-69
  - **Files**: `docs/project/design/screens/m5/*`, `docs/project/design.md`
  - **Acceptance criteria**:
    - [ ] Greyed state, reserved-missing state, notices and the settings Mint line match design.md; the user confirms.
  - **Tests**: screenshot review.
  - **Size**: S

  - **Result (2026-10-19)**: no new menu visuals in M5 beyond the greyed reasons, the first-run notice window and the settings polish; the preview rim bug found in UC-5 was fixed (729d7cc) and confirmed by the user. Screenshots of the first-run notice, the polished settings page and the greyed-with-reason tooltip are carried to UC-6/T-77.
- [x] **T-71**: M5 performance record · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-3, P-4 with BAM and Mint, P-5 in game)
  - **Depends on**: T-69
  - **Files**: `docs/project/performance.md` §5
  - **Acceptance criteria**:
    - [ ] Metrics recorded with pass/fail at 50 % of budget; P-4 must be 0 with vanilla, BAM and Mint, or R-1 is reopened.
    - [ ] Given the T-95 fix, then the settings-slider texture rebuild row is re-measured (UI 1.5, B 96, 5 rings; UI 2.0 if run): rebuilds per drag and ms per rebuild, pass/fail against 5 ms, closing or keeping open the UC-4 FAIL row in performance.md.
  - **Tests**: none.
  - **Size**: S

- [x] **T-93**: Settings page polish from the UC-4 review
  - **Owner**: visual-designer
  - **Requirements**: FR-6, NFR-5, NFR-7 (design.md S7, §8 "UC-4 visual review" items 7–10)
  - **Depends on**: T-59 (done; was T-64 until hold mode was dropped, 2026-10-19). T-95 follows it in the same file.
  - **Files**: `src/QuickMenu/Settings/SettingsPage.cs`, `src/QuickMenu/Settings/LayoutPreview.cs` (preview border), `src/QuickMenu/Settings/BindingRow.cs` (if the binding row label is drawn there)
  - **Acceptance criteria**:
    - [ ] Given a segmented control (Shape, Size), then the selected segment keeps `DrawHighlightSelected` and also has a 2 px `accent` outline inside its rect; unselected segments have none.
    - [ ] Given the preview box, then its 1 px border is `slotBorder`, not white.
    - [ ] Given any labelled row (Open menu with, Pin with, Shape, Size, Rings/Columns/Rows, Button size), then the label is `TextAnchor.MiddleLeft` in the row rect, and `Text.Anchor` is reset afterwards. Within each column, every control starts at one x: the widest measured label of that column + 8, at least 90.
    - [ ] Given the Rings/Columns/Rows stepper, then the slot count ("18 slots") is on the stepper row, 8 px after [+], Small `textMuted`, and the row below it is removed.
    - [ ] All colours come from `StyleTokens`; no literals. No per-frame allocations are added (labels and widths cached as now).
  - **Tests**: build and format; screenshot check in UC-5 (T-69), reviewed in T-70.
  - **Size**: S

- [x] **T-95**: `perf:` settings page rebuilds textures once per slider drag, not per step
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (texture rebuild ≤ 5 ms, performance.md UC-4 record), NFR-5, FR-6
  - **Depends on**: T-93 (same file `SettingsPage.cs`)
  - **Files**: `src/QuickMenu/Settings/{SettingsPage,LayoutPreview}.cs`, `src/QuickMenu/Menu/Textures.cs` (rebuild entry point, if the trigger moves), `src/QuickMenu/Diagnostics/PerfCounters.cs` (rebuild count), optional pure helper `src/QuickMenu.Core/Settings/RebuildGate.cs` with `tests/QuickMenu.Core.Tests/Settings/RebuildGateTests.cs` (test first), `docs/project/performance.md` (UC-4 FAIL row: fix note)
  - **Acceptance criteria**:
    - [ ] Also split the P-8 window counter by layout (wheel, grid, wheel satellite, sub-grid) in PerfCounters so UC-5 can report each separately (zero-alloc when off).
    - [ ] Given the button-size (or ring/row/column) value changes while the slider is being dragged, then no texture rebuild runs per drag step; the textures are rebuilt once when the drag ends (mouse up on the slider), or once the value has been unchanged for about 150 ms, whichever the engineer finds reliable under IMGUI; meanwhile the preview draws the existing textures scaled to the new size (a stretched rim is acceptable).
    - [ ] Given a drag from B 36 to 96 and back at any UI scale, then the dev-mode rebuild counter goes up by at most one per drag (plus one per pause if the debounce is used), and the menu opened afterwards uses textures built for the final values (no stale size).
    - [ ] Given the commit logic (drag-end or debounce) is pure, then it lives in Core with unit tests (value changes while dragging → no rebuild; release → one rebuild; unchanged value → none; time passed in as a parameter), and the per-frame path of the settings page adds no allocation.
    - [ ] Given one rebuild at UI 1.5, B 96, 5 rings, then it takes ≤ 5 ms, or the measured time is reported plainly with the cause (create + upload was 23.6 ms of the 32.5 ms at UC-4) and a proposed next step for the user to decide; the fix note is added to the UC-4 FAIL row in performance.md.
    - [ ] *(UC-5)* Re-measured in UC-5 (T-69 step, recorded by T-71): rebuilds per drag and ms per rebuild at UI 1.5 / B 96 / 5 rings, and at UI 2.0 if the game allows it.
  - **Tests**: unit `RebuildGateTests` (if the helper is added); `scripts/test.sh`, `scripts/perf.sh`; in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: S

### M6: Hardening and release prep

- [x] **T-72**: Fault isolation audit and fault-injection action · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: NFR-6, FR-17
  - **Depends on**: T-69
  - **Files**: `src/QuickMenu/Diagnostics/{PatchGuard,FaultInjection}.cs`, all patch and OnGUI entry points (review only, fixes where missing)
  - **Acceptance criteria**:
    - [ ] Given every Harmony patch body, `DoWindowContents`, `WindowUpdate`, settings UI and static constructors, then each is wrapped by `PatchGuard` (list in the PR).
    - [ ] Given dev-mode debug action "Quick Menu: inject fault" for each site, then the next call throws, one error is logged, the site is disabled, the game keeps running, and vanilla behaviour continues (e.g. architect clicks work after the pin prefix fails).
    - [ ] Given a corrupt settings file, then defaults load with one warning and unknown pin lines are kept.
  - **Tests**: `PinSerializerTests` (corrupt lines); in-game UC-6.
  - **In-game check**: UC-6 (user)
  - **Size**: M

- [x] **T-73**: Keyed-string and key-usage check in CI · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: NFR-4
  - **Depends on**: T-67
  - **Files**: `scripts/check-strings.sh`, `.github/workflows/ci.yml`, `scripts/test.sh` (call it)
  - **Acceptance criteria**:
    - [ ] Given `src/QuickMenu`, then the script fails on string literals passed to `Widgets.Label`, `Messages.Message`, `TooltipHandler.TipRegion`, `Widgets.ButtonText` and window titles (allow-list for log text and Keyed key names).
    - [ ] Given the Keyed XML, then every `"QuickMenu.*".Translate()` key used in code exists and every defined key is used.
  - **Tests**: the script runs in CI and passes.
  - **Size**: S

- [x] **T-74**: Clean-room, dependency and asset audit · `parallel-ok`
  - **Owner**: release-engineer
  - **Requirements**: NFR-7, NFR-3
  - **Depends on**: T-69
  - **Files**: `docs/project/assets.md`, `docs/project/test-reports/release-audit.md`
  - **Acceptance criteria**:
    - [ ] Given the repo, then nothing from `reference/` is tracked, no Keyed string is identical to a Mint Keyed string (script diff, result recorded), every non-code file is in assets.md, and every texture is ours.
    - [ ] Given About.xml and the package zip, then Harmony is the only dependency, no `0Harmony.dll` or pdb ships, and `supportedVersions` is 1.7.
  - **Tests**: audit commands recorded in the report.
  - **Size**: S

#### FR-19 pin marker (last feature; before UC-6 and release prep)

- [x] **T-87**: Spike S-8 (offline): pin-marker hook sites and pinned lookup; ADR 0009 · `parallel-ok`
  - **Owner**: architect (visual-designer advises on the free corner of the gizmo)
  - **Requirements**: FR-19, NFR-1, NFR-2, NFR-6 (architecture §6, ADR 0005 and its spike result, performance.md per-frame budgets)
  - **Depends on**: T-69 (M5 gate; the spike itself needs no M5 code)
  - **Files**: `docs/project/decisions/0009-pin-marker-hooks.md` (new), `docs/project/architecture.md` (§6 patch sites 4 and 5, §7 per-frame cost note, risk row "Only three patch sites")
  - **Time box**: one session. Output: a decision (ADR 0009), not production code. Any inspection script lives outside the repo (or in a gitignored scratch folder) and reads the DLLs by path; nothing is decompiled into the repo (NFR-7 applies to BAM too).
  - **Acceptance criteria**:
    - [ ] Given the 1.7.4982 `Assembly-CSharp.dll` (`$(RIMWORLD_DIR)` or the Krafs reference) and the Better Architect Menu DLL in the Steam workshop folder, when the candidates are inspected offline (`Command.GizmoOnGUI`, `Command.GizmoOnGUIInt`, `Gizmo.GizmoOnGUI` overrides in `Designator` and its subclasses incl. `Designator_Dropdown`, `Command.DrawIcon` / `Designator.DrawIcon`), then the ADR records for each: signature, which overrides exist (in the game and in BAM), whether vanilla `GizmoGridDrawer` and BAM's `DrawFlatGrid` / `DrawGroupedGrid` / `DrawOrdersPanel` all reach it, whether the button rect is available to the patch, and how often it runs per frame (per event type: Layout and Repaint).
    - [ ] Given dropdown list entries, when the candidates are inspected (the float menu that `Designator_Dropdown.ProcessInput` builds: `FloatMenuOption` icon / `extraPartOnGUI` / `DoGUI`, or a postfix on the dropdown's menu construction; plus how BAM shows dropdown lists), then the ADR chooses one site and states how a drawn option is mapped back to its element designator without per-frame work (e.g. an option → designator identity map filled once when the menu opens).
    - [ ] Given the chosen sites, then the ADR states: the patch type (postfix only; no transpiler), exact parameter types bound by index (`__0`, Harmony 3 type-checks), the fast path for every gizmo that is not a pinned designator (including pawn and building command bars, which go through the same base method), the guard (`if (Site.Disabled) return; try {…} catch → Site.Fail`), draw only on `EventType.Repaint`, and which corner of the gizmo rect is free of vanilla overlays (hotkey label, dropdown triangle, disabled overlay, BAM additions) at UI scale 1–2.
    - [ ] Given the lookup, then the ADR defines a zero-allocation O(1) "is pinned" check: an identity set of pinned designator instances (reference equality), rebuilt off the hot path when the board changes (pin, unpin, edit-mode swap is not a membership change), when settings load, when the catalog is built and when the Ideology part refreshes; the rules for a whole dropdown pin (marks the dropdown button only) vs a single-entry pin (marks the entry in the open list only); unresolved, opaque and missing-mod pins mark nothing; and how `build:` keys that resolve through `FindAllowedDesignator` map to the instance the architect draws.
    - [ ] Given ADR 0009, then architecture §6 lists the new site(s) as patch sites 4 (and 5) with their tasks (T-91, T-92), the "three patch sites" wording in §6 and the risk table is updated, and CLAUDE.md's patch-site count is reported to the orchestrator for update (this task does not edit CLAUDE.md).
    - [ ] Given BAM does not reach a candidate, or no site gives a rect for dropdown entries, then the ADR says so, names the fallback (or "not supported" for that UI), and the user is told before T-91 starts.
  - **Tests**: none (decision record); the decision is verified by T-90/T-91/T-92 and UC-6.
  - **Size**: S

- [x] **T-88**: P-19 budget and gate for the marker hook · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: FR-19, NFR-1
  - **Depends on**: T-87
  - **Files**: `docs/project/performance.md` (budget table row P-19, CI gate table, in-game measurement steps)
  - **Acceptance criteria**:
    - [x] Given performance.md, then row P-19 reads: marker hook cost per gizmo draw call (pinned and not pinned, designator and non-designator gizmo), ≤ 100 ns and 0 bytes; in-game total with the architect menu open on the largest category ≤ 10 µs per frame; methods: Core Perf-trait test and BenchmarkDotNet on the pinned-set lookup (T-90), and a dev-mode counter plus `MeasureScope` around the hook body reported by "Quick Menu: perf report" (T-91).
    - [x] Given the gate table, then the Core lookup gate is "0 bytes strict; fail above 5 × 100 ns", like P-3, and P-1's note says the marker hook runs only while gizmos are drawn (it is not part of the closed-menu cost unless a command bar is shown).
  - **Tests**: none (budget definition); the gates live in T-90.
  - **Size**: S

- [x] **T-89**: Pin-marker glyph and style tokens · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-19, NFR-5, NFR-7 (design.md §2 tokens, §3 glyphs, §6 texture level; the corner from T-87)
  - **Depends on**: T-87
  - **Files**: `src/QuickMenu.Core/Render/{Glyphs,StyleTokens,MaskSet,TextureSizing,PinMarkerSizing}.cs`, `tests/QuickMenu.Core.Tests/Render/{GlyphsTests,StyleTokensTests,PinMarkerSizingTests}.cs`, `src/QuickMenu/Menu/{Textures,QMStyle}.cs`, `docs/project/design.md` (new subsection "Architect pin marker")
  - **Acceptance criteria**:
    - [x] Given the glyph set, then `Glyphs.PinMarker` (our own pin shape, not Mint's or BAM's art) is rasterized in its final orientation, deterministic, with a coverage test (alpha mass in the expected region, empty margin) like the other glyphs, test first.
    - [x] Given the marker size is a fixed fraction of the gizmo (design.md states it, e.g. about 0.2 × the 75 px gizmo), then the texture is rasterized at its drawn size in screen px (`Glyphs.MinSize` rules), with a `rimOutline` copy 1 px larger under it, and rebuilt with the other glyphs on UI-scale change within the existing ≤ 5 ms rebuild budget; never per frame.
    - [x] Given `QMStyle` / `StyleTokens`, then the marker colour and outline are tokens (no colour literals), with a contrast note against a typical gizmo background (the marker is a shape, not colour alone).
    - [x] Given design.md, then the new subsection states the corner (from ADR 0009), size, tokens and draw order (drawn last, after the game's own gizmo drawing), and that it is drawn only for pinned designators.
  - **Tests**: unit `GlyphsTests` (PinMarker coverage, determinism), `StyleTokensTests` (new tokens), texture sizing test if a sizing helper is added.
  - **Size**: S

- [x] **T-90**: Core pinned-set cache rules and perf gate (test first) · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-19, FR-10, FR-14, FR-15, NFR-1, NFR-8 (ADR 0009)
  - **Depends on**: T-87
  - **Files**: `src/QuickMenu.Core/Pins/{PinnedSet,PinnedSetBuilder}.cs`, `tests/QuickMenu.Core.Tests/Pins/PinnedSetTests.cs`, `tests/QuickMenu.Core.Tests/Perf/PinnedSetPerfTests.cs`, `tests/QuickMenu.Core.Benchmarks/PinBenchmarks.cs` (new benchmark method)
  - **Acceptance criteria**:
    - [x] ADR-0009 alignment (overrides older wording in this task): a single-entry pin gives its dropdown a `Partial` mark (decision 11); pins hidden beyond the current capacity are NOT marked (decision 13); add the def-keyed map (BuildableDef → mark) so BAM edit-mode designator copies are marked.
    - [x] Given Core cannot reference game types, then `PinnedSet<T> where T : class` uses reference identity (`ReferenceEqualityComparer`), and the builder takes the board's pins plus a resolver delegate/interface (key → instance or none) supplied by the adapter.
    - [x] Given a board with a whole-dropdown pin (`dropdown:` / `moddrop:`), then the dropdown instance is in the set and its elements are not; given a single-entry pin, then that element instance is in the set and its dropdown is not; given unresolved, opaque or missing-mod pins, then nothing is added for them; given pins hidden beyond capacity, then they are not members (superseded by the ADR-0009 line above).
    - [x] Given a pin, unpin, settings load, catalog build or Ideology refresh, when `Rebuild` runs, then membership matches the board exactly; given an edit-mode swap, then membership is unchanged; a `Version` counter increments on each rebuild so the adapter can detect staleness cheaply.
    - [x] Given warm-up, when `Contains` runs 10,000 times for members and non-members (and `null`), then 0 bytes are allocated and the time per call fails above 5 × 100 ns (P-19 gate, trait `Category=Perf`); `Rebuild` for 200 pins reuses its storage (no growth after the first build) and is benchmarked.
    - [x] Core line coverage stays ≥ 90 % and `CoreIsolationTests` still pass.
  - **Tests**: unit `PinnedSetTests` (rules table above); perf `PinnedSetPerfTests`; benchmark `PinBenchmarks.PinnedSetContains`.
  - **Size**: M

- [x] **T-91**: Pin marker on architect buttons (hook site 4: vanilla, BAM, Mint's architect)
  - **Owner**: developer
  - **Requirements**: FR-19, NFR-1, NFR-2, NFR-5, NFR-6 (ADR 0009, architecture §6)
  - **Depends on**: T-72 (fault-injection list), T-88, T-89, T-90
  - **Files**: `src/QuickMenu/Pinning/{PinMarkerPatch,PinnedDesignators}.cs` (new), `src/QuickMenu/Pinning/PinService.cs` (rebuild on change), `src/QuickMenu/Catalog/DesignatorCatalog.cs` (rebuild after build and Ideology refresh), `src/QuickMenu/QuickMenuMod.cs` (patch registration), `src/QuickMenu/Diagnostics/{FaultInjection,PerfCounters,SelfTest}.cs` (new site, counter, report)
  - **Acceptance criteria**:
    - [x] ADR-0009: hook = postfix on `Command.GizmoOnGUIInt(Rect, GizmoRenderParms)`; marker only while an architect menu is drawing (vanilla tab / BAM / Mint build window grid view); the lookup is never rebuilt on the per-gizmo path. Known limits (user, 2026-10-19): Mint's list view is not marked; Mint's wheel gets no markers (pinning there still works).
    - [x] Given the site chosen in ADR 0009, then a Harmony postfix with exact parameter types bound by index follows the patch-guard convention (`if (Site.Disabled) return; try {…} catch (Exception ex) { Site.Fail(ex); }`), returns on anything but `EventType.Repaint`, and for a gizmo that is not a `Designator` returns after the measure scope, the disabled flag and one type check (order: measure scope, disabled/fault check, type check, once-per-frame revalidate only when the board has a pin, empty set, Repaint, lookup, architect context), and a designator not in the set costs one lookup (no LINQ, closures, string building or allocation).
    - [x] Given `PinnedDesignators` (adapter wrapper over Core `PinnedSet<Designator>`), then it is rebuilt by `PinService` after every toggle, by settings load and by the catalog after its build and after the Ideology refresh, never by the hook; the hook only reads it.
    - [x] Given a pinned designator drawn by vanilla `GizmoGridDrawer` or by BAM, then `Textures.PinMarker` is drawn in the corner from ADR 0009 with `QMStyle` tokens via `GUI.DrawTexture` (no rotation, no new texture per frame), scaled with the gizmo rect so it is correct at UI scale 1–2; a whole-dropdown pin marks the dropdown button.
    - [x] Given a pin or unpin by modifier+click while the architect menu is open, then the marker appears or disappears on the next frame.
    - [x] Given "Quick Menu: inject fault" for the marker site, then one error is logged, the site is disabled, and architect buttons keep drawing and working without markers.
    - [x] Given dev mode, then a counter and `MeasureScope` around the hook body feed "Quick Menu: perf report" (calls per frame, median/p99 ns per call, allocated bytes), and the self-test lists the marker patch as patched or failed; with dev mode off the measurement costs one cached bool check.
    - [ ] *(UC-6)* Markers show on pinned buttons in setups (a), (b) and (c)/(d) (Mint's architect, where pinning works), not on unpinned ones, at UI scale 1.0 and 2.0; P-19 is within budget with the architect open and with a pawn's command bar shown.
  - **Tests**: Core rules in T-90; build; `scripts/test.sh`, `scripts/perf.sh`; in-game UC-6.
  - **In-game check**: UC-6 (user)
  - **Size**: M

- [x] **T-92**: Pin marker on pinned entries in an open dropdown list (hook site 5)
  - **Owner**: developer
  - **Requirements**: FR-19, FR-10, NFR-1, NFR-2, NFR-6 (ADR 0009; T-27 entry pinning)
  - **Depends on**: T-91
  - **Files**: `src/QuickMenu/Pinning/{DropdownMarkerPatch,PinnedDesignators}.cs`, `src/QuickMenu/QuickMenuMod.cs` (patch registration), `src/QuickMenu/Diagnostics/{FaultInjection,SelfTest}.cs`
  - **Acceptance criteria**:
    - [x] ADR-0009 site 5: postfixes on `Designator_Dropdown.ProcessInput` (map options → entries via DropdownEntryPatch's EntryAction — expose it), `FloatMenuOption.DoGUI` and `FloatMenuGridOption.OnGUI`; files include `src/QuickMenu/Pinning/DropdownEntryPatch.cs`.
    - [x] Given the site chosen in ADR 0009 for dropdown entries, then the option → element mapping is built once when the list opens (allocation allowed there), and the per-frame draw path does only an identity lookup and a `Contains` (0 bytes), under the same patch-guard convention; if ADR 0009 found a UI (e.g. BAM's dropdown list) with no usable site, that UI is listed as a known limit for T-79 and the user is told.
    - [x] Given a single entry of an open dropdown list is pinned, then that entry shows the marker in the same corner logic as T-91 (adapted to the list row); given only the whole dropdown is pinned, then no entry shows it.
    - [x] Given a modifier+click on an entry in the open list (T-27 path), then its marker appears or disappears on the next frame if the list stays open.
    - [x] Given "Quick Menu: inject fault" for this site, then one error is logged, the site is disabled, and the dropdown list keeps working without markers.
    - [ ] *(UC-6)* In setups (a) and (b), a pinned entry shows the marker in the open list and an unpinned one does not.
  - **Tests**: Core rules in T-90 (entry vs whole dropdown); build; `scripts/test.sh`, `scripts/perf.sh`; in-game UC-6.
  - **In-game check**: UC-6 (user)
  - **Size**: M

- [ ] **T-75**: UC-6 final regression session
  - **Owner**: test-engineer
  - **Requirements**: all FR-1 … FR-19, NFR-1, NFR-2, NFR-3, NFR-5, NFR-6
  - **Depends on**: T-72, T-73, T-74, T-88, T-91, T-92
  - **Files**: `docs/project/test-checklist.md` (UC-6 = full checklist), `docs/project/test-reports/uc-6.md`
  - **Acceptance criteria**:
    - [ ] *(UC-6)* Carry-over M5 checks (M5 test review): Forget missing pins (confirmation, single write); Auto "menu is full" message; removing Quick Menu from a save and loading it (no errors); Alt/Ctrl+click inside Mint's own architect window with BAM; slider rebuild at UI 1.5 with 5 rings; P-3/P-4/P-5 with BAM and Mint; screenshots of the first-run notice, settings page and a greyed tooltip (T-70); Ideology precept reason if a precept-gated build is available.
    - [ ] The guide covers the whole checklist on (a)–(d) and (R), (e) if Multiplayer supports 1.7, the fault-injection action, the NFR-3 load test (only Harmony + Quick Menu), final measurement actions, and 3–5 clean screenshots for the Preview (wheel with a sub-ring, material ring, grid, edit mode) on an attractive colony.
    - [ ] The guide has an FR-19 section: with the architect menu open, the marker is on pinned buttons in vanilla (a), BAM (b) and Mint's architect (c or d), on a dropdown button whose whole dropdown is pinned, and on a pinned single entry in an open dropdown list (vanilla and BAM); after a modifier+click unpin it is gone on the next frame and after a pin it appears; one screenshot at UI scale 1.0 and one at 2.0; the fault-injection action for both marker sites; and the P-19 measurement ("perf report" after 600 frames with the architect open on the largest category, and again with a colonist selected so its command bar is drawn).
    - [ ] *(UC-6)* Every Must item passes, or the user accepts a documented exception.
  - **Tests**: the session itself.
  - **In-game check**: UC-6 (user)
  - **Size**: S

- [ ] **T-76**: Final performance pass and hardware-margin report · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1
  - **Depends on**: T-75
  - **Files**: `docs/project/performance.md` §1 (Latest, Status), §5
  - **Acceptance criteria**:
    - [ ] Given UC-6 numbers on (R) and the stress mod, then every P-row has a latest value and status; every in-game metric is ≤ 50 % of its budget or has a documented decision by the user.
  - **Tests**: `scripts/perf.sh` green; in-game numbers from UC-6.
  - **Size**: S

- [ ] **T-77**: Workshop preview image · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: A-3 (spec §12), NFR-7
  - **Depends on**: T-75
  - **Files**: `mod/About/Preview.png`, `scripts/make-preview.sh`, `docs/project/assets.md`
  - **Acceptance criteria**:
    - [ ] Given the UC-6 session, then the screenshots listed in design.md §8 "UC-4 visual review" (not captured in UC-4) exist and are reviewed: grid with sub-grid and material grid, edit-mode help panel (normal and full), drag ghost and drop target, empty hint, last-used + hover together, states 3/5/12, rim over snow, and the highest UI scale the game allows (2.0, or 1.75 at 2560 × 1440) with B 96.
    - [ ] Given the UC-6 screenshots, then `Preview.png` is 2.468:1 (e.g. 1234 × 500), under 1 MB, made by a checked-in `magick` script with title text, contains no Mint imagery, and its assets.md row is in the main table; the user confirms the look.
  - **Tests**: `identify mod/About/Preview.png` size check.
  - **Size**: S

- [ ] **T-78**: README and Workshop description · `parallel-ok`
  - **Owner**: docs-writer
  - **Requirements**: spec §12, NFR-2, NFR-3, NFR-7
  - **Depends on**: T-75
  - **Files**: `README.md`, `docs/workshop-description.txt` (Steam BBCode), `mod/About/About.xml` (`description`)
  - **Acceptance criteria**:
    - [ ] README covers what it does, install (Harmony required), default trigger and the middle-mouse fallback, pinning (Ctrl/Alt+click in the architect menu, vanilla or BAM; modifier+click on a slot to unpin; edit mode to rearrange; pinned items show a pin marker in the architect menu, FR-19), wheel/grid, settings, compatibility notes (BAM, Mint, Multiplayer status), building from source, license MIT, and the optional attribution line from assets.md.
    - [ ] The Workshop description says the same in BBCode, under Steam's length limit, and states the 1.7 beta status.
  - **Tests**: link check and a read-through by the user.
  - **Size**: S

- [ ] **T-79**: CHANGELOG · `parallel-ok`
  - **Owner**: docs-writer
  - **Requirements**: spec §12
  - **Depends on**: T-75
  - **Files**: `CHANGELOG.md`
  - **Acceptance criteria**:
    - [ ] Keep-a-Changelog format with the release version and date, listing user-visible features and known limits (Multiplayer unverified if blocked, BAM status).
  - **Tests**: read-through.
  - **Size**: S

- [ ] **T-80**: Version bump, package and draft release
  - **Owner**: release-engineer
  - **Requirements**: spec §12, NFR-3
  - **Depends on**: T-76, T-77, T-78, T-79
  - **Files**: `mod/About/About.xml` (`modVersion`), `Directory.Build.props` (assembly version), `dist/` (gitignored)
  - **Acceptance criteria**:
    - [ ] Steam Workshop upload is done **manually by the user through the game's built-in uploader** (user, 2026-10-19). T-80 prepares everything they need: the release folder/zip, `About/Preview.png`, the Workshop title and description text (T-78), and a short step-by-step upload note. No agent uploads to the Workshop.
    - [ ] Given the agreed version (Question 5), then About.xml and the assembly version match, `scripts/package.sh` produces `dist/QuickMenu-<version>.zip` with only mod content, CI is green on the release branch, and a **draft** GitHub release with the zip and CHANGELOG text is prepared but not published.
    - [ ] Publishing (GitHub release and Workshop upload) is left to Phase 7 and needs the user's explicit confirmation.
  - **Tests**: `unzip -l` of the zip matches the expected file list; CI run.
  - **Size**: S

## Requirement traceability
| Requirement | Tasks | Tests |
|---|---|---|
| FR-1 trigger binding, mouse 3–5, unbound, middle-mouse option | T-5, T-11, T-20, T-30, T-58, T-67 | `TriggerBindingTests`; UC-1 (input probe), UC-2 (binding items), UC-5 (first-run notice) · M1: UC-1 S-2 PASS (mouse 0-4 polled, 3=Mouse 4); skeleton poller only, no unit tests (binding logic is T-20) · M2: `TriggerBindingTests`, `BindingCaptureTests`, `InputEdgeTests`; UC-2 only binding Mouse 4 evidenced (Mouse 5, middle, key, unbound, capture dialog warnings: no evidence yet) · M5: `NoticePolicyTests`, `M5ReviewGapTests` (notice truth table); UC-5 first-run notice PASS (once, non-blocking); Mouse 5, middle, key, unbound, capture dialog warnings: still no in-game evidence -> UC-6 |
| FR-2 press-to-toggle only; no hold mode, no trigger-mode setting (hold dropped by the user 2026-10-19) | T-17, T-21, T-31, T-32, T-94 | `TriggerStateMachineToggleTests`, `HitTesterTests`; T-94 grep (`HoldMode`, `TriggerReleased`, `TriggerMode` absent); UC-2, UC-5 (old settings file with `triggerMode` loads, menu toggles) · M1: none yet (skeleton toggle only, UC-1 S-6) · M2 (toggle): `TriggerStateMachineToggleTests`, `...SequenceTests`, `...HubAndUnpinTests`, `...ClosedAndUnknownTests`, `HitTesterTests`, `HitTesterEdgeTests`; UC-2 close paths PASS · T-63/T-64 removed; the leftover hold code and setting go in T-94 (M5) · M5: `TriggerStateMachine*Tests` after T-94; UC-5 old settings file with `<triggerMode>` loads cleanly PASS |
| FR-3 centred on mouse, edge dodge, oversize centred | T-7, T-16, T-32, T-36 | `EdgeDodgerTests`, `MenuBoundsTests`, `SubMenuLayoutTests`; UC-2 (corners), UC-4 (UI scale 2.0) · M1: none yet (skeleton centres on mouse, UC-1 S-6; no edge dodge until T-16) · M2: `EdgeDodgerTests`, `EdgeDodgerTinyScreenTests`, `MenuBoundsTests`; UC-2 four corners PASS; UI scale 2.0 not yet |
| FR-4 wheel and grid with the same features | T-2, T-17, T-29, T-81, T-50, T-54, T-56, T-57, T-59 | `WheelLayoutTests`, `GridLayoutTests`, `HitTesterGridTests`; UC-4 FR-4 matrix · M1: `WheelLayoutTests`, `WheelLayoutExtremesTests`, `RingCapacityTests`, `WheelHitTestTests`, `WheelHitTestBoundaryTests` · M2 (wheel): `WheelLayoutCacheTests`, `HitTesterTests`; UC-2 wheel look PASS; grid: none yet (M4) · M4: `GridLayoutTests`, `HitTesterGridTests`, `AutoAndSubGridPlacementTests`, `M4ReviewGapTests` (every cell of 3x1 .. 11x9 hits its slot, far-edge cell); UC-4 PASS (grid panel, hover/plate, activate, sub-grid, Wall material grid, edge dodge, edit-mode swap); no unit tests for the adapter (`GridRenderer`, window); grid at UI scale 2.0, grid in edit mode with help panel and drag ghost: not captured, moved to UC-6 |
| FR-5 Fixed / Auto sizing, full | T-18, T-25, T-32, T-51, T-52, T-56, T-57, T-59 | `PinBoardFixedTests`, `PinBoardAutoTests`, `CapacityTests`; UC-2 (full message), UC-4 · M2 (Fixed): `PinBoardFixedTests`, `PinBoardCapacityTests`; the 19th-pin full message has no in-game evidence; Auto: none yet (`PinBoardAutoTests` is M4) · M4: `PinBoardAutoTests`, `CapacityTests`, `PinBoardCapacityTests`, `M4ReviewGapTests` (Fixed/Auto snapshot dropped by every pin change, restored otherwise, hidden pins kept; Auto full flips exactly at the screen-minus-margin boundary, wheel and grid); UC-4 PASS (Auto: only pins shown, new pins at the end, second ring after 6, unpin closes gap, Fixed->Auto->Fixed restores); the "menu is full" message in Auto and the Auto full state on a real screen: no in-game evidence (UC-5/UC-6) |
| FR-6 size settings used on next open | T-2, T-31, T-32, T-50, T-51, T-59, T-93, T-95 | `WheelLayoutTests`, `CapacityTests`; UC-2, UC-4 · M1: `WheelLayoutTests` (geometry only; setting not wired yet) · M2: `WheelLayoutCacheTests`, `SettingsRulesTests`; UC-2 shows only the settings page; no recorded check that a changed ring/size is used on next open (UC-2b step 7 result not recorded) · M4: `GridLayoutTests`, `CapacityTests`, `SettingsPreviewTests`, `M4ReviewGapTests` (stepper clamps, control visibility matrix, preview scale limits); UC-4 PASS (settings change shows in the preview live; UI scale 1.5 warning); that a changed ring/column/row value is used on the next open of the real menu: no recorded check; button-size slider rebuild FAIL -> T-95 (UC-5) · M5: `RebuildGateTests`, `M5ReviewGapTests` (gate, stretch), `TextureSizingTests` (RimStretch); UC-5 slider rebuild once per drag PASS (1.5-5.8 ms at UI 1), preview rim fixed 729d7cc; UI 1.5 / B 96 / 5 rings and UI 2.0 timing not recorded; changed ring/column/row used on next open of the real menu: no recorded check -> UC-6 |
| FR-7 ~~searchable picker~~ — **Won't** (removed by the user 2026-10-19, after UC-2) | — (T-38, T-39, T-45 removed; T-83, T-84, T-85 remove it from design, performance and architecture docs) | — (no test: nothing to build). The catalog work done under FR-7 in M1 (T-6, T-9, T-14) now serves FR-8, FR-10 and FR-14 |
| FR-8 modifier+click on any designator; Mint → Alt | T-8, T-20, T-26, T-27, T-31, T-67 | `ModifierPolicyTests`, `PinClickClassifierTests`; UC-1 (S-3 probe), UC-2 (a, b), UC-5 (c, d) · M1: UC-1 S-3 (idle 0 calls; modifiers via `Input`; dropdown entry needs T-27); patch count 8/0 failed · M2: UC-2 PASS (Ctrl+click Wall/Mine/dropdown button/dropdown entry, no tool activated, BAM); `PinClickDeduperTests`, `PinFeedbackTests`; Alt/Shift choice, Mint default and hotkey pass-through: no in-game evidence · M5: `ModIdsTests`, `ModifierPolicyTests`, `M5ReviewGapTests` (id variants); UC-5 Mint: Alt+click pins, Ctrl+click goes to Mint PASS; non-architect designator Ctrl+click untouched PASS; Ctrl/Alt+click inside Mint's own window and BAM+Mint P-3/P-4 pin-click timing: not recorded -> UC-6 |
| FR-9 edit mode: centre Edit/Done button, drag swap, modifier+click unpin (use and edit mode), new pins in the first free slot (Fixed) or at the end (Auto); no "+", no insert cursor | T-17, T-18, T-21, T-81, T-82, T-83, T-44, T-52, T-55, T-57 | `PinBoardFixedTests`, `PinBoardAutoTests`, `HitTesterTests`, `TriggerStateMachine*Tests`; UC-2b (centre button, unpin on the wheel), UC-3 (edit shell), UC-4 (drag, journey 5) · M2: unpin by modifier+click and centre Edit button (inert) UC-2b PASS; `TriggerStateMachineHubAndUnpinTests`; edit mode, drag swap: none yet (M4) · M3: `TriggerStateMachineEditModeTests`, `M3ReviewGapTests` (edit-mode close/save flag, resume, press/release mismatch); UC-3 PASS (pencil -> Done, plain click no-op, Ctrl+click unpins and stays open, Done exits, outside click closes, removal persists, carry-over M2 settings); drag swap, new pin in first free slot with edit open, Auto append: none yet (M4) · M4: `TriggerStateMachineDragTests`, `PinBoardAutoTests`, `PinBoardFixedTests` (Swap), `M4ReviewGapTests` (euclidean 6 px threshold, drag survives returning to source, close/MapLost/PressLost end a drag, drop on plate/padding cancels, second drag after cancel); UC-4 PASS (help panel, drag to empty slot moves, drag swaps, Esc/right-click cancel mid-drag, drop on hub/outside nothing, arrangement persists); one settings write per edit session: counter not recorded (UC-4 reports 25 writes over a session, `info`), P-17 explicit check -> UC-5 |
| FR-10 pin a whole dropdown | T-3, T-6, T-9, T-26, T-27, T-37, T-40, T-42, T-43 | `PinKeyTests`, `PinKeyFactoryTests`, `ModDropdownMatcherTests`; UC-1 self-test, UC-2, UC-3 · M1: `PinKeyTests`, `PinKeyEdgeTests`, `PinKeyFactoryTests`, `PinKeyFactoryEdgeTests`, `PinKeyRoundTripTests`; UC-1 S-4 · M2: UC-2 PASS (dropdown pinned as a whole, entry pinned alone); `PinEscapedKeyTests`; mod-dropdown matcher: none yet (M5) · M3: `ModDropdownMatcherTests`; UC-3 PASS (dropdown badge 1:30, Ctrl+click pins whole dropdown, stress mod pins resolve); journey 2 with BAM and `moddrop:` resolve in game: no evidence (M5) |
| FR-11 dropdown expands as sub-ring / sub-grid | T-21, T-36, T-42, T-43, T-54, T-56 | `SubMenuLayoutTests`, `TriggerStateMachine*Tests`, `SubMenuLayoutAllocTests`, `M3ReviewGapTests` (satellite modifier/back/reject, tiny-screen counts, sub-grid columns) · UC-3 PASS (sub-ring opens, main dims, option activates; Esc, right-click, hub, outside all go back; satellite rim rebuild 3.1 ms); sub-grid and grid-layout satellite: none yet (UC-4, M4); nested dropdown -> material ring -> Esc: not testable in vanilla, code review only (`TriggerStateMachineToggleTests` resume rows) |
| FR-11a dropdown of material variants shows material icons | T-36 (Core `MaterialVariants`), T-42, T-43 | `MaterialVariantsTests`, `M3ReviewGapTests` (duplicates, case, late missing ingredient, 500 elements) · UC-3 recheck PASS (user): fine tile sub-ring shows stone block icons; icon choice per element and "other dropdowns keep own icons" (carpets, BAM variants) have no in-game evidence beyond vanilla fine tile (UC-4 / UC-5 with a mod dropdown) |
| FR-12 material sub-ring | T-36, T-37, T-41, T-42, T-43, T-56 | `StuffOrderingTests`, `StuffSlotPolicyTests`, `SubMenuLayoutTests`, `M3ReviewGapTests` (duplicate ids, case, last-used at end never moves) · UC-3 recheck PASS (user): Wall ring fixed order, last-used highlighted only; Wall material ring pick and place PASS; 1 material skip, 0 materials greyed with "no materials" tooltip, god mode all stuffs, journey 4 chairs -> armchair -> wood: no recorded in-game evidence (UC-4); grid material ring: UC-4 |
| FR-13 activate exactly as the architect | T-6, T-7, T-29, T-32, T-41, T-43 | UC-1 (skeleton), UC-2, UC-3 · M1: UC-1 S-6 (Mine activates, Wall opens vanilla material list) · M2: UC-2 PASS (Wall -> material list -> tool, Mine, dropdown vanilla list); no unit tests (adapter) |
| FR-14 unavailable pins always greyed with reason (never hidden or empty); missing-mod pins hidden but kept | T-9, T-18, T-19, T-32, T-37, T-40, T-42, T-52, T-55, T-57, T-59, T-65, T-66, T-82 | `PinBoardFixedTests`, `PinBoardAutoTests`, `PinSerializerTests`, `ModDropdownMatcherTests`, `UnavailableReasonTests`; UC-1 (S-5 dump), UC-2b (morbid slabs, research-locked build), UC-3 (Ideology refresh), UC-5 (journey 6, greyed) · M1: UC-1 S-5 only (no unit tests for availability yet) · M2: UC-2b PASS (morbid slabs and research-locked build greyed with reason, reject on click); `UnavailableReasonTests`, `PinSerializerEdgeTests`; missing-mod hidden pins: none yet (M5) · M3: `IdeoSignatureTests`, `IdeoFingerprintTests`, `StuffSlotPolicyTests`, `M3ReviewGapTests`; UC-3 recheck PASS (no `Catalog.Ideology failed` in log after the NRE fix at 26d7add); Ideology pin resolving before/after a game with an allowing ideoligion and precept-change refresh: not evidenced (the Ideology part ran without error but no pinned Ideology build was re-resolved on record; UC-4) · M4: `PinBoardAutoTests` (hidden unresolved keep order, ForgetUnresolved), `PinBoardFixedTests` (Forget predicate, VisibleCount), `M4ReviewGapTests` (VisibleCount Fixed vs Auto, Forget none/all, hidden pins beyond capacity); UC-4: Missing-pins section not shown (nothing missing), so the Forget button, its confirmation and the `IsReady` gating have no in-game evidence -> UC-5 remove-mod test · M5: `UnavailableReasonTests`, `PinMissingRuleTests`, `MissingModRoundTripTests`, `M5ReviewGapTests` (cap with blanks, cross-cause dedup, round trip + Forget); UC-5 research-greyed pin PASS, missing-mod round trip with stress mod PASS (Fixed empty, "?" in edit mode, settings count + Forget, returns in place); no in-game evidence for monolith / grav engine / Biotech mechanitor / precept reasons, nor the Forget confirmation and one-write -> UC-6 |
| FR-15 no duplicates (toggle) | T-3, T-18, T-25, T-26, T-52, T-81 | `PinBoardFixedTests` (toggle), `PinKeyTests`; UC-2, UC-3 · M1: `PinKeyTests` (canonical equality) only; toggle is T-18 · M2: `PinBoardFixedTests` (toggle, unpin in place); UC-2 + UC-2b PASS (pin, unpin, re-pin in first free slot) |
| FR-16 empty-menu hint | T-55, T-58 | UC-4 (journey 1) · M4: no unit test (adapter-only text and layout; Keyed strings checked by T-73); UC-4 PASS (Clear all pins -> empty hint, Auto ring-1 disc); Fixed empty-menu hint with all slots empty, hint with a non-default modifier name and the 360 px wrap at UI scale 2.0: not captured, UC-6 |
| FR-17 global settings, nothing in saves | T-3, T-9, T-19, T-25, T-32, T-66, T-72 | `PinSerializerTests`; grep for Game/World/MapComponent (T-25); UC-2 (second save, restart), UC-5 (remove mod from save) · M1: nothing persisted yet; `PinKeyTests` / `PinKeyRoundTripTests` cover key stability · M2: `PinSerializerTests`, `PinSerializerEdgeTests`, `PinWriteLimitTests`; `scripts/test.sh` Component grep gate; UC-2 PASS (restart + second save) · M5: `PinSerializer*Tests`, `MissingModRoundTripTests`; UC-5 settings writes counted (3), missing-mod round trip PASS; removing Quick Menu from a save and loading it: not recorded -> UC-6 |
| FR-18 Multiplayer-safe | T-32, T-41, T-68 | Source audit (T-68); UC-6 setup (e) when Multiplayer supports 1.7, else blocked per A-2 · M5: source audit only (T-68, ADR 0006); no unit test by design; in-game blocked per A-2 |
| FR-19 pin marker on pinned architect buttons and pinned dropdown entries (vanilla, BAM, …), next-frame update, no per-frame cost | T-87, T-88, T-89, T-90, T-91, T-92, T-75 | `PinnedSetTests` (whole dropdown vs entry, unresolved/opaque, hidden beyond capacity, rebuild/version), `PinnedSetPerfTests` (P-19: 0 bytes, ≤ 100 ns gate), `GlyphsTests` (PinMarker), `StyleTokensTests`; UC-6 FR-19 section (a, b, c/d; dropdown entry; pin/unpin next frame; UI scale 1.0/2.0; fault injection; P-19 in game) |
| NFR-1 no per-frame cost closed; open in one frame; catalog built once < 200 ms | T-5, T-7, T-8, T-10, T-15, T-24, T-35, T-40, T-46, T-49, T-53, T-62, T-71, T-76, T-82, T-84, T-88, T-90, T-91, T-92, T-95 | `ZeroAllocTests`, `LayoutTimingTests`, `PinBoardPerfTests`, `SerializerPerfTests`, `TextureRebuildPerfTests`; in-game P-rows at every UC (UC-2b: P-16 breakdown, texture prewarm, P-9 sub-buckets; P-11c catalog build on (R) and the stress mod in UC-3) · M1: UC-1 P-1, P-2, P-4, P-6, P-7a, P-8, P-9, P-11c, P-12, P-18 all PASS; `MeasureStatsTests`, `SampleRingTests`; 2 Perf-trait tests · M2: `ZeroAllocTests`, `LayoutTimingTests`, `PinBoardPerfTests`, `SerializerPerfTests`, `TextureRebuildPerfTests` (15 Perf tests pass); UC-2/UC-2b P-1, P-6, P-7a, P-8, P-16, P-18 PASS; P-9 p99 attributed to game sound call; P-3 pin prefix never measured; settings-change texture rebuild time not recorded · M4: `ZeroAllocTests` (grid hit 0 B), `LayoutTimingTests` (grid/Auto/sub-menu), `SubMenuLayoutAllocTests`; `scripts/perf.sh` 22 Perf tests pass; UC-4: P-7a median 47 us, P-7c 0.27 ms, P-8 29.8 us median, P-9 +64 B vs control, P-11c 6.2 ms, texture leak check 27 = 27 PASS; slider texture rebuild FAIL -> T-95 · M5: `scripts/perf.sh` 23 Perf tests pass; UC-5: window per frame median 60 us / p99 203 us PASS, texture leak 27 = 27, one 8.2 ms first-open hitch accepted by user; P-3/P-4 with BAM and Mint and P-5 -> T-71 |
| NFR-2 vanilla / BAM / Mint / DLC | T-6, T-8, T-9, T-13, T-26, T-40, T-47, T-67, T-68, T-75, T-87, T-91, T-92 | Self-test on (a), (b), (R); UC-2 (a, b), UC-3 (a, b), UC-5 (c, d), UC-6 (all) · M1: UC-1 S-1/S-4 on (a) and (b); (c) Mint, (d), (R) not run · M5: UC-5 (b) and (d) Mint PASS (self-test `mint=True bam=True effectiveModifier=Alt`); BAM's own Mint-integration error is not ours; (c) and DLC-only setups not separately run -> UC-6 |
| NFR-3 Harmony only dependency | T-1, T-5, T-13, T-74, T-80 | About.xml review and zip listing (T-74, T-80); UC-1 and UC-6 load with Harmony + Quick Menu only · M1: UC-1 loads with Harmony + Quick Menu; `mod/1.7/Assemblies` holds only QuickMenu.dll and QuickMenu.Core.dll; CI green |
| NFR-4 every string Keyed | T-5, T-25, T-30, T-31, T-44, T-55, T-58, T-59, T-65, T-67, T-73 | `scripts/check-strings.sh` in CI (T-73) · M1: none (only dev-mode diagnostic strings exist) · M2: none (check-strings.sh is T-73) · M5: none (`scripts/check-strings.sh` is T-73, M6); new notice/menu strings seen rendering in UC-5 |
| NFR-5 button size; UI scale | T-2, T-12, T-16, T-28, T-29, T-31, T-32, T-81, T-82, T-51, T-59, T-61, T-89, T-91 | `WheelLayoutTests` (all B), `CapacityTests` (960 × 540); UC-1 texture probe, UC-4 screenshots at 1.0 and 2.0 · M1: `WheelLayoutTests`, `WheelLayoutExtremesTests` (all B); UC-1 S-7 screenshots at 1.0 and 1.5 (2.0 refused at 2560x1440) · M2: `TextureSizingTests`, `StyleTokensTests`, `WheelVisualsTests`; UC-2 look PASS at 1.0; 2.0 not yet · M4: `CapacityTests` (960 x 540 and 1920 x 1080), `SettingsPreviewTests`, `M4ReviewGapTests` (FitsScreen inclusive at the margin); UC-4 UI scale 1.0 and 1.5 only (2.0 refused at 2560 x 1440); grid, edit help panel, drag ghost, empty hint at 2.0 not captured -> UC-6/T-77 |
| NFR-6 failures never break the game; log once | T-5, T-19, T-25, T-26, T-65, T-66, T-72, T-91, T-92 | `PinSerializerTests` (bad lines); fault-injection action in UC-6; UC-5 missing-mod test · M1: `PatchGuard`/`PatchSite` verified by build only; UC-1 no errors logged; fault injection is T-72 · M2: `PinSerializerEdgeTests` (bad/oversize lines); UC-2 no errors logged; fault injection is T-72 · M5: `PinMissingRuleTests`, `MissingModRoundTripTests`; UC-5 missing-mod and old-settings loads logged no errors; fault injection still T-72 |
| NFR-7 clean-room | T-4, T-22, T-23, T-28, T-74, T-77, T-89 | Mint string diff and asset audit (T-74); visual reviews · M1: `MaskRasterizerTests`, `MaskRasterizerEdgeTests`, `PngWriterTests` (own assets); audit is T-74 · M2: `MaskSetTests`, `GlyphsTests`, `DiscRimTests`; audit is T-74 |
| NFR-8 layout and pins unit-tested | T-2, T-3, T-16, T-17, T-18, T-19, T-20, T-21, T-81, T-36, T-37, T-50, T-51, T-52, T-90, T-94, T-95 | Whole Core suite, `scripts/test.sh` with ≥ 90 % line coverage in CI · M1: 322 tests, Core line coverage 99.4 %; `CoreIsolationTests` (Core references only the BCL) · M2: 934 functional tests, Core line coverage 99.5 %, 15 Perf tests · M4: 1284 functional tests after the M4 review (1244 before `M4ReviewGapTests`, +40), Core line coverage 99.5 %; uncovered Core lines are single defensive branches (PinKeyRoundTrip, SubMenuLayout, PinSerializer) · M5: 1358 functional tests (1320 before the M5 review, +38 `M5ReviewGapTests`), Core line coverage 99.5 %, 23 Perf tests |

All FR-1 … FR-19 (except FR-7, Won't) and NFR-1 … NFR-8 have at least one task and one test. Revision 2026-10-19 (hold mode dropped, after UC-4): T-63 and T-64 were removed from FR-2, FR-9, FR-11 and NFR-8, and each row still has other tasks and tests; FR-2 now also covers the removal (T-94: grep check, updated toggle tests, old settings file loads in UC-5); the UC-4 slider rebuild FAIL had no task (added T-95 to NFR-1 and FR-6, re-measured in UC-5 by T-71). Revision 2026-10-19 (FR-19 pin marker): FR-19 is covered by T-87 … T-92 and the UC-6 FR-19 section (T-75); its NFR-1 clause ("no measurable per-frame cost or allocation while the architect menu is open") had no budget, so P-19 was added (T-88) with a CI gate (T-90) and an in-game measurement (T-91, UC-6); fault isolation for the two new patch sites is in T-91/T-92 because T-72 audits the sites that exist before them; the README line is added to T-78. Revision 2026-10-19 (picker removal): FR-7 now has no task or test by design; T-38/T-39/T-45 were removed from FR-7, FR-10, FR-15, NFR-1, NFR-2, NFR-4 and NFR-8, and each of those rows still has other tasks and tests; FR-9's use-mode unpin had no task (added to T-81) and the centre-button and greyed-pin fixes had no in-game check after UC-2 (added UC-2b, T-82); the leftover insert-cursor code (`PinBoard.SetCursor`, from T-18) and the `IconAdd`/`Caret` glyphs (from T-23) had no removal task (added to T-52 and T-55). Gaps found and fixed while planning: FR-16 had no owner beyond the renderer (added T-58); FR-17 "removing the mod from a save" had no check (added to UC-5 via T-69); NFR-3 had no load test (added to UC-1 and UC-6); FR-18 had no verification path (added T-68 with the A-2 blocked status); NFR-4 had only "review grep" (made a CI script, T-73).

## Risks and spikes

### Spikes (S-1 … S-7 in the M1 spike build, run by the user once in UC-1; S-8 is an offline M6 spike)
| Spike | Question | Task | How the user runs it | Result goes to |
|---|---|---|---|---|
| S-1 | Does Harmony 3 rc.3 patch every `ProcessInput` override, including one in a mod assembly? | T-8 (+ count in T-9) | self-test on (a) and on (b)/(R) | ADR 0001, 0005 |
| S-2 | Which physical button is index 3/4; IMGUI blind to 3/4; up events for hold mode | T-11 | input probe: press six buttons in order | ADR 0003, default label |
| S-3 | Which `Event` fields show the modifier and left button; idle invocations (P-4); do dropdown-entry clicks reach the element's `ProcessInput`; hotkey path | T-8 | ProcessInput probe with the click sequence in T-13 step 5 | ADR 0005, architecture §6, T-27 go/no-go |
| S-4 | Are keys unique and do all round-trip on a large list? | T-9 | self-test on (a) and (R) | ADR 0004, 0006; possible fix tasks |
| S-5 | How to detect dev-only designators; which `Visible` causes produce which reasons | T-9 | catalog dump | ADR 0006; T-40, T-65 |
| S-6 | Full-screen window: non-pausing, absorbs map clicks, same-frame open, closes on map/world change | T-7 | skeleton wheel steps in T-13 step 7 | ADR 0007 |
| S-7 | Runtime `Texture2D`, loose PNG, 9-slice under Waylith | T-12 | texture probe + 2 screenshots | ADR 0007, design.md §6; T-28 level |

| S-8 (M6, offline, no user session) | Which hook do vanilla and BAM both go through when drawing a designator button and an open dropdown list entry; which gizmo corner is free; how to make "is pinned" O(1) and allocation-free | T-87 | none: the architect inspects the 1.7 `Assembly-CSharp.dll` and the BAM DLL offline; the result is checked in UC-6 | ADR 0009, architecture §6; T-90, T-91, T-92 |

Each spike is time-boxed to one session of agent work; the UC-1 session is about 45–60 minutes for the user. Probe code is deleted in T-14; only the self-test and the patch enumeration stay as production code.

### Risks
| Risk | Likelihood | Impact | Mitigation in this plan |
|---|---|---|---|
| UC-1 shows a blocker (Harmony cannot patch, runtime textures fail, window cannot absorb clicks) | Medium | High (rework of M2) | All spikes run before any dependent M2 task; T-14 recorded the results (all spikes passed in UC-1) |
| With the picker removed, a designator whose `ProcessInput` override cannot be patched (or a mod architect UI that bypasses `ProcessInput`) cannot be pinned at all | Low (S-1: 8 of 8 patched, 0 failed; BAM works in UC-2) | Medium (some mod designators unpinnable) | Self-test patch count and per-method failure log (T-9); checked on (R) in UC-3 and on all setups in UC-6; any failure is reported as a known limit in the CHANGELOG (T-79) and the user is asked (T-85 rewrites the architecture risk row) |
| In-game verification depends on the user's time | High | Medium (milestones wait) | Six grouped sessions plus the short UC-2b follow-up; guides with exact steps and log markers; Core logic is test-covered so sessions check integration, not logic |
| RimWorld 1.7 or Harmony rc updates break the build or patches mid-project | High | Medium | Pinned references; rerun self-test at every UC; record build number in every result |
| BAM or Multiplayer have no 1.7 build | High | Medium | Their checks are recorded as blocked, not failed (A-2); no code depends on them |
| S-3 shows `ProcessInput` is called outside clicks (P-4 ≠ 0) | Low | Medium | T-15/T-71 reopen R-1; the classifier fast path is already zero-alloc and gated (T-24) |
| Auto full rule and UI scale 2.0 make the menu feel cramped | Medium | Low | Settings preview warning (T-59); checked in UC-4 |
| Hard-coded strings slip in | Medium | Low | CI string check (T-73) |
| The FR-19 marker hook runs for every gizmo every frame (architect buttons and also pawn/building command bars), unlike the click-only pin prefix | Medium | Medium (frame cost with many gizmos) | Repaint-only, one type check and one identity `Contains` per call (T-91); P-19 budget ≤ 100 ns and 0 bytes with a strict CI gate (T-88, T-90) and an in-game measurement in UC-6 |
| BAM (or another architect UI) draws buttons or dropdown lists through a path the chosen hook does not reach | Medium | Low (no marker in that UI; pinning still works) | Checked offline against the BAM DLL before coding (T-87); any unsupported UI is told to the user and listed as a known limit (T-92, T-79) |
| GitHub repo or push not available | Medium | Low | T-1 is independent; local `scripts/test.sh`/`perf.sh` remain the gate until CI runs |

## Decisions (user, 2026-10-19)

1. **GitHub (T-1)**: yes — create public repo `Sixdd6/rimworld-quick-menu` at the start of M1 and push so CI runs.
2. **Real mod list**: yes — the user saves a named 1.7 mod list (with BAM if it loads).
3. **Skeleton trigger**: Mouse 4 hard-coded in M1 with a dev-mode switch; binding UI in M2.
4. **Stress mod**: yes — `sixdd.QuickMenuStress`, test-only, never shipped.
5. **Release version**: **0.9.0** until RimWorld 1.7 and Harmony 3 are final.
6. **Keyed files split per area**: accepted.
7. **Frame Profiler overhead**: measured in UC-1 (T-15).
8. **Picker removed (after UC-2)**: no picker, no search, no "+"/Add, no insert cursor; FR-7 is Won't. Pins come only from modifier+click in the architect menu; modifier+click on a wheel slot unpins.


- **Decisions 11–13 (user, 2026-10-19)**: 11 — when only single entries of a dropdown are pinned, the dropdown button shows a **smaller** marker (whole-dropdown pin = normal marker), and (decision 14, after the T-89 preview) that marker is **white** to say "a pin is inside"; 12 — marker in the **architect menu only** (not on copy/build buttons of selected things); 13 — pins hidden beyond the current capacity are **not** marked.

## Open questions (revision 2026-10-19)

- **Question 8**: resolved — the menu stays open after an unpin (implemented in T-81, 00f1671).
- **Question 9**: resolved — spec journey 5 now says Ctrl+click unpins (FR-9).
- **Question 10**: resolved — design.md (099d1c0) now says modifier+click in use mode unpins (user decision).
- **Question 11 (FR-19)**: when only a single entry of a dropdown is pinned (not the whole dropdown), should the dropdown's own button in the architect also show a marker (for example a hint that something inside is pinned)? The plan assumes **no**: the button is marked only when the whole dropdown is pinned, and the entry is marked inside the open list (T-90 rules).
- **Question 12 (FR-19)**: a pinned designator can also appear outside the architect menu (for example the Install/Reinstall or copy-build gizmos on a selected building, if they are the same designator instance). Should the marker show there too? The plan assumes the marker appears wherever the same pinned designator instance is drawn as a gizmo; T-87 reports where that happens so you can decide before T-91.
- **Question 13 (FR-19)**: pins hidden because the ring count was reduced (Fixed sizing) are still pinned. The plan marks them too (a modifier+click would unpin them). Confirm.
