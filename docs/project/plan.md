# Implementation Plan: rimworld-quick-menu (Quick Menu)

- **Status**: approved (2026-10-19)
- **Spec**: [spec.md](spec.md) · **Architecture**: [architecture.md](architecture.md) · **ADRs**: [decisions/](decisions/) · **Design**: [design.md](design.md) · **Performance**: [performance.md](performance.md) · **Environment**: [environment.md](environment.md)

## Planning notes

- **Agents cannot run the game.** Every in-game check is done by the user in a few grouped **user sessions** (UC-1 … UC-6), one at the end of each milestone. A task that needs the game says **In-game check: UC-n (user)**. Its acceptance criteria marked *(UC-n)* are ticked only after the user reports back. Everything else is verified by `scripts/test.sh`, `scripts/perf.sh`, `dotnet build` and `dotnet format --verify-no-changes`.
- **Each user session has a guide** (written by the test-engineer in `docs/project/test-checklist.md`, section per session). It gives exact steps, the setups to use, what to look at, and what to send back. Every in-game output the session needs is written to the game log between `[QuickMenu] BEGIN <name>` and `[QuickMenu] END <name>` markers, so the user can copy blocks without reading them.
- **Setups** (architecture §9): (a) vanilla + all DLC; (b) + Better Architect Menu; (c) + Dubs Mint Menus; (d) + BAM + Mint; (e) + Multiplayer when it supports 1.7 (A-2); (R) the user's real mod list that loads on 1.7.
- **Done for every task**: tests written first; `scripts/test.sh` (Core ≥ 90 % lines) and `scripts/perf.sh` pass; build has 0 warnings; format check passes; every new player-facing string is Keyed (NFR-4); every patch body and OnGUI entry is wrapped by `PatchGuard` (NFR-6) once T-5 exists; no LINQ, string building or closures in per-frame paths.
- **Keyed files**: strings go in `mod/Languages/English/Keyed/`, split per area (`QuickMenu.Menu.xml`, `QuickMenu.Settings.xml`, `QuickMenu.Picker.xml`, `QuickMenu.Messages.xml`, `QuickMenu.Notices.xml`, `QuickMenu.Debug.xml`) so parallel tasks do not edit the same file. The game loads every file in the folder. Keys stay prefixed `QuickMenu.` (architecture §6 names one file; see Question 6).
- **`parallel-ok`** on a task means it has no file overlap with the other `parallel-ok` tasks of the same milestone, and it can start as soon as its listed dependencies are done.
- **Gated mode**: each milestone ends with its user session and the user's approval before the next milestone starts.

## Milestones
| ID | Milestone | Goal / demoable outcome | Tasks | Count |
|---|---|---|---|---|
| M1 | Walking skeleton + spike build | The mod loads on 1.7.4982 with Harmony 3. Mouse 4 on a colony map opens a crude wheel with two hard-coded pins (Mine, Wall); clicking one selects the designator. A dev-mode spike build answers S-1 … S-7 in one user session (UC-1); results go into the ADRs. CI is green on GitHub | T-1 … T-15 | 15 |
| M2 | Pin by click, use the wheel | Ctrl+click any designator in vanilla or BAM pins it (message; full; toggle). The bindable trigger opens the styled Fixed wheel centred on the mouse, dodging edges; clicking a slot activates it like the architect. Pins and settings persist across saves and restarts | T-16 … T-35 | 20 |
| M3 | Sub-menus, materials, picker | Whole dropdowns pin and fan out as a sub-ring; stuff builds open a material ring (last-used first); edit mode shell with Add opens a searchable picker of every architect designator, independent of the architect UI | T-36 … T-49 | 14 |
| M4 | Edit mode, grid, Auto sizing | Drag to swap, right-click to clear, insert cursor, help panel, full state; grid layout with sub-grids; Auto sizing; full layout settings with live preview; empty-menu hint | T-50 … T-62 | 13 |
| M5 | Hold mode, availability, compatibility | Hold-and-release with 150 ms hover-open; greyed slots with the game's (or composed) reason; missing-mod pins kept and restored; Mint detection with Alt default and notices; first-run notice; Multiplayer-safe audit | T-63 … T-71 | 9 |
| M6 | Hardening and release prep | Fault isolation verified, string and clean-room audits, final performance pass, final regression on all setups, Preview.png, README, CHANGELOG, Workshop description, release package (publishing is the separate gated Phase 7) | T-72 … T-80 | 9 |
| | | | **Total** | **80** |

**Critical path** (milestone gates included): T-3 → T-6 → T-7 / T-9 → T-13 **[UC-1]** → T-14 → T-28 → T-29 → T-32 → T-33 **[UC-2]** → T-41 → T-43 → T-47 **[UC-3]** → T-56 → T-57 → T-60 **[UC-4]** → T-64 → T-69 **[UC-5]** → T-72 → T-75 **[UC-6]** → T-77 → T-80. Each user session sits on the path, so how soon you can run a session decides the schedule more than agent work does.

## User in-game sessions

| Session | When | Setups | What the user does (details in the session guide) | Feeds |
|---|---|---|---|---|
| **UC-1** Spike session | End of M1 (T-13) | (a), then (b) or (R) | Load the spike build, run 5 dev-mode debug actions, try the skeleton wheel, take 4 screenshots, paste the log blocks back. About 45–60 min | T-14, T-15 (ADRs 0001/0003/0005/0006/0007, design.md §6, performance.md §5) |
| **UC-2** | End of M2 (T-33) | (a), (b) | Bind the trigger, pin by Ctrl+click in vanilla and BAM, use the wheel, edge-dodge at corners, restart and load a second save, measurement actions, screenshots | T-34, T-35 |
| **UC-3** | End of M3 (T-47) | (a), (b), (R), stress mod | Sub-rings, material rings, picker search and pinning, Ideology designators, measurement actions | T-48, T-49 |
| **UC-4** | End of M4 (T-60) | (a) | Edit mode, grid, Auto, settings preview, UI scale 1.0 and 2.0 screenshots | T-61, T-62 |
| **UC-5** | End of M5 (T-69) | (a), (c), (d); remove-mod test | Hold mode, greyed slots, missing-mod pins, Mint Alt default and notices, first-run notice, removing the mod from a save | T-70, T-71 |
| **UC-6** | M6 (T-75) | (a)–(d), (R), (e) if available | Full regression checklist, fault-injection action, final measurements, Preview screenshots | T-76, T-77, release |

## Tasks

Task states: `[ ]` todo · `[~]` in progress · `[x]` done · `[!]` blocked

### M1: Walking skeleton + spike build

- [ ] **T-1**: Put the repository on GitHub and get CI green · `parallel-ok`
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

- [ ] **T-2**: Core geometry, ring capacity and Fixed wheel layout · `parallel-ok`
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

- [ ] **T-3**: Core `PinKey` grammar, format and parse · `parallel-ok`
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

- [ ] **T-4**: Core `MaskRasterizer` v0 and PNG export (for spike S-7) · `parallel-ok`
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

- [ ] **T-5**: Adapter foundation: LogOnce, PatchGuard, Harmony bootstrap, skeleton trigger poller
  - **Owner**: developer
  - **Requirements**: FR-1 (skeleton), NFR-1, NFR-3, NFR-6 (ADR 0001, 0003)
  - **Depends on**: —
  - **Files**: `src/QuickMenu/QuickMenuMod.cs`, `src/QuickMenu/Diagnostics/{Log,PatchGuard}.cs`, `src/QuickMenu/Input/TriggerPoller.cs`, `src/QuickMenu/Diagnostics/Spike/SkeletonTrigger.cs`, `mod/Languages/English/Keyed/QuickMenu.Debug.xml`
  - **Acceptance criteria**:
    - [ ] Given the mod ctor, when it runs, then Harmony id `sixdd.QuickMenu` applies the postfix on `RimWorld.UIRoot_Play.UIRootUpdate` inside try/catch, and a failure is logged once with `[QuickMenu]` and does not stop loading.
    - [ ] Given `PatchGuard.Run(site, body)`, when the body throws, then the error is logged once per site, the site is disabled for the session, and later calls return without running the body (unit-testable part kept in the adapter, verified in game by T-72).
    - [ ] Given the skeleton trigger (Mouse index 3; debug action "Quick Menu: cycle skeleton trigger" switches Mouse 4 / Mouse 5 / Middle), when the button is pressed on a colony map with no modal window and not in world view, then `MenuController.Open` is called once; otherwise nothing happens. The postfix body does no allocation (early return, one `Input` call).
    - [ ] *(UC-1)* Given setup (a), when the game loads, then the log shows no `[QuickMenu]` errors and the mod appears in the mod list.
  - **Tests**: build; in-game UC-1 (load, trigger).
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [ ] **T-6**: `DesignatorCatalog` v1 (static part) and Core `PinKeyFactory`
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

