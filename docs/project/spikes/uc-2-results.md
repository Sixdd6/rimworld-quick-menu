# UC-2 session results (2026-10-19)

Run by the user on RimWorld 1.7.4982, setups (a) Harmony + 5 DLC + Quick Menu and (b) (a) + Better Architect Menu. The orchestrator read the logs directly (raw logs kept locally in `raw/`, not committed). Settings file checked: `Mod_QuickMenu_QuickMenuMod.xml`.

## Functional results

| Step | Result |
|---|---|
| Settings page (two columns, binding Mouse 4, Pin with Ctrl, rings, button size) | PASS |
| Ctrl+click Wall / Mine (vanilla) → pinned message + tick, tool not activated | PASS |
| Ctrl+click dropdown button (fine tile floors) → whole dropdown pinned, list not opened | PASS (`dropdown:Floor_Tile_Fine`) |
| Ctrl+click an entry inside an open dropdown → only that entry pinned | PASS (`build:terrain/FineTileLimestone`) |
| Pinned Ideology morbid slabs (`build:thing/MorbidSlab_Medium`, `MorbidSlab_Broad`) | **FAIL**: pins saved, but the slots show as empty in the wheel. Cause: Ideology designators are not in the static catalog and the fallback lookup requires `Visible`; unresolved → Empty. **User requirement (2026-10-19): a pinned item must always show; when unavailable (no resources, not researched, ideology-gated, …) it is greyed and selection is blocked with the reason, never hidden.** |
| Wheel look, hover highlight + sound + plate label + tooltip | PASS |
| Press-drag-off-release does nothing; release on same slot activates | PASS |
| Wall → material list → wall tool; Mine → mining tool; dropdown pin → vanilla list | PASS |
| Edge dodge at all four corners | PASS |
| Close by Esc / right-click / click outside / hub × / trigger again | PASS (no pause menu) |
| Same frame open | 28/28 + 5/5 `same=True` |
| Restart: pins persist; second save shows same pins | PASS |
| BAM: Ctrl+click pin / unpin / pin (small shelf) and activate from wheel | PASS |
| Errors in log | none |

## Performance (setup b, user's main machine)

| Budget | Measured | Target (50 % margin) | Result |
|---|---|---|---|
| P-1 closed poller | median 0.17 µs, p99 0.63 µs, max 642 µs | ≤ 10 µs (5) | PASS |
| P-6 open latency | 33/33 same frame | same frame | PASS |
| P-7a open path | median 35 µs, max 75 µs, 1.9 KB per open (content now built in first frame) | ≤ 3 ms | PASS |
| P-8 draw per frame (6 slots, 2 rings) | median 16.5 µs, p99 62.5 µs | ≤ 1 ms (0.5) | PASS |
| P-9 draw allocations | median 64 B/frame vs control 32 (+32); p99 1,912 B; first frame after open max 528 B | ≤ 512 B over control (256) | PASS at median; **p99 above budget — investigate** |
| P-16 settings load | **92.6 ms** | ≤ 20 ms (10) | **FAIL** |
| P-18 mod ctor | 124.6 ms (of which settings 92.6, patching 30.5) | ≤ 300 ms with BAM (150) | PASS, but driven by P-16 |
| Texture rebuild incl. upload (T-28) | 6.46 ms (first), 5.08 ms | ≤ 5 ms | **FAIL (marginal)** |
| P-3 pin prefix | no samples (no pin clicks while measuring) | — | not measured |
