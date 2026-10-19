# UC-1 spike session results (2026-10-19)

Run by the user on RimWorld 1.7.4982 (Waylith), Harmony mod v3.0.1.0 (library 3.0.0-rc.3), Linux, 2560x1440.
The orchestrator read the log directly; full raw logs are kept locally in `raw/` (not committed: they contain local paths):
- `raw/uc-1a-Player.log`: setup (a) Harmony + all 5 DLC + Quick Menu.
- `raw/uc-1b-Player.log`: setup (b) as (a) + Better Architect Menu (`ferny.BetterArchitect`).
Screenshots: `../design/spikes/uc1-texture-probe-scale1.0.png`, `uc1-texture-probe-scale1.5.png` (the game refused UI scale 2.0 at 2560x1440; 1.5 used).

## Summary by spike

| Spike | Result |
|---|---|
| S-1 patch all ProcessInput overrides | PASS. patched=8 failed=0 in both setups (19–24 ms). All 8 overrides are in Assembly-CSharp; BAM adds none. |
| S-2 input | PASS. Mouse indices 0–4 all seen by polling, down and up (hold mode viable). User's mouse: back side button = index 3 ("Mouse 4"), front side button = index 4 ("Mouse 5"). IMGUI saw no side-button events. Keyboard key seen. |
| S-3 ProcessInput only on click | PASS for idle: 0 calls over 4,042 frames of hovering (P-4). Findings: (1) when the prefix runs the Event is already `used` (type=used, isMouse=False, isKey=False) for clicks AND hotkeys, so click vs hotkey cannot be told from the Event; read `UnityEngine.Input` mouse/key state instead. (2) `ev.control`/`ev.alt` are reliable for direct clicks. (3) Ctrl+click on an entry inside an open dropdown list reaches `Designator_Dropdown.ProcessInput` (instance = the dropdown, not the entry) with `ev.control=False` but `Input.GetKey(LeftControl)=True` → the conditional third patch (U-3, `GetDesignatorSelectAction`) is needed to pin single entries, and modifiers must be read from `Input`. (4) A hotkey press produced a `Designator_Cancel` call with a used event. (5) With BAM, BAM's `ProcessGizmoInteractions` calls `ProcessInput` → our prefix sees BAM clicks with Ctrl (FR-8 viable under BAM). |
| S-4 pin key round trip | PASS. (a) 454 entries, (b) 718 entries; 0 failures, 0 duplicates, complete=True, skipped=0. |
| S-5 dev-only / reasons | No dev-only designators by name. God-only with no known cause: `build:terrain/Substructure`, `build:thing/HoldingSpot`, `dropdown:Floor_Tile_Metal`, `dropdown:Floor_Carpet`, `dropdown:Floor_Carpet_Fine`, `order:RimWorld.Designator_ZoneAdd_Fishing` (likely DLC/ideology/other gates, not dev-only). 243 god-only explained by research / research prereqs / place workers / tech level. `disabledReason` never set; reasons must be composed. |
| S-6 window | PASS. 21/21 opens drawn in the trigger frame (`same=True`). Game keeps running. Clicks outside close the menu and do not reach the map or UI (a click on the World button only closed the menu). Mine activates; Wall opens the vanilla material list. Esc closes the menu without opening the pause menu. Wheel works with BAM loaded. |
| S-7 textures | PASS. Runtime `Texture2D` (RGBA32) and loose PNG via ContentFinder both load and draw (PNG is baked to BC7 with 9 mips automatically). Circles smooth at 36/56/96 px; `Widgets.DrawAtlas` 9-slice (64 px atlas, 14 px radius) keeps round corners at two sizes and at UI scale 1.0 and 1.5. Runtime path chosen; PNG fallback also works. |

## Performance baseline (setup a, user's main machine)

| Budget | Measured | Target | Result |
|---|---|---|---|
| P-1 closed poller | median 0.15 µs, p99 0.54 µs, max 281 µs (10,000 frames) | ≤ 10 µs median | PASS |
| P-2 closed allocations | 0 bytes over 10,000 frames | 0 | PASS |
| P-4 idle ProcessInput calls | 0 over 4,042 frames | 0 | PASS |
| P-6 open latency | 21/21 same frame | same frame | PASS |
| P-7a open path | median 159 µs, first open 1,213 µs, ~58 KB per open | ≤ 3 ms | PASS (allocation per open noted) |
| P-8 draw per frame | median 3.0 µs, p99 11.3 µs | ≤ 1 ms | PASS (skeleton has 2 slots) |
| P-9 draw allocations | 32 B/frame vs control 32 B/frame | ≤ 512 B over control | PASS (+0) |
| P-11c catalog build | 5.2 ms (a), 5.3 ms (b) | ≤ 200 ms | PASS |
| P-12 builds per session | 1 | 1 | PASS |
| P-18 mod ctor | 29 ms (a) | ≤ 150 ms | PASS |

## User-confirmed observations (setup a, wheel)

The user reported "everything else seemed fine" for the wheel steps, which confirms: the wheel appears at the mouse while the game keeps running (not paused); pressing the trigger again closes it; clicking away from the slots closes it with no effect on the map; Mine activates the mining tool; Wall opens the material list and then the wall tool; 20 open/close cycles near screen edges worked. Esc closed the wheel without opening the pause menu. Clicking the World button while the wheel was open only closed the wheel (the click is absorbed), so "world view closes the wheel" was not directly exercised; it is covered by code (`WindowUpdate`) and rechecked at UC-2. No `[QuickMenu]` errors or exceptions in either log. Map-change close and the trigger-cycle debug action were not exercised.