- [ ] **T-7**: Skeleton menu window (spike S-6)
  - **Owner**: developer
  - **Requirements**: FR-3 (centred), FR-13, NFR-1 (P-6) (ADR 0007)
  - **Depends on**: T-2, T-5, T-6
  - **Files**: `src/QuickMenu/UI/{MenuController,QuickMenuWindow}.cs`, `src/QuickMenu/Diagnostics/Spike/SkeletonPins.cs`
  - **Time box**: one session. Output: window flags and layer recorded for T-14.
  - **Acceptance criteria**:
    - [ ] Given the trigger fires, when `MenuController.Open` runs, then a full-screen transparent `Window` (no background, no shadow, `forcePause=false`, `closeOnCancel`, layer per ADR 0007) is added in the same frame, and dev mode logs `Time.frameCount` at the trigger and at the first `DoWindowContents`.
    - [ ] Given the window, then it draws a crude 1-ring wheel (flat boxes, designator icons) from `WheelLayout` centred on the mouse, with two hard-coded pins `order:RimWorld.Designator_Mine` and `build:thing/Wall` resolved through the catalog.
    - [ ] Given a slot click, then `designator.ProcessInput(Event.current)` runs and the window closes. Given Esc, a click outside the slots, the trigger again, a map change or the world view, then it closes.
    - [ ] *(UC-1)* Given the menu is open, when the user clicks the map outside the slots, then no designation or selection happens on the map, the game is not paused, and the two frame numbers are equal.
  - **Tests**: build; in-game UC-1.
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [ ] **T-8**: Spike S-1 and S-3: patch every `ProcessInput` override in observe mode · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-8, FR-10, NFR-1 (P-4), NFR-2 (ADR 0005)
  - **Depends on**: T-5
  - **Files**: `src/QuickMenu/Pinning/PinClickPatch.cs` (enumeration and patching, production), `src/QuickMenu/Diagnostics/Spike/ProcessInputProbe.cs`
  - **Time box**: one session. Output: patch count, event field table, idle count, dropdown-entry path → T-14.
  - **Acceptance criteria**:
    - [ ] Given all loaded assemblies, when the mod starts, then every declared, non-abstract `ProcessInput(Event)` in `Designator` and its subclasses gets the shared prefix `static bool Prefix(Designator __instance, Event ev)`, each in its own try/catch, and the count of patched / failed methods (with type and assembly) is stored. The prefix always returns `true` in this build.
    - [ ] Given debug action "Quick Menu: ProcessInput probe" is on, when any patched method runs, then it logs instance type, `ev.type`, `ev.rawType`, `ev.button`, `ev.isKey`, `ev.control/alt/shift`, `Input.GetKey` for both Ctrl/Alt/Shift keys, and whether the event is already used; and it counts invocations per frame.
    - [ ] *(UC-1)* The report answers: patched count including at least one mod-assembly override (setup b or R); idle invocations over 600 frames with the architect open (P-4, target 0); which fields reliably show Ctrl/Alt and the left button; whether clicking an entry inside an open dropdown menu calls the element's `ProcessInput` (decides T-27); whether hotkeys arrive with `isKey`.
  - **Tests**: build; in-game UC-1 log.
  - **In-game check**: UC-1 (user)
  - **Size**: M

- [ ] **T-9**: Spike S-4 and S-5: "Quick Menu: self-test" debug action
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

- [ ] **T-10**: Dev-mode `MeasureScope`, closed-state and window counters
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-1, P-2, P-6, P-7a, P-8, P-9, P-18; performance.md T-6)
  - **Depends on**: T-5, T-7, T-9
  - **Files**: `src/QuickMenu/Diagnostics/{MeasureScope,PerfCounters,ControlRunWindow}.cs`, `src/QuickMenu/Input/TriggerPoller.cs` (scope around the body), `src/QuickMenu/UI/QuickMenuWindow.cs` (scopes around the open path, `DoWindowContents`, `WindowUpdate`), `src/QuickMenu/Diagnostics/SelfTest.cs` (print counters)
  - **Acceptance criteria**:
    - [ ] Given dev mode is off, then the poller and the window pay only one cached bool check for measurement.
    - [ ] Given the window scopes and debug action "Quick Menu: control window" (an empty full-screen transparent window making the same `Widgets`/`GUI` calls, for the P-9 control run), then open-path ms, per-frame draw ms and allocated bytes are printed by "Quick Menu: perf report". T-32 keeps these scopes when it replaces the skeleton window.
    - [ ] Given debug action "Quick Menu: measure closed frames", when it runs for 10,000 frames, then it logs the median, p99 and max µs of the poller body and the total allocated bytes (`GC.GetAllocatedBytesForCurrentThread`), into preallocated counters (the scope itself allocates nothing).
    - [ ] Given the self-test, then it also prints mod ctor time (settings, detection, patching).
  - **Tests**: build; in-game UC-1.
  - **In-game check**: UC-1 (user)
  - **Size**: S

- [ ] **T-11**: Spike S-2: input probe · `parallel-ok`
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

- [ ] **T-12**: Spike S-7: texture probe window · `parallel-ok`
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

- [ ] **T-13**: UC-1 spike session guide and run
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

- [ ] **T-14**: Record spike results; remove spike-only code
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

- [ ] **T-15**: Performance baseline from UC-1 · `parallel-ok`
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

- [ ] **T-16**: Core `EdgeDodger` and wheel bounding box · `parallel-ok`
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

- [ ] **T-17**: Core `HitTester` for the wheel and hub · `parallel-ok`
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

- [ ] **T-18**: Core `PinBoard` (Fixed sizing) · `parallel-ok`
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

