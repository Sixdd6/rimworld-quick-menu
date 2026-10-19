# UC-5 session results (2026-10-19)

Setups: (b) Harmony + 5 DLC + BAM + Quick Menu; (b) + stress mod; (d) (b) + Dubs Mint Menus. Logs read by the orchestrator (raw local only).

| Part | Result |
|---|---|
| Old settings file with `<triggerMode>` loads cleanly (T-94) | PASS |
| First-run notice appears once on first colony load, non-blocking; not again after reload (T-67) | PASS |
| Unresearched pin greyed with lock, research reason in red, click → reject + message (T-65) | PASS |
| Ctrl+click on a colonist command button does its normal thing, nothing pinned (T-66) | PASS |
| BAM moved building pins (if used) | PASS (user) |
| Missing-mod round trip with the stress mod: Fixed slot empty, "?" in edit mode with tooltip, settings shows 1 missing + Forget, pin returns in the same slot when re-enabled (T-66) | PASS |
| Mint: one-time Mint message, Alt+click pins, Ctrl+click goes to Mint, settings shows Alt + Mint note, wheel works alongside Mint, message not repeated (T-67) | PASS — self-test `mint=True bam=True effectiveModifier=Alt` |
| Slider rebuild: once per drag (10 rebuilds over the session, 1.5–5.8 ms each at UI 1) (T-95) | PASS |
| UI scale 1.75 + button size 96: wheel, material ring, grid sharp and on screen | PASS (user) |
| **Settings preview rim does not match the disc fill** at B 96 (rim too large, overflows box) and B 36 (rim too small, looks broken) — screenshots `uc5-preview-b96.png`, `uc5-preview-b36.png` | **FAIL** — fix in progress |

## Other log notes
- `ERROR InitializeDubsMintMenusReflection: toggleDesMethod is null` — from Better Architect Menu's own Mint integration (not Quick Menu). Note in compatibility docs.
- One menu frame took 8.2 ms / 1.5 MB (texture rebuild on the first open after a size change) — accepted one-time hitch (user decision).
- Texture leak check: live 27 = expected 27. Settings writes: 3 this session.
- Window per frame (whole session, incl. UI 1.75 and grid): median 60 µs, p99 203 µs — PASS (≤ 1 ms).
