# UC-4 session results (2026-10-19)

Setup (b): Harmony + 5 DLC + BAM + Quick Menu; user's colony. Logs read by the orchestrator (raw local only). Screenshots: `../design/spikes/uc4-material-ring-lastused.png`, `uc4-settings-page.png`.

| Part | Result |
|---|---|
| 1 Edit drag (wheel, Fixed): help panel, drag to empty slot moves, drag onto pin swaps, Esc/right-click cancels mid-drag, drop on hub/outside does nothing, arrangement persists | PASS |
| 2 Grid: panel with Edit hub cell, hover/plate, activate, fine-tile sub-grid with stone icons + dimming, Wall material grid, edge dodge at corners, edit-mode swap | PASS |
| 3 Auto: only pins shown, new pins at the end, second ring after 6, unpin closes the gap, Fixed→Auto→Fixed restores the Fixed layout | PASS |
| 4 Settings preview live; UI scale 1.5 warning; Clear all pins → empty hint; material ring last-used highlight without hover (screenshot) | PASS |
| 4.5 Missing-pins section | Not shown — correct: it only appears when pins from an unloaded mod exist (nothing missing; catalog ready at the main menu). Real test moves to UC-5 (remove a mod). |

## Performance (setup b, UI scale 1 and 1.5 tried)

| Budget | Measured | Result |
|---|---|---|
| P-7c Ideology refresh | 0.27 ms, 5 Ideology entries, 3 rebuilds over the session (game load + changes), site enabled | PASS |
| Satellite rim rebuild | 0.86 ms (was 3.1 ms at UC-3) | PASS |
| Texture rebuilds (rim at UI 1.5, all at UI 1) | 1.37–2.16 ms incl. upload | PASS (≤ 5) |
| Texture leak check | live 27 = expected 27 | PASS |
| P-7a open path | median 47 µs, first 318 µs | PASS |
| P-8 draw per frame (incl. material ring open) | median 29.8 µs, p99 84.5 µs | PASS |
| P-9 allocations | median 96 B vs control 32 B (+64); p99 from the game's hover sound | PASS |
| P-11c catalog | 6.2 ms, 723 entries, round trip 100 % | PASS |
| Settings writes | 25 over the session (many settings toggles + edits) | info |