- [ ] **T-19**: Core `PinSerializer` v1 · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-14, FR-17, NFR-6, NFR-8
  - **Depends on**: T-3
  - **Files**: `src/QuickMenu.Core/Pins/PinSerializer.cs`, `tests/QuickMenu.Core.Tests/Pins/PinSerializerTests.cs`
  - **Acceptance criteria**:
    - [ ] Given a board, when `ToLines` then `FromLines` runs, then slot, key, packageId and label round-trip, with `|` and `\` escaped.
    - [ ] Given a line with an unknown key prefix, then it is kept verbatim and written back unchanged. Given a malformed line, then it is skipped and reported once (count returned), never thrown.
  - **Tests**: unit `PinSerializerTests`.
  - **Size**: S

- [ ] **T-20**: Core `TriggerBinding`, `ModifierPolicy` and `PinClickClassifier` · `parallel-ok`
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

- [ ] **T-21**: Core `TriggerStateMachine` (toggle mode) · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-2, FR-9 (enter/exit), FR-11 (back), NFR-8 (design.md close and cancel rules)
  - **Depends on**: T-17
  - **Files**: `src/QuickMenu.Core/Input/{TriggerStateMachine,TriggerEvent,MenuCommand,MenuState}.cs`, `tests/QuickMenu.Core.Tests/Input/TriggerStateMachineToggleTests.cs`
  - **Acceptance criteria**:
    - [ ] Given closed, when the trigger is pressed, then `Open`; given open, when pressed again, then `Close`.
    - [ ] Given the main level, then Esc, right-click, left-click outside and left-click `HubClose` give `Close`; left-click (down and up) on `HubEdit` gives `EnterEdit`; a click on `Slot(i)` gives `Activate(i)` or `OpenSub(i)` per slot kind.
    - [ ] Given a satellite open, then Esc, right-click, click outside and click on its hub give `Back`.
    - [ ] Given edit mode, then Esc and `HubClose` (Done) give `ExitEdit`, a left-click outside gives nothing, and a second Esc closes. Given map lost, then `Close` in every state.
  - **Tests**: unit `TriggerStateMachineToggleTests` (one test per row of the close and cancel table).
  - **Size**: M

- [ ] **T-22**: `MaskRasterizer` full shape set · `parallel-ok`
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

- [ ] **T-23**: `MaskRasterizer` badges and hub icons
  - **Owner**: visual-designer
  - **Requirements**: NFR-7, NFR-8 (design.md S4, S4a, §3)
  - **Depends on**: T-22
  - **Files**: `src/QuickMenu.Core/Render/Glyphs.cs`, `tests/QuickMenu.Core.Tests/Render/GlyphsTests.cs`
  - **Acceptance criteria**:
    - [ ] Given the glyph set, then `SubMenuBadge`, `Caret`, `Lock`, `IconClose`, `IconEdit`, `IconDone`, `IconAdd` are rasterized in their final orientation on a 64 px grid with a 4 px margin and 6 px strokes (no runtime rotation needed).
    - [ ] Each glyph has a coverage test (alpha mass in the expected region, empty margin) and is deterministic.
  - **Tests**: unit `GlyphsTests`.
  - **Size**: M

- [ ] **T-24**: Perf gates v1 and benchmarks for the hot paths
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

- [ ] **T-25**: Settings model and `PinService`
  - **Owner**: developer
  - **Requirements**: FR-5, FR-15, FR-17, NFR-4, NFR-6 (architecture §4; ADR 0004)
  - **Depends on**: T-6, T-14, T-18, T-19, T-20
  - **Files**: `src/QuickMenu/Settings/QuickMenuSettings.cs`, `src/QuickMenu/Pinning/PinService.cs`, `src/QuickMenu/QuickMenuMod.cs`, `mod/Languages/English/Keyed/QuickMenu.Messages.xml`
  - **Acceptance criteria**:
    - [ ] Given `ExposeData`, then every field of architecture §4 is saved with its default, `schemaVersion` = 1, pins as `PinSerializer` lines; a missing or corrupt file loads defaults with one warning.
    - [ ] Given startup after the catalog build, then stored pins are resolved against the key map once; opening the menu does no catalog work.
    - [ ] Given `PinService.Toggle(entry)`, then the result shows the design.md S9 message with its sound (`Tick_High`, `Tick_Low`, or `ClickReject` + full text), and settings are written once.
    - [ ] Given no GameComponent, WorldComponent, MapComponent or def is added, then nothing is written into saves (code check: grep for those base types is empty).
  - **Tests**: unit coverage is in T-18/T-19; build; grep check in CI script `scripts/test.sh` stays green; in-game UC-2 (persist across restart and saves).
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [ ] **T-26**: Pin by modifier+click (production prefix)
  - **Owner**: developer
  - **Requirements**: FR-8, FR-10, FR-15, NFR-1, NFR-2, NFR-6 (ADR 0005)
  - **Depends on**: T-8, T-14, T-25
  - **Files**: `src/QuickMenu/Pinning/PinClickPatch.cs`, `src/QuickMenu/QuickMenuMod.cs` (cached effective modifier)
  - **Acceptance criteria**:
    - [ ] Given the effective modifier is cached at startup and on settings change, when the prefix runs, then it reads only the cached value and the event (fields chosen in T-14), never `ModsConfig`.
    - [ ] Given a left click with the modifier held on any designator (button of a dropdown included), when the prefix runs, then `PinService.Toggle(key of __instance)` runs and the prefix returns `false` (no float menu, no selection). A re-entrancy guard stops a base call from toggling twice.
    - [ ] Given a hotkey (key event) or a click without the modifier, then the original runs unchanged.
    - [ ] Given the prefix throws, then `PatchGuard` logs once, disables the prefix for the session, and the original runs.
    - [ ] *(UC-2)* Given setups (a) and (b), when the user Ctrl+clicks Wall, Mine and a dropdown button, then each is pinned with the message; Ctrl+click again unpins; the 19th pin with 2 rings shows "The quick menu is full …".
  - **Tests**: classifier unit tests (T-20); in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [ ] **T-27**: Pin a single entry inside an open dropdown list (only if S-3 needs it)
  - **Owner**: developer
  - **Requirements**: FR-8, FR-10 (user decision U-3; ADR 0005)
  - **Depends on**: T-26
  - **Files**: `src/QuickMenu/Pinning/DropdownEntryPatch.cs`, `docs/project/architecture.md` §6 (third patch site)
  - **Acceptance criteria**:
    - [ ] Given T-14 recorded that entry clicks already reach the element's `ProcessInput`, then this task is closed as "not needed" with a note, and no code is added.
    - [ ] Otherwise: given a postfix on `RimWorld.Designator_Dropdown.GetDesignatorSelectAction`, when the user holds the modifier and clicks an entry, then the entry's own key is toggled and the original action does not run; without the modifier, the action is unchanged.
    - [ ] *(UC-2)* Ctrl+click on an entry in an open dropdown pins only that entry; Ctrl+click on the dropdown button pins the whole dropdown.
  - **Tests**: in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: S

- [ ] **T-28**: Runtime textures and style tokens
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7 (design.md §2, §6; ADR 0007)
  - **Depends on**: T-14, T-22, T-23
  - **Files**: `src/QuickMenu/UI/{Textures,QMStyle}.cs`; only if S-7 chose the PNG level: `scripts/export-masks.sh`, `mod/Textures/QuickMenu/UI/*.png`, `docs/project/assets.md`
  - **Acceptance criteria**:
    - [ ] Given startup, then every mask is uploaded once at the level chosen in T-14 (bilinear, no mipmaps, clamp), and rebuilt only when button size or UI scale changes (≤ 5 ms), never per frame.
    - [ ] Given `QMStyle`, then every colour token of design.md §2 exists with its exact value, and `danger` uses `ColorLibrary.RedReadable` only under the stated 3:1 rule. No drawing code uses colour literals.
    - [ ] If the PNG level was chosen: the PNGs are exported by our rasterizer, committed, and each has an assets.md row (MIT, made by the project).
  - **Tests**: build; in-game UC-2 visual review.
  - **In-game check**: UC-2 (user)
  - **Size**: S

- [ ] **T-29**: Wheel renderer: disc, base slot states, hub, label plate
  - **Owner**: visual-designer
  - **Requirements**: FR-4, FR-13 (look), NFR-5 (design.md S1, S4 states 1/2/3/6, S4a)
  - **Depends on**: T-16, T-17, T-28
  - **Files**: `src/QuickMenu/UI/{MenuRenderer,SlotRenderer,HubRenderer,PlateRenderer}.cs`
  - **Acceptance criteria**:
    - [ ] Given a placement array, origin and scale, then the renderer draws disc + rim + outlines, slots, hub, label plate in that order, with no allocation per frame (labels cached per open).
    - [ ] Slot states Normal, Hover, Pressed and Empty (Fixed, use mode) match design.md S4 exactly; icons are drawn with `Command.DrawIcon` in a 0.75 B square.
    - [ ] Hub: Close main area and Edit sub-button per S4a; no hover highlight until the mouse moved ≥ 4 px since open; the Edit sub-button is drawn only when a flag says edit mode exists (off until T-44).
    - [ ] The renderer accepts a scale for the settings preview (used in T-59).
  - **Tests**: build; visual review T-34 on UC-2 screenshots.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [ ] **T-30**: Trigger binding: capture dialog, binding row, poller reads the setting
  - **Owner**: developer
  - **Requirements**: FR-1, NFR-1, NFR-4 (ADR 0003; design.md S7 binding, capture dialog)
  - **Depends on**: T-14, T-20, T-25
  - **Files**: `src/QuickMenu/Settings/{BindingCaptureWindow,BindingRow}.cs`, `src/QuickMenu/Input/TriggerPoller.cs`, `mod/Languages/English/Keyed/QuickMenu.Settings.xml`
  - **Acceptance criteria**:
    - [ ] Given the capture dialog, when a key is pressed (`KeyDown`) or a middle/side button (polled `GetMouseButtonDown(2..4)` in `WindowUpdate`), then it is stored; Esc cancels; left or right shows "Left and right click can't open the menu." with `ClickReject`.
    - [ ] Given the key is also a game key binding, then it is accepted and the row shows "{key} is also used for: {binding}." in `danger`.
    - [ ] Given [Clear], then the row shows "Unbound" and the poller returns early; given [Use middle mouse instead], then the binding is `Mouse(2)` and the button is hidden.
    - [ ] Given the poller, then it uses the stored binding (skeleton trigger removed) and still allocates nothing.
    - [ ] *(UC-2)* Mouse 4, Mouse 5, middle and a key each open the menu after binding; unbound opens nothing.
  - **Tests**: `TriggerBindingTests` (T-20); in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [ ] **T-31**: Settings page v1
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

- [ ] **T-32**: Production menu: controller, toggle window, plain activation
  - **Owner**: developer
  - **Requirements**: FR-2 (toggle), FR-3, FR-5 (Fixed), FR-6, FR-13, FR-17, FR-18, NFR-1, NFR-5 (ADR 0006, 0007)
  - **Depends on**: T-21, T-25, T-29, T-31
  - **Files**: `src/QuickMenu/UI/{MenuController,QuickMenuWindow}.cs`, `src/QuickMenu/Activation/DesignatorActivator.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`; delete `SkeletonPins`
  - **Acceptance criteria**:
    - [ ] Given open, then the layout comes from a cache rebuilt only on settings or UI screen size change, is edge-dodged, and the window is added in the trigger frame.
    - [ ] Given input, then every IMGUI event is mapped to a `TriggerEvent`, hit-tested by Core, and the `MenuCommand` is executed; the window holds no menu logic of its own.
    - [ ] Given `Activate(i)` on a plain designator (order or build), then `DesignatorActivator` calls `ProcessInput(Event.current)` and the window closes (stuff builds and dropdowns use the vanilla menus until T-41/T-43).
    - [ ] Given hover, then `Mouseover_Command` plays once on enter and the tooltip is the designator's own; labels and tooltips are cached per open; `Disabled` designators use the base disabled state until T-65.
    - [ ] Given middle mouse is the trigger, then camera drag while the menu is open is prevented if T-14 found it disruptive (`preventCameraMotion`).
    - [ ] *(UC-2)* Given setup (a), the user opens the wheel near each corner (fully on screen), places a wall and designates mining from it, and pins are the same after a restart and in a second save.
  - **Tests**: Core tests cover logic; in-game UC-2.
  - **In-game check**: UC-2 (user)
  - **Size**: M

- [ ] **T-33**: UC-2 checklist, session guide and run
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

- [ ] **T-34**: M2 visual review · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7
  - **Depends on**: T-33
  - **Files**: `docs/project/design/screens/m2/*` (user screenshots), `docs/project/design.md` (review notes)
  - **Acceptance criteria**:
    - [ ] Given the UC-2 screenshots, then each drawn state is compared with design.md S1/S4/S4a and the mockups; differences are fixed or listed as follow-ups; the user confirms the look.
  - **Tests**: screenshot review.
  - **Size**: S

- [ ] **T-35**: M2 performance record · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-1, P-2, P-5, P-6, P-7a, P-8, P-9 with control run)
  - **Depends on**: T-33
  - **Files**: `docs/project/performance.md` §5
  - **Acceptance criteria**:
    - [ ] Given UC-2 numbers, then each metric has a row and a pass/fail at 50 % of budget; any failure becomes a `perf:` task in M3.
  - **Tests**: none (measurement record).
  - **Size**: S

### M3: Sub-menus, materials, picker

- [ ] **T-36**: Core `SubMenuLayout` (satellite and sub-grid) · `parallel-ok`
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

- [ ] **T-37**: Core `StuffOrdering` and `ModDropdownMatcher` · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-10, FR-12, FR-14, NFR-8 (ADR 0004, 0006)
  - **Depends on**: T-3
  - **Files**: `src/QuickMenu.Core/Materials/StuffOrdering.cs`, `src/QuickMenu.Core/Pins/ModDropdownMatcher.cs`, `tests/QuickMenu.Core.Tests/{Materials/StuffOrderingTests,Pins/ModDropdownMatcherTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given options and a last-used id, then last-used is index 0 and the rest keep the input order; 1 option → skip; 0 → none.
    - [ ] Given a stored `moddrop` key and candidates, then only the same type matches, Jaccard of child keys ≥ 0.5, same category breaks ties, then the first; no match → none.
  - **Tests**: unit tests above.
  - **Size**: S

- [ ] **T-38**: Core `SearchIndex` · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-7, NFR-1, NFR-8
  - **Depends on**: —
  - **Files**: `src/QuickMenu.Core/Search/{SearchEntry,SearchIndex}.cs`, `tests/QuickMenu.Core.Tests/Search/SearchIndexTests.cs`
  - **Acceptance criteria**:
    - [ ] Given entries, when `Query(text, results)` runs, then matching is case-insensitive per token on label, category and mod name (all tokens must match), in stable index order, into the caller's list (cleared first).
    - [ ] Given `includeDev` false, then `DevOnly` entries are excluded; children carry `ParentId`.
    - [ ] Given an empty query, then all non-filtered entries are returned.
  - **Tests**: unit `SearchIndexTests` (non-ASCII, punctuation, multiple tokens, mod-name-only match).
  - **Size**: M

- [ ] **T-39**: Synthetic catalog, search perf gates and benchmarks
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-11a, P-11b, P-13, P-14, scaling)
  - **Depends on**: T-38
  - **Files**: `tests/Shared/SyntheticCatalog.cs`, `tests/QuickMenu.Core.Tests/Perf/SearchPerfTests.cs`, `tests/QuickMenu.Core.Benchmarks/SearchBenchmarks.cs`
  - **Acceptance criteria**:
    - [ ] Given `Generate(seed, mods, designators)`, then W-1 (1, 500, 2000) and W-1x (1, 2000, 20000) are reproducible per performance.md §2 rules.
    - [ ] Gates: W-1x build median of 5 fails > 200 ms (warn > 100 ms); W-1 fails > 30 ms; W-1 retained memory fails > 5 MB; query with a reused list allocates 0 bytes; W-1x/W-1 ratio fails > 15.
  - **Tests**: `scripts/perf.sh`.
  - **Size**: M

- [ ] **T-40**: Catalog v2: search entries, dev-only flag, Ideology refresh, mod-dropdown resolve
  - **Owner**: developer
  - **Requirements**: FR-7, FR-10, FR-14, NFR-1 (P-7c, P-11c), NFR-2 (ADR 0006)
  - **Depends on**: T-14, T-37, T-38
  - **Files**: `src/QuickMenu/Catalog/{DesignatorCatalog,CatalogEntry,IdeoSignature}.cs`, `src/QuickMenu/Diagnostics/SelfTest.cs`
  - **Acceptance criteria**:
    - [ ] Given the startup build, then each entry gets label, category label, mod name, `ParentId`, and `DevOnly` by the rule from T-14 (S-5), and `SearchIndex.Build` runs once in the same pass; the self-test reports total ms.
    - [ ] Given a menu or picker open, then the signature (game, primary ideo, precept count) is compared in O(1) and the Ideology entries are rebuilt only when it changed.
    - [ ] Given a `moddrop:` key with no exact match, then `ModDropdownMatcher` resolves it; the stored key is not rewritten.
    - [ ] *(UC-3)* The self-test on (R) and the stress mod shows build ≤ 100 ms (50 % of 200 ms) and 1 build per session.
  - **Tests**: Core tests (T-37, T-38); in-game UC-3 self-test.
  - **In-game check**: UC-3 (user)
  - **Size**: M

- [ ] **T-41**: `StuffOptions` and full `DesignatorActivator`
  - **Owner**: developer
  - **Requirements**: FR-12, FR-13, FR-18 (ADR 0006)
  - **Depends on**: T-32, T-37
  - **Files**: `src/QuickMenu/Catalog/StuffOptions.cs`, `src/QuickMenu/Activation/DesignatorActivator.cs`
  - **Acceptance criteria**:
    - [ ] Given a stuff build, then the options are the counted stuffs on the map that `IsStuff` and `CanMake` the def (all in god mode), ordered by `StuffOrdering` with the designator's current stuff first.
    - [ ] Given `Activate(entry, stuff)`, then `SetStuffDef(stuff)`, the interaction check, `CurActivateSound`, `DesignatorManager.Select`; Ideology style variants keep the game's default style.
    - [ ] Given a dropdown element, then it is activated the same way and `SetActiveDesignator` is called on the dropdown so the architect shows it as last used.
    - [ ] Given no options, then the slot reports "none" with the vanilla "NoStuffsToBuildWith" text.
    - [ ] The activator calls no `Designate*` method itself (FR-18).
  - **Tests**: `StuffOrderingTests`; in-game UC-3.
  - **In-game check**: UC-3 (user)
  - **Size**: M

- [ ] **T-42**: Slot states for sub-menus, materials, greyed, open parent; dimmed menu · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-10, FR-11, FR-12, FR-14 (look) (design.md S3, S4 states 4, 5, 12, 13, 14)
  - **Depends on**: T-29
  - **Files**: `src/QuickMenu/UI/{SlotRenderer,SatelliteRenderer}.cs`
  - **Acceptance criteria**:
    - [ ] States 4 (last-used), 5 (greyed with lock), 12 (sub-menu badge at 1:30), 13 (material dot at 4:30 in the last-used stuff colour or `textMuted`) and 14 (open parent) are drawn per S4 and combine as stated; material options show the stuff icon in its colour.
    - [ ] The main menu draws at 45 % opacity while a satellite is open.
  - **Tests**: build; visual review T-48.
  - **In-game check**: UC-3 (user)
  - **Size**: S

- [ ] **T-43**: Sub-ring and material ring in the window
  - **Owner**: developer
  - **Requirements**: FR-10, FR-11, FR-12, FR-13 (design.md S3)
  - **Depends on**: T-36, T-41, T-42
  - **Files**: `src/QuickMenu/UI/{QuickMenuWindow,MenuController,SatelliteController}.cs`
  - **Acceptance criteria**:
    - [ ] Given a pinned dropdown slot (`dropdown:` or `moddrop:`), when it is clicked, then its options fan out as a satellite with the parent as its hub, `Tick_Tiny` plays, and any option can be activated.
    - [ ] Given a stuff build slot or option, then 2+ materials open the material ring (last-used first, highlighted), 1 activates directly, 0 greys the slot with the "no materials" tooltip; choosing a material activates with that stuff.
    - [ ] Given a satellite, then Esc, right-click, click on its hub or outside goes back one level (T-21 commands).
    - [ ] *(UC-3)* Journey 4: chairs dropdown → armchair → wood → designator ready to place, menu closed.
  - **Tests**: `SubMenuLayoutTests`, `TriggerStateMachineToggleTests`; in-game UC-3.
  - **In-game check**: UC-3 (user)
  - **Size**: M

- [ ] **T-44**: Edit mode shell: enter, Done, Esc, Add
  - **Owner**: developer
  - **Requirements**: FR-9 (enter/exit, add), FR-7 (entry point) (design.md S4a, S5)
  - **Depends on**: T-32
  - **Files**: `src/QuickMenu/UI/{EditModeController,QuickMenuWindow,HubRenderer}.cs`
  - **Acceptance criteria**:
    - [ ] Given use mode, when the Edit sub-button gets a deliberate click (down and up on it), then edit mode starts; the hub shows Done and Add; satellites do not open in edit mode.
    - [ ] Given edit mode, then Done or the first Esc leaves it (settings written once if dirty), a second Esc closes, a left-click outside does nothing.
    - [ ] Given Add, then `PickerWindow` opens above the menu and the menu stays open (stub until T-45 if needed).
  - **Tests**: `TriggerStateMachineToggleTests` (edit rows); in-game UC-3.
  - **In-game check**: UC-3 (user)
  - **Size**: S

- [ ] **T-45**: Picker window
  - **Owner**: developer
  - **Requirements**: FR-7, FR-10, FR-15, NFR-1 (P-15), NFR-2, NFR-4 (design.md S6)
  - **Depends on**: T-40, T-44
  - **Files**: `src/QuickMenu/UI/{PickerWindow,PickerRow}.cs`, `mod/Languages/English/Keyed/QuickMenu.Picker.xml`
  - **Acceptance criteria**:
    - [ ] Given the picker, then it is a 540 × 680 dialog (clamped), non-pausing, with `QuickSearchWidget` focused, a status line, rows grouped by category, only visible rows drawn, and a footer.
    - [ ] Given a search, then results match label, category and mod name (Core `SearchIndex`); Enter pins when exactly one row matches; no results shows "Nothing matches “{0}”."
    - [ ] Given a row click, then pin or unpin (`Tick_High`/`Tick_Low`), the "Pinned" pill updates, the window stays open; dropdown rows pin the whole dropdown and their disclosure shows children that pin singly.
    - [ ] Given a locked item, then it is greyed with its reason (disabled reason now; composed reasons in T-65) and can still be pinned; dev-only items appear under "DEV MODE ONLY" only in dev mode; full shows "The menu is full." in `danger`.
    - [ ] *(UC-3)* Journey 2 with BAM installed: search "chair", pin the chair dropdown; it fills a slot as a whole sub-menu.
  - **Tests**: `SearchIndexTests`; in-game UC-3.
  - **In-game check**: UC-3 (user)
  - **Size**: M

- [ ] **T-46**: In-game stress mod generator · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-11c, W-1, W-2)
  - **Depends on**: —
  - **Files**: `scripts/make-stress-mod.sh`, `.gitignore` (output folder)
  - **Acceptance criteria**:
    - [ ] Given `scripts/make-stress-mod.sh N`, then it writes a test-only mod (gitignored, outside `mod/`) with N buildable `ThingDef`s (some stuff-made, some in dropdown groups) over vanilla and 3 fake `DesignationCategoryDef`s, and an About.xml with packageId `sixdd.QuickMenuStress`; it is never packaged.
  - **Tests**: script run in CI-like check (XML well-formed via `xmllint` if present, else a `dotnet` one-liner test).
  - **Size**: S

- [ ] **T-47**: UC-3 checklist, session guide and run
  - **Owner**: test-engineer
  - **Requirements**: FR-7, FR-10, FR-11, FR-12, FR-13, NFR-2
  - **Depends on**: T-40, T-43, T-45, T-46
  - **Files**: `docs/project/test-checklist.md` (M3 items, section UC-3), `docs/project/test-reports/uc-3.md`
  - **Acceptance criteria**:
    - [ ] Checklist items cover every M3 acceptance criterion on (a) and (b), journey 2 and 4, an Ideology designator (setup a with Ideology), the self-test on (R) and on the stress mod (2,000 and 8,000 defs), and picker scrolling for P-15.
    - [ ] *(UC-3)* Results recorded; failures become bug tasks.
  - **Tests**: the session itself.
  - **In-game check**: UC-3 (user)
  - **Size**: S

- [ ] **T-48**: M3 visual review · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-7, NFR-5
  - **Depends on**: T-47
  - **Files**: `docs/project/design/screens/m3/*`, `docs/project/design.md`
  - **Acceptance criteria**:
    - [ ] States 4, 5, 12, 13, 14, satellites, material ring and picker rows match design.md and the mockups; the user confirms.
  - **Tests**: screenshot review.
  - **Size**: S

- [ ] **T-49**: M3 performance record · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-7c, P-8 with sub-ring, P-11c, P-12, P-13 in game, P-15)
  - **Depends on**: T-47
  - **Files**: `docs/project/performance.md` §5
  - **Acceptance criteria**:
    - [ ] Each metric recorded with W-0/W-1/W-2 and (R) counts, pass/fail at 50 % of budget; failures become `perf:` tasks before M4 ends.
  - **Tests**: none.
  - **Size**: S

### M4: Edit mode, grid, Auto sizing

- [ ] **T-50**: Core `GridLayout` and grid hit testing · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-4, FR-6, NFR-8 (design.md §1.1 grid)
  - **Depends on**: T-17
  - **Files**: `src/QuickMenu.Core/Layout/{GridLayout,GridParams}.cs`, `src/QuickMenu.Core/Layout/HitTester.cs`, `tests/QuickMenu.Core.Tests/Layout/{GridLayoutTests,HitTesterGridTests}.cs`
  - **Acceptance criteria**:
    - [ ] Given 5 × 3 at B = 56, then the panel is 336 × 208, the hub is cell (1, 2), 14 slots in reading order skipping the hub; odd-only cols {3…11} and rows {1…7} (Fixed).
    - [ ] Given a point, then a slot is hit inside its P × P cell; the hub sub-button first, then the centre cell as main area; panel and plate count as inside.
  - **Tests**: unit tests above.
  - **Size**: M

- [ ] **T-51**: Core `Capacity` (Auto growth and full) for wheel and grid
  - **Owner**: developer
  - **Requirements**: FR-5, FR-6, NFR-5, NFR-8
  - **Depends on**: T-16, T-50
  - **Files**: `src/QuickMenu.Core/Layout/Capacity.cs`, `src/QuickMenu.Core/Layout/WheelLayout.cs` (Auto mode), `tests/QuickMenu.Core.Tests/Layout/CapacityTests.cs`
  - **Acceptance criteria**:
    - [ ] Given Auto wheel with count c, then the smallest m with Σ cap ≥ c is used, inner rings full, outer ring spread evenly; c = 0 draws ring-1 geometry; edit mode adds one append slot.
    - [ ] Given Auto wheel, then full when count = Σ cap(m) and m + 1 rings do not fit the screen minus margins, or m = 8. Given Auto grid, rows = smallest odd with rows·cols − 1 ≥ count; full when rows + 2 do not fit or rows = 9.
    - [ ] Given Fixed, then capacity = rings or rows·cols − 1.
  - **Tests**: unit `CapacityTests` (UI screen 1920 × 1080 and 960 × 540).
  - **Size**: M

- [ ] **T-52**: Core `PinBoard` Auto semantics, insert cursor, mode conversion, forget missing · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-5, FR-9, FR-14, FR-15, NFR-8 (architecture §4, U-2)
  - **Depends on**: T-18
  - **Files**: `src/QuickMenu.Core/Pins/PinBoard.cs`, `tests/QuickMenu.Core.Tests/Pins/PinBoardAutoTests.cs`
  - **Acceptance criteria**:
    - [ ] Given Auto, then pins are an ordered list, unresolved ones are hidden but keep their order, and pins go in front of the cursor (Fixed: into the cursor slot; occupied cursor in Fixed still reports full).
    - [ ] Given Fixed ↔ Auto conversion, then order is preserved and no pin is lost.
    - [ ] Given `CountUnresolved` and `ForgetUnresolved`, then only unresolved pins are counted and removed.
  - **Tests**: unit `PinBoardAutoTests`.
  - **Size**: M

- [ ] **T-53**: Perf gates v2: grid, Auto and sub-menu layout
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-9 Core part, P-10)
  - **Depends on**: T-36, T-50, T-51
  - **Files**: `tests/QuickMenu.Core.Tests/Perf/{ZeroAllocTests,LayoutTimingTests}.cs`, `tests/QuickMenu.Core.Benchmarks/LayoutBenchmarks.cs`
  - **Acceptance criteria**:
    - [ ] Grid hit test allocates 0 bytes; grid/Auto/sub-menu layout for 40 slots + one sub-ring fails above 5 × 50 µs; benchmarks added.
  - **Tests**: `scripts/perf.sh`.
  - **Size**: S

- [ ] **T-54**: Grid panel and sub-grid renderer · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-4, FR-11 (look) (design.md S2, §2 grid panel)
  - **Depends on**: T-29, T-42
  - **Files**: `src/QuickMenu/UI/{GridRenderer,PanelRenderer}.cs`
  - **Acceptance criteria**:
    - [ ] Given a grid layout, then the 9-slice panel and rim (radius 10), slots, hub in the centre cell and the label plate are drawn using the same slot and hub routines as the wheel; sub-grids and the material grid use the same panel; the main panel dims to 45 % while one is open.
  - **Tests**: build; visual review T-61.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [ ] **T-55**: Edit-mode states, help panel and hint plate renderer · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: FR-9, FR-14, FR-16 (look) (design.md S4 states 7–11, S5)
  - **Depends on**: T-29
  - **Files**: `src/QuickMenu/UI/{EditStateRenderer,HelpPanelRenderer}.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] States 7 (empty edit), 8 (reserved-missing "?"), 9 (drag source ghost and dragged icon at 0.9 B, 80 %, offset +16), 10 (insert cursor with caret) and 11 (drop target) are drawn per S4.
    - [ ] The help panel is at top centre (60 px down, max 520 wide) with the Fixed or Auto text and the capacity line (`danger` when full); the hint plate wraps at 360.
  - **Tests**: build; visual review T-61.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [ ] **T-56**: Grid layout and Auto sizing in the window
  - **Owner**: developer
  - **Requirements**: FR-4, FR-5, FR-11, FR-12, FR-6 (design.md S2, S3)
  - **Depends on**: T-43, T-51, T-54
  - **Files**: `src/QuickMenu/UI/{MenuController,QuickMenuWindow,SatelliteController}.cs`
  - **Acceptance criteria**:
    - [ ] Given layout = Grid, then the menu, sub-grids and material grids use `GridLayout`/`SubMenuLayout` and every feature that works in the wheel works in the grid (FR-4).
    - [ ] Given sizing = Auto, then the menu grows by ring or row as pins are added and pinning reports full per `Capacity`.
    - [ ] *(UC-4)* Every M2/M3 checklist item passes again with Grid and with Auto.
  - **Tests**: Core layout tests; in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [ ] **T-57**: Edit interactions: drag swap, right-click clear, insert cursor, help panel, full
  - **Owner**: developer
  - **Requirements**: FR-9, FR-5, FR-14, NFR-1 (P-17 write count)
  - **Depends on**: T-44, T-52, T-55, T-56
  - **Files**: `src/QuickMenu/UI/{EditModeController,QuickMenuWindow}.cs`, `src/QuickMenu/Pinning/PinService.cs`
  - **Acceptance criteria**:
    - [ ] Given edit mode, when slot A is dragged (start after 6 px) onto slot B, then they swap with `Tick_Tiny`; less than 6 px is a click that sets or clears the insert cursor.
    - [ ] Given a right-click on a slot (including a reserved one), then it is cleared; on the hub it does nothing.
    - [ ] Given the help panel, then it shows the right text and capacity line; when full, pinning from the picker or by click plays `ClickReject` with the full message.
    - [ ] Given an edit session, then settings are written once at edit end or menu close if dirty, never per drag step (counter in the self-test).
    - [ ] *(UC-4)* Journey 5 in wheel and grid.
  - **Tests**: `PinBoardFixedTests`, `PinBoardAutoTests`; in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [ ] **T-58**: Empty-menu state
  - **Owner**: developer
  - **Requirements**: FR-16, FR-1 (journey 1)
  - **Depends on**: T-55, T-56
  - **Files**: `src/QuickMenu/UI/QuickMenuWindow.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] Given no visible pins and not in edit mode, when the menu opens, then the label plate is replaced by the hint with the effective modifier name; Fixed draws its empty slots, Auto a ring-1 disc (or 1-row panel); the hub with Edit is shown.
    - [ ] *(UC-4)* Journey 1 after "Clear all pins".
  - **Tests**: in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: S

- [ ] **T-59**: Settings page v2: shape, size, grid, preview, missing pins
  - **Owner**: developer
  - **Requirements**: FR-4, FR-5, FR-6, FR-14, NFR-5 (design.md S7 right column)
  - **Depends on**: T-31, T-51, T-52, T-54
  - **Files**: `src/QuickMenu/Settings/{SettingsPage,LayoutPreview}.cs`, `mod/Languages/English/Keyed/QuickMenu.Settings.xml`
  - **Acceptance criteria**:
    - [ ] Shape [Wheel][Grid], Size Fixed/Auto with tooltips, Rings or Columns/Rows steppers (odd steps, hidden in Auto except Columns), slot count text.
    - [ ] Preview 410 × 270 at 50 % from Core, updated live, with the "bigger than the screen" warning and the Fixed shrink note.
    - [ ] Given unresolved pins, then the count text and [Forget missing pins] with a confirmation; on confirm, `ForgetUnresolved` and one write.
    - [ ] *(UC-4)* Each setting change shows in the preview and on the next open; at UI scale 2.0 the 4-ring warning appears and the menu is centred.
  - **Tests**: `CapacityTests`; in-game UC-4.
  - **In-game check**: UC-4 (user)
  - **Size**: M

- [ ] **T-60**: UC-4 checklist, session guide and run
  - **Owner**: test-engineer
  - **Requirements**: FR-4, FR-5, FR-6, FR-9, FR-16, NFR-5
  - **Depends on**: T-56, T-57, T-58, T-59
  - **Files**: `docs/project/test-checklist.md` (M4, UC-4), `docs/project/test-reports/uc-4.md`
  - **Acceptance criteria**:
    - [ ] Items for every M4 criterion, the FR-4 matrix (each feature × wheel/grid), UI scale 1.0 and 2.0 screenshots of every S4 state in both layouts, edit-session write counter, and the perf steps for P-8 (grid, max size) and P-9.
    - [ ] *(UC-4)* Results recorded; failures become bug tasks.
  - **Tests**: the session itself.
  - **In-game check**: UC-4 (user)
  - **Size**: S

- [ ] **T-61**: M4 visual review (UI scale 1.0 and 2.0) · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7
  - **Depends on**: T-60
  - **Files**: `docs/project/design/screens/m4/*`, `docs/project/design.md`
  - **Acceptance criteria**:
    - [ ] Every S4 state in wheel and grid at both UI scales matches design.md; masks are sharp at B = 96 and UI scale 2; the user confirms.
  - **Tests**: screenshot review.
  - **Size**: S

- [ ] **T-62**: M4 performance record · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-8 grid and max size, P-9, P-17 in game)
  - **Depends on**: T-60
  - **Files**: `docs/project/performance.md` §5
  - **Acceptance criteria**:
    - [ ] Metrics recorded with pass/fail at 50 % of budget; failures become `perf:` tasks.
  - **Tests**: none.
  - **Size**: S

### M5: Hold mode, availability, compatibility

- [ ] **T-63**: Core `TriggerStateMachine` hold mode · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-2, FR-9, FR-11, NFR-8 (design.md S3 hold, S4a, close and cancel rules)
  - **Depends on**: T-21
  - **Files**: `src/QuickMenu.Core/Input/TriggerStateMachine.cs`, `tests/QuickMenu.Core.Tests/Input/TriggerStateMachineHoldTests.cs`
  - **Acceptance criteria**:
    - [ ] Given hold mode, when the trigger is released over a slot, then `Activate(i)`; over the hub (either part) or elsewhere, then `Close` (or `Back` with a satellite open — releasing elsewhere closes everything per S3).
    - [ ] Given hovering a sub-menu or material slot for ≥ 150 ms (time passed in as a parameter), then `OpenSub(i)`; less, then nothing; releasing over an option activates it.
    - [ ] Given HubEdit, then only a deliberate click enters edit mode, never a release; in edit mode releasing keeps the menu open.
  - **Tests**: unit `TriggerStateMachineHoldTests`; zero-alloc gate extended in `ZeroAllocTests` (owner may add it here).
  - **Size**: M

- [ ] **T-64**: Hold mode in the window and the trigger-mode setting
  - **Owner**: developer
  - **Requirements**: FR-2 (design.md S7 trigger mode)
  - **Depends on**: T-56, T-63
  - **Files**: `src/QuickMenu/UI/QuickMenuWindow.cs`, `src/QuickMenu/Settings/SettingsPage.cs`, `mod/Languages/English/Keyed/QuickMenu.Settings.xml`
  - **Acceptance criteria**:
    - [ ] Given hold mode, then `WindowUpdate` polls `GetMouseButtonUp(i)`/`GetKeyUp(code)` for the bound trigger and feeds the release and the dwell time to the state machine.
    - [ ] Given settings, then "Trigger mode" radio (Press to open… / Hold, release over a button) is shown and saved.
    - [ ] *(UC-5)* FR-2 hold criteria pass in wheel and grid, with a mouse button and with a key.
  - **Tests**: `TriggerStateMachineHoldTests`; in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: S

- [ ] **T-65**: `AvailabilityResolver` and greyed slots
  - **Owner**: developer
  - **Requirements**: FR-14, FR-7 (greyed items), NFR-4, NFR-6 (ADR 0006, U-4)
  - **Depends on**: T-14, T-42, T-45
  - **Files**: `src/QuickMenu/Catalog/AvailabilityResolver.cs`, `src/QuickMenu/UI/{QuickMenuWindow,PickerRow}.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] Given a `Disabled` designator, then the reason is `disabledReason`; given `Visible == false`, then the reason is composed from the causes found in S-5 (research with the project label, tech level, required building, monolith, grav engine) using game strings where they exist, else "Not available right now."
    - [ ] Given a greyed slot, then the tooltip adds the reason in `danger`; clicking it plays `ClickReject` and shows the reason as a `RejectInput` message; availability is evaluated on open and on hover only.
    - [ ] Given the picker, then greyed rows show the same reason in the sub-line.
    - [ ] *(UC-5)* An unresearched build (e.g. a tier-2 power building on a new colony) shows the research reason; a Biotech mechanitor item without a mechanitor is greyed.
  - **Tests**: in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: M

- [ ] **T-66**: Missing-mod pins end to end · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-14, FR-17, NFR-6 (architecture §4, U-2)
  - **Depends on**: T-57, T-59
  - **Files**: `src/QuickMenu/Pinning/PinService.cs`, `src/QuickMenu/UI/QuickMenuWindow.cs`, `mod/Languages/English/Keyed/QuickMenu.Menu.xml`
  - **Acceptance criteria**:
    - [ ] Given a pin whose def or type is not loaded, then Fixed reserves its slot (empty in use mode, "?" in edit mode with the S4 state 8 tooltip and mod name), Auto hides it, and the settings count includes it; no error is logged.
    - [ ] Given the mod is added back, then the pin returns in its place.
    - [ ] *(UC-5)* Journey 6 with the stress mod (or BAM-only designator): pin, remove the mod, restart, check, re-add, check.
  - **Tests**: `PinBoardFixedTests`, `PinBoardAutoTests`, `PinSerializerTests`; in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: S

- [ ] **T-67**: Mod detection, Mint Alt default, notices · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-1, FR-8, NFR-2, NFR-4 (design.md S7 Mint line, S8)
  - **Depends on**: T-31
  - **Files**: `src/QuickMenu/Compat/ModDetection.cs`, `src/QuickMenu/UI/FirstRunNoticeWindow.cs`, `src/QuickMenu/QuickMenuMod.cs`, `mod/Languages/English/Keyed/QuickMenu.Notices.xml`
  - **Acceptance criteria**:
    - [ ] Given startup, then Mint (`Dubwise.DubsMintMenus`), BAM and Multiplayer are detected once; the effective modifier is Alt when Mint is active and the user has not chosen; the settings page shows the Mint line.
    - [ ] Given the first colony map ever, then the first-run notice (S8) appears once with [Use middle mouse] [Open settings] [OK], and includes the Mint line if Mint is active (setting both flags).
    - [ ] Given Mint becomes active after the first run, then the one-time `NeutralEvent` message appears once.
    - [ ] *(UC-5)* Setups (c) and (d): Alt+click pins, Ctrl+click is left to Mint, notices appear once.
  - **Tests**: `ModifierPolicyTests`; in-game UC-5.
  - **In-game check**: UC-5 (user)
  - **Size**: M

- [ ] **T-68**: Multiplayer readiness audit · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: FR-18, NFR-2 (ADR 0006, A-2)
  - **Depends on**: T-41
  - **Files**: `docs/project/test-checklist.md` (FR-18 item, marked blocked until Multiplayer supports 1.7), `docs/project/decisions/0006-designator-catalog-and-activation.md` (audit note)
  - **Acceptance criteria**:
    - [ ] Given the source, then a grep shows no `Designate*` call, no Multiplayer API reference, no world/map state mutation outside `SetStuffDef`, `SetActiveDesignator` and `DesignatorManager.Select`; the result is written in ADR 0006.
    - [ ] Given Multiplayer has no 1.7 build, then the FR-18 checklist item is recorded as **blocked (not failed)** per A-2, with the exact steps for when it does.
  - **Tests**: grep audit in the checklist; in-game setup (e) when available (UC-6).
  - **Size**: S

- [ ] **T-69**: UC-5 checklist, session guide and run
  - **Owner**: test-engineer
  - **Requirements**: FR-2, FR-8, FR-14, FR-17, NFR-2, NFR-6
  - **Depends on**: T-64, T-65, T-66, T-67, T-68
  - **Files**: `docs/project/test-checklist.md` (M5, UC-5), `docs/project/test-reports/uc-5.md`
  - **Acceptance criteria**:
    - [ ] Items for every M5 criterion on (a), (c), (d), journey 6, removing Quick Menu from a save and loading it (FR-17: no errors), and Ctrl/Alt+click in Mint's own architect window and BAM with Mint.
    - [ ] *(UC-5)* Results recorded; failures become bug tasks.
  - **Tests**: the session itself.
  - **In-game check**: UC-5 (user)
  - **Size**: S

- [ ] **T-70**: M5 visual review · `parallel-ok`
  - **Owner**: visual-designer
  - **Requirements**: NFR-5, NFR-7
  - **Depends on**: T-69
  - **Files**: `docs/project/design/screens/m5/*`, `docs/project/design.md`
  - **Acceptance criteria**:
    - [ ] Greyed state, reserved-missing state, notices and the settings Mint line match design.md; the user confirms.
  - **Tests**: screenshot review.
  - **Size**: S

- [ ] **T-71**: M5 performance record · `parallel-ok`
  - **Owner**: performance-engineer
  - **Requirements**: NFR-1 (P-3, P-4 with BAM and Mint, P-5 in game)
  - **Depends on**: T-69
  - **Files**: `docs/project/performance.md` §5
  - **Acceptance criteria**:
    - [ ] Metrics recorded with pass/fail at 50 % of budget; P-4 must be 0 with vanilla, BAM and Mint, or R-1 is reopened.
  - **Tests**: none.
  - **Size**: S

### M6: Hardening and release prep

- [ ] **T-72**: Fault isolation audit and fault-injection action · `parallel-ok`
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

- [ ] **T-73**: Keyed-string and key-usage check in CI · `parallel-ok`
  - **Owner**: developer
  - **Requirements**: NFR-4
  - **Depends on**: T-67
  - **Files**: `scripts/check-strings.sh`, `.github/workflows/ci.yml`, `scripts/test.sh` (call it)
  - **Acceptance criteria**:
    - [ ] Given `src/QuickMenu`, then the script fails on string literals passed to `Widgets.Label`, `Messages.Message`, `TooltipHandler.TipRegion`, `Widgets.ButtonText` and window titles (allow-list for log text and Keyed key names).
    - [ ] Given the Keyed XML, then every `"QuickMenu.*".Translate()` key used in code exists and every defined key is used.
  - **Tests**: the script runs in CI and passes.
  - **Size**: S

- [ ] **T-74**: Clean-room, dependency and asset audit · `parallel-ok`
  - **Owner**: release-engineer
  - **Requirements**: NFR-7, NFR-3
  - **Depends on**: T-69
  - **Files**: `docs/project/assets.md`, `docs/project/test-reports/release-audit.md`
  - **Acceptance criteria**:
    - [ ] Given the repo, then nothing from `reference/` is tracked, no Keyed string is identical to a Mint Keyed string (script diff, result recorded), every non-code file is in assets.md, and every texture is ours.
    - [ ] Given About.xml and the package zip, then Harmony is the only dependency, no `0Harmony.dll` or pdb ships, and `supportedVersions` is 1.7.
  - **Tests**: audit commands recorded in the report.
  - **Size**: S

- [ ] **T-75**: UC-6 final regression session
  - **Owner**: test-engineer
  - **Requirements**: all FR-1 … FR-18, NFR-1, NFR-2, NFR-3, NFR-5, NFR-6
  - **Depends on**: T-72, T-73, T-74
  - **Files**: `docs/project/test-checklist.md` (UC-6 = full checklist), `docs/project/test-reports/uc-6.md`
  - **Acceptance criteria**:
    - [ ] The guide covers the whole checklist on (a)–(d) and (R), (e) if Multiplayer supports 1.7, the fault-injection action, the NFR-3 load test (only Harmony + Quick Menu), final measurement actions, and 3–5 clean screenshots for the Preview (wheel with a sub-ring, grid, picker) on an attractive colony.
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
    - [ ] Given the UC-6 screenshots, then `Preview.png` is 2.468:1 (e.g. 1234 × 500), under 1 MB, made by a checked-in `magick` script with title text, contains no Mint imagery, and its assets.md row is in the main table; the user confirms the look.
  - **Tests**: `identify mod/About/Preview.png` size check.
  - **Size**: S

- [ ] **T-78**: README and Workshop description · `parallel-ok`
  - **Owner**: docs-writer
  - **Requirements**: spec §12, NFR-2, NFR-3, NFR-7
  - **Depends on**: T-75
  - **Files**: `README.md`, `docs/workshop-description.txt` (Steam BBCode), `mod/About/About.xml` (`description`)
  - **Acceptance criteria**:
    - [ ] README covers what it does, install (Harmony required), default trigger and the middle-mouse fallback, pinning (picker and Ctrl/Alt+click), wheel/grid, settings, compatibility notes (BAM, Mint, Multiplayer status), building from source, license MIT, and the optional attribution line from assets.md.
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
    - [ ] Given the agreed version (Question 5), then About.xml and the assembly version match, `scripts/package.sh` produces `dist/QuickMenu-<version>.zip` with only mod content, CI is green on the release branch, and a **draft** GitHub release with the zip and CHANGELOG text is prepared but not published.
    - [ ] Publishing (GitHub release and Workshop upload) is left to Phase 7 and needs the user's explicit confirmation.
  - **Tests**: `unzip -l` of the zip matches the expected file list; CI run.
  - **Size**: S

## Requirement traceability
| Requirement | Tasks | Tests |
|---|---|---|
| FR-1 trigger binding, mouse 3–5, unbound, middle-mouse option | T-5, T-11, T-20, T-30, T-58, T-67 | `TriggerBindingTests`; UC-1 (input probe), UC-2 (binding items), UC-5 (first-run notice) |
| FR-2 toggle / hold, hover-open in hold | T-17, T-21, T-31, T-32, T-63, T-64 | `TriggerStateMachineToggleTests`, `TriggerStateMachineHoldTests`, `HitTesterTests`; UC-2, UC-5 |
| FR-3 centred on mouse, edge dodge, oversize centred | T-7, T-16, T-32, T-36 | `EdgeDodgerTests`, `MenuBoundsTests`, `SubMenuLayoutTests`; UC-2 (corners), UC-4 (UI scale 2.0) |
| FR-4 wheel and grid with the same features | T-2, T-17, T-29, T-50, T-54, T-56, T-59 | `WheelLayoutTests`, `GridLayoutTests`, `HitTesterGridTests`; UC-4 FR-4 matrix |
| FR-5 Fixed / Auto sizing, full | T-18, T-25, T-32, T-51, T-52, T-56, T-57, T-59 | `PinBoardFixedTests`, `PinBoardAutoTests`, `CapacityTests`; UC-2 (full message), UC-4 |
| FR-6 size settings used on next open | T-2, T-31, T-32, T-50, T-51, T-59 | `WheelLayoutTests`, `CapacityTests`; UC-2, UC-4 |
| FR-7 searchable picker independent of architect UI; greyed; dev-only | T-6, T-9, T-38, T-40, T-44, T-45, T-65 | `SearchIndexTests`, `SearchPerfTests`, self-test round-trip; UC-1, UC-3 (setup b), UC-5 |
| FR-8 modifier+click on any designator; Mint → Alt | T-8, T-20, T-26, T-27, T-31, T-67 | `ModifierPolicyTests`, `PinClickClassifierTests`; UC-1 (S-3 probe), UC-2 (a, b), UC-5 (c, d) |
| FR-9 edit mode: picker, drag swap, right-click clear, insert cursor | T-17, T-18, T-21, T-44, T-52, T-55, T-57, T-63 | `PinBoardFixedTests`, `PinBoardAutoTests`, `TriggerStateMachine*Tests`; UC-3, UC-4 |
| FR-10 pin a whole dropdown | T-3, T-6, T-9, T-26, T-27, T-37, T-40, T-42, T-43, T-45 | `PinKeyTests`, `PinKeyFactoryTests`, `ModDropdownMatcherTests`; UC-1 self-test, UC-2, UC-3 |
| FR-11 dropdown expands as sub-ring / sub-grid | T-21, T-36, T-42, T-43, T-54, T-56, T-63 | `SubMenuLayoutTests`, `TriggerStateMachine*Tests`; UC-3, UC-4 |
| FR-12 material sub-ring | T-36, T-37, T-41, T-42, T-43, T-56 | `StuffOrderingTests`, `SubMenuLayoutTests`; UC-3 (journey 4), UC-4 |
| FR-13 activate exactly as the architect | T-6, T-7, T-29, T-32, T-41, T-43 | UC-1 (skeleton), UC-2, UC-3 |
| FR-14 greyed with reason; missing-mod pins hidden but kept | T-9, T-18, T-19, T-37, T-40, T-42, T-52, T-55, T-57, T-59, T-65, T-66 | `PinBoardFixedTests`, `PinBoardAutoTests`, `PinSerializerTests`, `ModDropdownMatcherTests`; UC-1 (S-5 dump), UC-5 (journey 6, greyed) |
| FR-15 no duplicates (toggle) | T-3, T-18, T-25, T-26, T-45, T-52 | `PinBoardFixedTests` (toggle), `PinKeyTests`; UC-2, UC-3 |
| FR-16 empty-menu hint | T-55, T-58 | UC-4 (journey 1) |
| FR-17 global settings, nothing in saves | T-3, T-9, T-19, T-25, T-32, T-66, T-72 | `PinSerializerTests`; grep for Game/World/MapComponent (T-25); UC-2 (second save, restart), UC-5 (remove mod from save) |
| FR-18 Multiplayer-safe | T-32, T-41, T-68 | Source audit (T-68); UC-6 setup (e) when Multiplayer supports 1.7, else blocked per A-2 |
| NFR-1 no per-frame cost closed; open in one frame; index < 200 ms | T-5, T-7, T-8, T-10, T-15, T-24, T-35, T-39, T-40, T-46, T-49, T-53, T-62, T-71, T-76 | `ZeroAllocTests`, `LayoutTimingTests`, `PinBoardPerfTests`, `SerializerPerfTests`, `SearchPerfTests`; in-game P-rows at every UC |
| NFR-2 vanilla / BAM / Mint / DLC | T-6, T-8, T-9, T-13, T-26, T-40, T-45, T-67, T-68, T-75 | Self-test on (a), (b), (R); UC-2 (a, b), UC-3 (a, b), UC-5 (c, d), UC-6 (all) |
| NFR-3 Harmony only dependency | T-1, T-5, T-13, T-74, T-80 | About.xml review and zip listing (T-74, T-80); UC-1 and UC-6 load with Harmony + Quick Menu only |
| NFR-4 every string Keyed | T-5, T-25, T-30, T-31, T-45, T-55, T-58, T-59, T-65, T-67, T-73 | `scripts/check-strings.sh` in CI (T-73) |
| NFR-5 button size; UI scale | T-2, T-12, T-16, T-28, T-29, T-31, T-32, T-51, T-59, T-61 | `WheelLayoutTests` (all B), `CapacityTests` (960 × 540); UC-1 texture probe, UC-4 screenshots at 1.0 and 2.0 |
| NFR-6 failures never break the game; log once | T-5, T-19, T-25, T-26, T-65, T-66, T-72 | `PinSerializerTests` (bad lines); fault-injection action in UC-6; UC-5 missing-mod test |
| NFR-7 clean-room | T-4, T-22, T-23, T-28, T-74, T-77 | Mint string diff and asset audit (T-74); visual reviews |
| NFR-8 layout and pins unit-tested | T-2, T-3, T-16, T-17, T-18, T-19, T-20, T-21, T-36, T-37, T-38, T-50, T-51, T-52, T-63 | Whole Core suite, `scripts/test.sh` with ≥ 90 % line coverage in CI |

All FR-1 … FR-18 and NFR-1 … NFR-8 have at least one task and one test. Gaps found and fixed while planning: FR-16 had no owner beyond the renderer (added T-58); FR-17 "removing the mod from a save" had no check (added to UC-5 via T-69); NFR-3 had no load test (added to UC-1 and UC-6); FR-18 had no verification path (added T-68 with the A-2 blocked status); NFR-4 had only "review grep" (made a CI script, T-73).

## Risks and spikes

### Spikes (all in the M1 spike build, run by the user once in UC-1)
| Spike | Question | Task | How the user runs it | Result goes to |
|---|---|---|---|---|
| S-1 | Does Harmony 3 rc.3 patch every `ProcessInput` override, including one in a mod assembly? | T-8 (+ count in T-9) | self-test on (a) and on (b)/(R) | ADR 0001, 0005 |
| S-2 | Which physical button is index 3/4; IMGUI blind to 3/4; up events for hold mode | T-11 | input probe: press six buttons in order | ADR 0003, default label |
| S-3 | Which `Event` fields show the modifier and left button; idle invocations (P-4); do dropdown-entry clicks reach the element's `ProcessInput`; hotkey path | T-8 | ProcessInput probe with the click sequence in T-13 step 5 | ADR 0005, architecture §6, T-27 go/no-go |
| S-4 | Are keys unique and do all round-trip on a large list? | T-9 | self-test on (a) and (R) | ADR 0004, 0006; possible fix tasks |
| S-5 | How to detect dev-only designators; which `Visible` causes produce which reasons | T-9 | catalog dump | ADR 0006; T-40, T-65 |
| S-6 | Full-screen window: non-pausing, absorbs map clicks, same-frame open, closes on map/world change | T-7 | skeleton wheel steps in T-13 step 7 | ADR 0007 |
| S-7 | Runtime `Texture2D`, loose PNG, 9-slice under Waylith | T-12 | texture probe + 2 screenshots | ADR 0007, design.md §6; T-28 level |

Each spike is time-boxed to one session of agent work; the UC-1 session is about 45–60 minutes for the user. Probe code is deleted in T-14; only the self-test and the patch enumeration stay as production code.

### Risks
| Risk | Likelihood | Impact | Mitigation in this plan |
|---|---|---|---|
| UC-1 shows a blocker (Harmony cannot patch, runtime textures fail, window cannot absorb clicks) | Medium | High (rework of M2) | All spikes run before any dependent M2 task; T-14 records the fallback (picker-only for unpatched designators; PNG then baker textures; alternative window layer) and amends T-26/T-28/T-32 before they start |
| In-game verification depends on the user's time | High | Medium (milestones wait) | Six grouped sessions only; guides with exact steps and log markers; Core logic is test-covered so sessions check integration, not logic |
| RimWorld 1.7 or Harmony rc updates break the build or patches mid-project | High | Medium | Pinned references; rerun self-test at every UC; record build number in every result |
| BAM or Multiplayer have no 1.7 build | High | Medium | Their checks are recorded as blocked, not failed (A-2); no code depends on them |
| S-3 shows `ProcessInput` is called outside clicks (P-4 ≠ 0) | Low | Medium | T-15/T-71 reopen R-1; the classifier fast path is already zero-alloc and gated (T-24) |
| Auto full rule and UI scale 2.0 make the menu feel cramped | Medium | Low | Settings preview warning (T-59); checked in UC-4 |
| Hard-coded strings slip in | Medium | Low | CI string check (T-73) |
| GitHub repo or push not available | Medium | Low | T-1 is independent; local `scripts/test.sh`/`perf.sh` remain the gate until CI runs |

## Decisions (user, 2026-10-19)

1. **GitHub (T-1)**: yes — create public repo `Sixdd6/rimworld-quick-menu` at the start of M1 and push so CI runs.
2. **Real mod list**: yes — the user saves a named 1.7 mod list (with BAM if it loads).
3. **Skeleton trigger**: Mouse 4 hard-coded in M1 with a dev-mode switch; binding UI in M2.
4. **Stress mod**: yes — `sixdd.QuickMenuStress`, test-only, never shipped.
5. **Release version**: **0.9.0** until RimWorld 1.7 and Harmony 3 are final.
6. **Keyed files split per area**: accepted.
7. **Frame Profiler overhead**: measured in UC-1 (T-15).
