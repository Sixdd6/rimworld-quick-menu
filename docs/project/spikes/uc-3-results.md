# UC-3 session results (2026-10-19)

Setups: (b) Harmony + 5 DLC + BAM + Quick Menu; then (b) + stress mod (`sixdd.QuickMenuStress`, 2,000 defs) on a `-quicktest` map. Logs read by the orchestrator (raw logs local only).

## Functional

| Step | Result |
|---|---|
| Dropdown pin badge (1:30), stuff pin dot (4:30) | PASS |
| Fine tile dropdown opens sub-ring, main wheel dims, option activates | PASS — **feedback**: options should show the material (stone block) icon, not the tile (→ spec FR-11a) |
| Back from sub-ring via Esc / right-click / hub / outside | PASS |
| Wall material ring, pick material, place | PASS — **feedback**: materials must stay in fixed positions; last-used only highlighted, not moved first (→ spec FR-12 updated) |
| Nested dropdown → material ring → Esc returns to sub-ring | not testable in vanilla (no such dropdowns); covered by code review |
| Edit mode: pencil → ✓ Done, plain click no-op, Ctrl+click unpins and stays open, ✓ exits, outside click closes, removal persists | PASS |
| Carry-over M2: capture dialog rejects left click; Mouse 5; key F; Use middle mouse (no camera drag); Pin with Alt; rings 3 + bigger size apply; Reset to defaults keeps pins | PASS |
| Stress mod buildings visible in architect, Ctrl+click pin and activate from wheel | PASS |

## Defects found

- **Ideology refresh crashed** (`NullReferenceException` in `DesignatorCatalog.Fingerprint`) on the quicktest colony; the site disabled itself (fault isolation worked; menu kept working). Ideology part therefore did not run. Fix in progress.
- **Stress mod defNames end with a digit** → 2,000 RimWorld config errors (defs still load). Generator fix in progress.

## Performance (stress setup: 11 mods, 2,850 catalog entries, 1,702 top-level designators)

| Budget | Measured | Result |
|---|---|---|
| P-11c catalog build | 10.9 ms, builds=1, complete, 0 duplicates, round trip 100 % | PASS (≤ 100) |
| P-18 mod ctor | 87.3 ms (settings incl. game's read 56.1, patching 30.3) | PASS |
| P-16 settings, our part | 4.2 + 0.17 ms | PASS |
| P-7a open path | median 78 µs, max 101 µs, 4.7 KB | PASS |
| P-8 draw per frame | median 39.7 µs, p99 75 µs | PASS |
| P-9 allocations | 96 B/frame median (no control run this time) | PASS (≤ 512 over control) |
| Texture prewarm | 14.0 ms at startup (loading screen) | off play path |
| Satellite rim rebuild | 3.1 ms on first sub-ring open | PASS (≤ 5) |

## UC-3 recheck after fixes (26d7add)

| Check | Result |
|---|---|
| Fine tile sub-ring shows stone block icons (FR-11a) | PASS (user) |
| Wall material ring keeps a fixed order; last-used only highlighted (FR-12) | PASS (user) |
| No "Catalog.Ideology failed" in the log; no errors; 4/4 opens same frame | PASS (log) |
