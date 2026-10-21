# Test checklist (in-game sessions)

Sessions you run by hand in RimWorld. Each one has numbered steps: one action per step, what you should see, and what to copy.

## UC-1 spike session

**Task**: T-13. **Time**: about 45 to 60 minutes in total (per-part times below). **What it is for**: it answers the open questions in spikes S-1 to S-7 so that T-14 can write the ADR updates.

**What you hand back**: log blocks pasted into `docs/project/spikes/uc-1-results.md`, plus 2 screenshots (4 if you count both setups) saved in `docs/project/design/spikes/`. The agent cannot see the game, so do not summarise: paste the raw text.

### Facts checked on this machine (2026-10-19)

| Item | Result |
|---|---|
| Release build | `dotnet build QuickMenu.slnx -c Release` succeeded, 0 warnings, 0 errors |
| `mod/1.7/Assemblies/` | `QuickMenu.dll`, `QuickMenu.Core.dll` and their two `.pdb` files (plus an empty `.gitkeep`). No `0Harmony.dll`. |
| Harmony for 1.7 | Installed from the Workshop (item 2009463077, "Harmony" v3.0.1.0, lists 1.7). **packageId: `brrainz.harmony`** |
| Quick Menu link | `RimWorld/Mods/` holds only `3526354009` (another mod). **No QuickMenu link yet**: do step 0.3. |
| Better Architect Menu (BAM) | Installed: Workshop item 3563882422, **packageId `ferny.BetterArchitect`**, lists 1.6 and 1.7. It needs `brrainz.harmony` (and lists `com.bymarcin.architecticons` as an optional partner). Also present, not for this session: "Better Architect Menu | Tweaks" (3571749771, 1.6 only). |
| Game log file | `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Logs/Player.log`. Version 1.7 writes a "Waylith log" there (header `=== Waylith log ===`, `Runtime: .NET 10`). The game rewrites it at every start. |
| DLC packageIds | `Ludeon.RimWorld.Royalty`, `.Ideology`, `.Biotech`, `.Anomaly`, `.Odyssey` (all in `RimWorld/Data/`) |

Setup names used below:

- **(a)** vanilla: Harmony, all DLC, Quick Menu.
- **(b)** Better Architect Menu added.
- **(R)** your real saved 1.7 mod list, if you prefer it over (b).

### Part 0: Before you start (about 5 min)

0.1. Make sure RimWorld is closed.
- Expected: Steam shows "Play", not "Stop".

0.2. Open a terminal and run `cd ~/Documents/Programming/rimworld-quick-menu && . scripts/_env.sh && dotnet build QuickMenu.slnx -c Release`.
- Expected: "Build succeeded", 0 errors. (The agent already did this; do it again only if you pull new commits.)

0.3. Run `scripts/link-mod.sh` once.
- Expected: `linked: .../RimWorld/Mods/QuickMenu -> .../rimworld-quick-menu/mod`, or `already linked`.
- If you see "exists and is not a symlink" or "symlink to somewhere else", remove that path first and run it again.

0.4. In Steam, right-click RimWorld, Properties, General, Launch Options. Paste this single line for setup (a):
```
-mod=brrainz.harmony,ludeon.rimworld.royalty,ludeon.rimworld.ideology,ludeon.rimworld.biotech,ludeon.rimworld.anomaly,ludeon.rimworld.odyssey,sixdd.QuickMenu -quicktest
```
- Expected: Steam stores it. If the game starts with a mod-list error, remove the DLC IDs you do not own, or start with only `-mod=brrainz.harmony,sixdd.QuickMenu -quicktest` and enable the DLC in the Mods screen.
- Note: the mod order Harmony, then DLC, then Quick Menu is on purpose. Harmony must load first.

0.5. Start the game. Open Options, tick Development mode (or use the mod list screen). Then load or start a colony on any map with a few colonists. Use a normal, pausable game; you can pause with Space.
- Expected: the Mods screen (if you open it) shows Harmony, the DLC and "Quick Menu" with no red errors. The debug-actions toolbar (the bug-and-gear icons at the top of the screen) is visible.

0.6. Open the debug actions menu (the bug icon with the list). Use the filter box and type `Quick Menu`.
- Expected: you see the category "Quick Menu" with these actions: self-test, catalog dump, input probe (15 s), ProcessInput probe (toggle), ProcessInput probe report, measure closed frames, perf measuring (toggle), perf report, control window, Cycle skeleton trigger, texture probe.
- If none show up, stop and copy every line of `Player.log` that contains `QuickMenu` or `Harmony` or `Exception`, and send them back.

**About the log**: every block you copy is in `Player.log` (path in the table above). Open that file in any text editor after each step (or keep it open and reload). Search for the text `[QuickMenu] BEGIN` and copy from that line to the matching `[QuickMenu] END` line, including both lines. You can also copy from the in-game log window (the debug "Log" button), but the file is easier because it has no limit and keeps the line breaks. Do not edit the text. The file is overwritten when you restart the game, so **copy the blocks of a setup before you close the game**.

How to use the result file: open `docs/project/spikes/uc-1-results.md`. Each heading there matches one block below. Paste under the heading inside the code fence.

### Part 1: Self-test and catalog dump (about 5 min)

1.1. Debug actions, Quick Menu, click **self-test**.
- Expected: the log window shows a message. In `Player.log` there is one block `[QuickMenu] BEGIN self-test` ... `[QuickMenu] END self-test`. Inside it you should see the sections: catalog, patches, keys, S-5 dev-only and reasons, startup (P-18), perf counters. `buildMs` is near or under 200. `patched=` is above 0 and `failed=` is 0.
- Copy: the `self-test` block. Paste under "Self-test (a)".
- P-18 target: `modCtorMs` is 150 ms or less (vanilla set).

1.2. Debug actions, Quick Menu, click **catalog dump**.
- Expected: a first line `[QuickMenu] BEGIN catalog dump entries=N parts=M ...`, then several messages `[QuickMenu] dump part 1/M`, `dump part 2/M`, and so on, and a last line `[QuickMenu] END catalog dump`. The dump is large and cut into chunks, so each chunk is its own log message; in `Player.log` they follow each other.
- Copy: everything from BEGIN to END, all parts. If it is very long, paste it in `uc-1-results.md` in one fence anyway; do not trim.
- **Important**: this action tests god mode on every entry (the "god-only" and dev-only checks, spike S-5). It can leave the game in a changed state. **After you have copied the dump, reload the save** (or quit to the menu and load again) before you do anything else that matters. Do not play on in this session.
- This is also true for the god-mode / dev-only check in the self-test (section "S-5 dev-only and reasons"): reload the save before normal play.

1.3. Reload your save now (Menu, Load game; or close and restart the game with the same launch options, then load). Turn on Dev mode again if needed.
- Expected: the map loads and the debug toolbar is back.

### Part 2: Mouse-button probe (about 3 min)

2.1. Debug actions, Quick Menu, click **input probe (15 s)**. A small window "input probe" opens with a countdown.
- Expected: a 420 x 220 window with the seconds left.

2.2. In the next 15 seconds, press these **in order, about one second apart**: left mouse button, right button, middle button (wheel click), the **front** side button, the **back** side button, then any one keyboard key (for example the letter K). Aim at the map, not at buttons in the UI, for the clicks.
- Expected: the window closes by itself after 15 seconds. The game keeps running. (A click on the map may select things or give orders; that is fine.)

2.3. Open `Player.log`. Copy the block `[QuickMenu] BEGIN input probe` ... `[QuickMenu] END input probe`.
- Expected: lines for button 0, 1, 2, and then 3 and 4 with `polled down=1 up=1` and `IMGUI down=0` for the side buttons; `keyDowns=` is at least 1.
- Under "Input probe" in the results, also write in plain words: "front side button = index ?, back side button = index ?" (the last lines of the block ask for this).

### Part 3: ProcessInput probe, vanilla (about 8 min)

This checks spike S-3: does the game call a designator's `ProcessInput` only on a click or hotkey? Idle calls must be **0** (P-4).

3.1. Debug actions, Quick Menu, click **ProcessInput probe (toggle)**.
- Expected: a log line `[QuickMenu] ProcessInput probe ON from frame N`.

3.2. Click the **Architect** tab at the bottom of the screen, then **Structure**. Do not click any button inside.
- Expected: the Structure buttons (Wall, Door, Floor and others) are shown.

3.3. Move the mouse slowly over the Structure buttons, back and forth, for **10 seconds**. Do not click.
- Expected: tooltips show. Nothing is built. No `BEGIN ProcessInput call` appears in the log.

3.4. Debug actions, Quick Menu, click **ProcessInput probe report**. (This is the report after the idle phase.)
- Expected: a block `[QuickMenu] BEGIN ProcessInput report` ... `END ProcessInput report`. In it: `invocations=0`, `framesWithCalls=0`, `callsBeforeFirstMouseOrKeyEvent=0`. **If `invocations` is not 0, this is a finding: P-4 failed. Still continue.** The block also lists the patch sites (`patched=` count, `failed=`).
- Copy: this block. Paste under "ProcessInput report, after idle phase (a)".

3.5. Click **Wall** in the Structure list. If a material list appears, pick any material (for example Wood or Steel).
- Expected: the Wall designator is selected (you see the wall placement cursor). One block `BEGIN ProcessInput call` ... `END ProcessInput call` is logged.

3.6. Press **Esc** (or right-click) to leave the placement mode. Then **Ctrl + click** Wall (hold Ctrl, click, release).
- Expected: another `ProcessInput call` block. In it `ev.control=True`.

3.7. Press Esc. Then **Alt + click** Wall.
- Expected: another `ProcessInput call` block with `ev.alt=True`. (If your desktop uses Alt+click to move windows, hold Alt after pressing, or note that nothing was logged.)

3.8. Press Esc. Open a **dropdown button**: one with a small triangle in the corner (in the Architect tab, look for a button with a small triangle in a corner; if you cannot find one, write "no dropdown found"). Click it so the list opens. **Ctrl + click one entry** in the list.
- Expected: a `ProcessInput call` block for that entry, or none. Both are valid answers: paste whatever you see, and write "no block appeared" if so. (This tells us whether dropdown entries reach `ProcessInput`.)

3.9. Press Esc. Press the **hotkey** of one order if it has one (the key letter is shown on the button, for example the letter on the Mine or Chop order under Orders).
- Expected: a `ProcessInput call` block with `ev.isKey=True`, or `ev=null`. Write "no hotkey on screen" if the buttons show no letter.

3.10. Debug actions, Quick Menu, click **ProcessInput probe report** again. (Report after the click phase.)
- Expected: a second `ProcessInput report` block. `invocations` is now greater than 0. `mouseOrKeyCalls` is at least 3, `maxPerFrame` is 1 or more.
- Copy: this second block. Paste under "ProcessInput report, after click phase (a)".

3.11. Copy **all** `[QuickMenu] BEGIN ProcessInput call` ... `END ProcessInput call` blocks from the click phase. Paste them together under "ProcessInput calls (a)".
- Note: when you test the Quick Menu wheel in Part 5 the probe is still on, so the menu's own clicks will also show up as `ProcessInput call` blocks. That is expected. Switch the probe off before Part 5 if you want a clean list: use step 3.12.

3.12. Click **ProcessInput probe (toggle)** once more to turn it off.
- Expected: log line `[QuickMenu] ProcessInput probe OFF`. (If you want the wheel's clicks in the probe log, leave it ON and run **report** once more at the end of Part 5; paste that under "ProcessInput calls from menu clicks (a)".)

### Part 4: Idle cost, 10,000 frames (about 6 min)

4.1. Close all windows. Un-pause the game. Do not open the Quick Menu in this part. Debug actions, Quick Menu, click **measure closed frames**.
- Expected: a log line `[QuickMenu] Quick Menu: measuring 10000 frames; leave the map idle and do not open the menu.`

4.2. Leave the map **idle** for about 3 minutes (10,000 frames at 60 fps; longer if your frame rate is lower). Do not click, do not scroll, do not open any window. You can keep the game running at normal speed.
- Expected: after the frame count is reached, a block `[QuickMenu] BEGIN closed-frame measurement` ... `END closed-frame measurement` appears in the log. It is printed on the frame after the 10,000th, so wait a moment before copying it.
- It shows `medianUs` (P-1 target 10 microseconds or less, goal 2 or less) and `allocatedBytesTotal=0` (P-2).
- Copy: this block. Paste under "Closed-frame measurement". Measuring stays ON afterwards; leave it on.

### Part 5: Skeleton wheel (about 10 min)

The skeleton wheel is a test menu with two slots: Mine and Wall.

5.1. Move the mouse over an empty part of the map, away from the edges. Press **Mouse 4**. The mod calls this mouse index 3, and the input probe in Part 2 shows which physical side button that is on your mouse.
- Expected: a small circle with two icons appears at the mouse position, in the same frame. The game keeps running (not paused). Time moves on.
- If nothing appears: debug actions, Quick Menu, **Cycle skeleton trigger**. A message says "Quick Menu skeleton trigger: Mouse 5". Try Mouse 5. If still nothing, cycle again to "Middle mouse" and try the middle button. Note in the results which one worked. Mouse 4 is index 3, Mouse 5 is index 4 (see the input probe block for which of your buttons they are).

5.2. Press the trigger again while the menu is open.
- Expected: the menu closes.

5.3. Open the menu again. Click the map **away** from the two slots (and outside them).
- Expected: the menu closes. **Nothing happens on the map under the click** (no colonist selected, no order given, no camera move).

5.4. Open the menu again. Click the **Mine slot** (the pickaxe icon).
- Expected: the menu closes and the Mine designator is selected (you see the mining cursor and the Orders mining hint). Press Esc to deselect.

5.5. Open the menu again. Click the **Wall slot**.
- Expected: the vanilla material list opens (Wood, Steel and so on). Choose one. The menu is already closed. After choosing, the Wall designator is selected with that material. Press Esc to deselect.
- If the Wall has **no material available** (you have no wood, steel or stone anywhere), a "no stuff" or "no materials" message is expected instead; that is also a pass. Say which one happened.

5.6. Open the menu again and press **Esc**.
- Expected: the menu closes. The game does not open its pause/options menu in the same press (note it if it does).

5.7. Open the menu again. With it open, switch to the world map with the **World** button at the bottom right of the screen.
- Expected: the menu closes when the world view opens. Go back to the map.

5.8. Look at the log lines that begin `[QuickMenu] Menu trigger frame=` and `[QuickMenu] Menu first DoWindowContents frame=... same=True` (they appear in dev mode every time the menu opens). Copy the last 10 of each kind.
- Expected: `same=True` on all of them (P-6: opens in the same frame). Paste under "Menu open frames".

5.9. (Only if the ProcessInput probe is still ON) Debug actions, Quick Menu, **ProcessInput probe report**.
- Expected: the menu clicks on Mine and Wall appear as `ProcessInput call` blocks in the log, and the report counts them. Paste under "ProcessInput calls from menu clicks (a)".

### Part 6: Open/close cost and control run (about 6 min)

6.1. Reset the counters so this run is clean: run **perf measuring (toggle)** twice (the log says `perf measuring OFF`, then `perf measuring ON`). If you restarted the game since Part 4, run it once only, so the log ends with `perf measuring ON`.
- Make sure the last message is `perf measuring ON`; if it says OFF, the counters stay empty.

6.2. Open and close the wheel **20 times** (trigger, then Esc). Do about half of the openings with the mouse near the screen centre and half near a screen edge.
- Expected: no stutter, no errors in the log.

6.3. Hold the menu open once for about 10 seconds with the mouse sweeping over both slots (P-8 draw cost), then close it.
- Expected: the menu stays up and responds.

6.4. Debug actions, Quick Menu, click **control window**. A full-screen invisible window opens for 10 seconds. It draws the same number of slots (N) and the same icons as the wheel you just opened, so you must have opened the wheel at least once before this step. Leave your mouse still and do not click (a click closes it early).
- Expected: after 10 seconds it closes. Log line "control window open for 10 s; run 'perf report' afterwards."

6.5. Debug actions, Quick Menu, click **perf report**.
- Expected: a block `[QuickMenu] BEGIN perf report` ... `END perf report`. It lists startup time (P-18), and for each counter calls, median, p99 and bytes per frame.
  - P-7a: the "open path" line, median/max (within about 3 ms of the closed median). Its `firstCallUs` is the first open of the session (S-4), and the `firstFrameAfterOpen` fields show the first frame after each open.
  - P-8: the "menu window per frame" line, median 1.0 ms or less (the report prints microseconds, so 1000 us). All numbers are per frame, as medians.
  - P-9: compare bytes per frame on the "menu window per frame" line with the "control window per frame" line (the report prints N for both; the target is at most 512 B per frame above the control).
  - P-18: `modCtorMs`.
- Copy: this block. Paste under "Perf report (a)".

### Part 7: Texture probe and screenshots (about 6 min)

7.1. Debug actions, Quick Menu, click **texture probe**. A window opens with 4 rows.
- Expected: row 1 shows a white circle and a half-transparent cyan circle (at 36, 56 and 96 px, made at run time). Row 2 shows the PNG circle (from `SpikeCircle.png`). Row 3 shows the circle again with mipmaps. Row 4 shows two rounded rectangles drawn with Widgets.DrawAtlas (one white at 120 x 60, one cyan at 300 x 80). Nothing should be black, blank, or a pink/red "bad texture" square.

7.2. Take a screenshot at the current UI scale. First check the scale: Options, Interface, "UI scale". It should be **1.0**. Press **F12** (or the game's screenshot key). Screenshots go to `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Screenshots/` (the game prints the path). Copy the file to `docs/project/design/spikes/texture-probe-scale-1.0.png`.
- Expected: the file shows the whole probe window, all four rows visible.

7.3. In the probe window, click the **Log report** button.
- Expected: a block `[QuickMenu] BEGIN texture probe` ... `[QuickMenu] END texture probe`. Each step says success or the exception. It also states `UI scale=1` and the screen size, texture sizes and formats.
- Copy: this block. Paste under "Texture probe at UI scale 1.0".

7.4. Open Options, Interface, set **UI scale to 2.0**. Apply. Open the probe again: close the old window with its X (this also logs the report) and run **texture probe** again.
- Expected: the probe window is bigger on screen. The look of the circles and the rounded rectangles is the same shape as before. Look for blur, jagged edges or gaps in the rounded rectangle.

7.5. Take a second screenshot (F12) and save it as `docs/project/design/spikes/texture-probe-scale-2.0.png`. Click **Log report** and copy the `texture probe` block (it says `UI scale=2`). Paste under "Texture probe at UI scale 2.0".

7.6. Set the UI scale back to your normal value. Close the probe with its X.
- Expected: closing the window also logs the report once more; you do not need to copy that one.

7.7. Optional but useful: take a third screenshot of the open skeleton wheel (Part 5) at scale 1.0, saved as `docs/project/design/spikes/wheel-scale-1.0.png`, and a fourth at 2.0 as `wheel-scale-2.0.png`. This gives the four screenshots the plan names.

### Part 8: Second setup, (b) or (R) (about 12 min)

Only the **self-test** and the **ProcessInput probe** are repeated. Copy your log blocks from Part 1 to Part 7 before you close the game, because the log is overwritten at the next start.

8.1. Quit the game fully.

8.2. Change the launch options.
- Setup (b), Better Architect Menu added:
```
-mod=brrainz.harmony,ferny.BetterArchitect,ludeon.rimworld.royalty,ludeon.rimworld.ideology,ludeon.rimworld.biotech,ludeon.rimworld.anomaly,ludeon.rimworld.odyssey,sixdd.QuickMenu -quicktest
```
- Setup (R): remove `-mod=...` from the launch options and keep `-quicktest`; then in the in-game Mods screen select your saved 1.7 mod list and make sure `Harmony` and `Quick Menu` are active and `Quick Menu` is last (or below all architect mods).
- Note: BAM may ask for `com.bymarcin.architecticons` as an optional partner; ignore the warning, or add it if the game complains.
- Expected: BAM changes the Architect tab into its new layout when you load a map.

8.3. Start the game, turn on Dev mode, load the same save.
- Expected: no red mod errors. The Architect tab is BAM's version in setup (b).

8.4. Run **self-test**, as in 1.1. Copy the block. Paste under "Self-test (b or R)".
- Expected: compared with (a): more catalog entries; patch counts different; `failed=0`. Note a larger `modCtorMs` (P-18: 300 ms or less on the large list).

8.5. Run the ProcessInput probe, as in Part 3 (3.1 to 3.10), but using BAM's Structure page (or any architect page with Wall). Repeat the Wall click, the Ctrl+click, the Alt+click, the dropdown (3.8) and the hotkey (3.9) as in vanilla.
- Expected: idle invocations still 0 (P-4 "with BAM").
- Copy: the idle report (3.4), the click report (3.10) and the call blocks (3.11). Paste under the "(b or R)" headings.

8.6. Do **not** run the catalog dump or the other probes again. Turn off Dev mode if you like, and quit.

### Part 9: Finish (about 3 min)

9.1. Check that `uc-1-results.md` has every heading filled, or has "not run" or "no block appeared" written next to the ones you skipped.

9.2. Check that `docs/project/design/spikes/` holds at least `texture-probe-scale-1.0.png` and `texture-probe-scale-2.0.png`.

9.3. Put the Steam launch options back to your normal ones if you want. Tell the agent: "UC-1 done". The agent then runs T-14 using these files. Do not commit anything yourself unless you want to.

### Time estimate

| Part | Minutes |
|---|---|
| 0 Before you start | 5 |
| 1 Self-test, catalog dump, reload | 5 |
| 2 Mouse-button probe | 3 |
| 3 ProcessInput probe (a) | 8 |
| 4 Idle 10,000 frames | 6 |
| 5 Skeleton wheel | 10 |
| 6 Open/close, control window, perf report | 6 |
| 7 Texture probe and screenshots | 6 |
| 8 Second setup (b) or (R) | 12 |
| 9 Finish | 3 |
| **Total** | **about 64; 45 to 60 if you skip the optional steps and Part 8 restarts are quick** |

### Pass/fail marks the agent will read

| Target | Where | Pass |
|---|---|---|
| P-1 closed median | closed-frame measurement | 10 us or less (goal 2 us) |
| P-2 closed allocation | closed-frame measurement | 0 bytes |
| P-4 idle ProcessInput calls | report after idle phase, (a) and (b)/(R) | exactly 0 |
| P-6 open latency | Menu open frames | `same=True` |
| P-7a open path | perf report | 3 ms or less above the closed median |
| P-8 draw cost | perf report | median 1 ms or less |
| P-9 draw allocation | perf report | 512 bytes per frame or less above the control window |
| P-18 startup | self-test, perf report | 150 ms or less (a); 300 ms or less (b)/(R) |
| NFR-3 | mod list | game loads with Harmony and Quick Menu only |

## FR-18 Multiplayer (setup (e))

**Task**: T-68 (audit), run in UC-6 (T-75) or later. **Status: BLOCKED, not failed (spec A-2).** Multiplayer (`rwmt.Multiplayer`) is not installed, and its newest release (v0.11.5) and "Continuous" snapshot support RimWorld 1.6 only (checked 2026-10-19). Run this section only when Multiplayer lists 1.7. Until then, record it in the session report as `FR-18: blocked (Multiplayer has no 1.7 build)`.

### Source audit (done 2026-10-19, no game needed)

The grep audit and its result are in ADR 0006, "Multiplayer audit (2026-10-19)". Summary: no `Designate*` call, no Multiplayer API, nothing written into the save, no `Rand` or game-time use. The only game-state calls are `SetStuffDef`, `DesignatorManager.Select` and `Designator_Dropdown.SetActiveDesignator`, the same calls the architect makes. Pass. Re-run the grep from the ADR before this session if `src/QuickMenu/Activation/` or `src/QuickMenu/Pinning/` changed since then.

### Steps (when Multiplayer supports 1.7, about 30 min)

You need two game clients: a second PC, a friend, or a second copy of the game started with `-savedatafolder=<another folder>` so the two clients do not share settings. Both clients must have exactly the same mod list.

e.0. Check that Multiplayer's `About/About.xml` (or its Workshop page) lists 1.7.
- If it does not, stop. FR-18 stays blocked.

e.1. On both clients, set the mod list to Harmony (`brrainz.harmony`), Multiplayer (`rwmt.Multiplayer`), all DLC, then Quick Menu (`sixdd.QuickMenu`). Turn on Dev mode.
- Expected: the game loads without red errors. The log has no `[QuickMenu]` error.

e.2. Host: Multiplayer, Host, load a colony that has wood, steel and at least two kinds of stone blocks on the map (and an ideoligion with a special building, if you can). Client: join it.
- Expected: both clients show the same colony, and the Multiplayer status shows no desync.

e.3. Host: in the architect, Ctrl+click a wall, Mine, one fine floor tile entry inside its dropdown and (if present) an Ideology building, to pin them.
- Expected: the "pinned" message and sound appear **on the host only**. The client sees nothing. No desync.

e.4. Host: open Quick Menu, choose Wall, pick **granite blocks** in the material ring, and drag a 5-cell wall on the map.
- Expected: the client sees 5 granite wall blueprints on the same cells. No desync.

e.5. Host: from the menu, choose Mine and drag over rock.
- Expected: the client sees the same mining designations.

e.6. Host: from the menu, open the floor sub-ring, choose the fine tile entry, then the material if asked, and place a 3x3 floor.
- Expected: the client sees the same floor blueprints with the same material. In the host's architect, that dropdown now shows the chosen entry as last used (local only).

e.7. Host (if present): place the Ideology building from the menu.
- Expected: the client sees the same building blueprint, with the same style.

e.8. Client: pin a wall in its own architect, then repeat e.4 from the client with **steel**.
- Expected: the host sees 5 steel wall blueprints. Pins on the two clients are separate (each has its own settings file).

e.9. Let the game run at speed 3 for one in-game day while both players open and close the menu now and then.
- Expected: no "Desynced" dialog and no Multiplayer desync report.

e.10. Host: run the debug action "Quick Menu: self-test".
- Expected: Multiplayer syncs debug actions, so the self-test block may appear in **both** logs. No desync and no error. This is expected and accepted (ADR 0006).

e.11. Host: save the game (Multiplayer save). Search the save file for the mod: `grep -c QuickMenu <save file>.rws`.
- Expected: `0`.

e.12. Copy from both clients: the `[QuickMenu]` lines of the log and any Multiplayer desync or sync-error lines that mention `QuickMenu`, `Designator_Build` or `DesignatorManager`.

**Pass**: e.4 to e.9 show identical blueprints and designations on both clients, no desync, no `[QuickMenu]` error, and e.11 gives 0. **Fail**: any desync or mismatch traced to a menu action becomes a bug task. A desync that also happens when the same thing is placed through the plain architect is a Multiplayer issue, not an FR-18 failure.

## FR-8 Pinning only in the architect menu (added 2026-10-19)

- Setup: any colony, vanilla architect, and also with Better Architect Menu if installed.
- f.1. Close the Architect tab. Select a pawn or a built thing, then hold the pin modifier and click a command-bar designator (including the Copy and Build gizmos).
- Expected: it activates normally and nothing is pinned (no "pinned" message or sound, the pin list is unchanged).
- f.2. Open the Architect tab (vanilla, then Better Architect Menu, then with Dubs Mint Menus active both its build grid and its wheel; the default modifier is Alt there) and modifier+click a building, an order and a dropdown entry.
- Expected: each one pins or unpins and does not activate.
- f.3. With Mint active, close all of these and use Mint's always-visible designator bars (if enabled in its settings): modifier+click does not pin there (known gap, bars are not detected).

## UC-6 final regression session

**Task**: T-75 (part 1: this guide). **Total**: about 3.5 h in 17 Parts of 15 min or less, with a break after any Part. **How it runs**: the agent relays one Part at a time in chat and reads `Player.log` and your screenshots itself. You click, look, and say `done` or what you saw. You do not copy logs. **Do not restart or close the game until the agent says it has read the log** (the log is rewritten at each start).

Marks: **[M]** = Must (every one must pass or you accept a documented exception), **[S]** = Should. FR/NFR tags show what a step covers.

### Rules for every Part

- Game in dev mode. Debug actions = the bug-icon menu; type `Quick Menu` in its filter box.
- "Screenshot" = F12. No copying needed: say `shot taken` and the agent takes the newest file from `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Screenshots/`.
- "Pin" = hold the pin key (Ctrl; Alt when Mint is loaded) and click. Pinning never activates the tool.
- If a step does not match "Expect", stop, say what you saw, and the agent decides whether to go on.

### Setups and launch options (paste into Steam, Properties, Launch Options)

DLC = `ludeon.rimworld.royalty,ludeon.rimworld.ideology,ludeon.rimworld.biotech,ludeon.rimworld.anomaly,ludeon.rimworld.odyssey` (drop the ones you do not own).

| Setup | Launch options |
|---|---|
| **(a)S** vanilla + stress mod | `-mod=brrainz.harmony,<DLC>,sixdd.QuickMenu,sixdd.QuickMenuStress -quicktest` |
| **(b)S** (a)S + Better Architect Menu | `-mod=brrainz.harmony,ferny.BetterArchitect,<DLC>,sixdd.QuickMenu,sixdd.QuickMenuStress -quicktest` |
| **(d)S** (b)S + Dubs Mint Menus | `-mod=brrainz.harmony,ferny.BetterArchitect,<DLC>,Dubwise.DubsMintMenus,sixdd.QuickMenu,sixdd.QuickMenuStress -quicktest` |
| **(c)S** Mint, no BAM | `-mod=brrainz.harmony,<DLC>,Dubwise.DubsMintMenus,sixdd.QuickMenu,sixdd.QuickMenuStress -quicktest` |
| **(R)** your real 1.7 list | no `-mod=`, keep `-quicktest` off; select your saved list in the Mods screen (Harmony first, Quick Menu last) |
| **(H)** Harmony + Quick Menu only | `-mod=brrainz.harmony,sixdd.QuickMenu -quicktest` |
| **(e)** Multiplayer | **Skipped**: Multiplayer has no 1.7 build (spec A-2). Recorded as `FR-18: blocked`. Section "FR-18 Multiplayer" above stays valid for later. |

The stress mod (2,000 buildings in tabs "stress A/B/C", some in dropdown groups) gives the large categories P-19 needs. All vanilla and BAM checks run with it on; (H), (R) and Part 16 are without it.

### Blocks used by several Parts

**Block P: pin-click and marker performance** (about 6 min; needs an Architect tab with many buttons, "stress A" or the Mint/BAM equivalent, and at least 5 pinned buttons visible in it):
P.1. Debug actions, **perf measuring (toggle)**. Expect the log line `perf measuring ON (counters reset)`; if it says OFF, toggle again. Do the same before each P step marked "reset".
P.2. Open the architect category. Move the mouse slowly over the buttons for 15 s. No clicks. Then **perf report**. *Agent reads*: pin click prefix `calls=0` (P-4, idle); pin marker hook: calls per frame, median us/frame <= 10, bytes/frame 0 (P-19).
P.3. Reset. Click one button, press Esc; repeat 20 times (no modifier). **perf report**. *Agent reads*: pin click prefix calls about 20 (P-3, plain click fast path), 0 bytes.
P.4. Reset. Pin the same button 20 times (toggles on/off, ends where it started). **perf report**. *Agent reads*: pin click prefix median <= 1 ms, bytes <= 2 KB per call (P-5).
P.5. Reset. Close the architect. Select a colonist so its command bar shows. Wait 15 s. **perf report**. *Agent reads*: marker hook calls > 0, median <= 10 us, bytes 0; dropdown-entry hooks `calls=0`.
P.6. Reset. Open the architect, plain-click a dropdown button (list opens), Esc, close the architect. Right-click the ground to open a vanilla float menu, hold 5 s, Esc. **perf report**. *Agent reads*: dropdown-entry hooks (`PinMarkerEntry`) `calls=0`.

### Part 0: Prep, terminal only (5 min, game closed)

0.1. `cd ~/Documents/Programming/rimworld-quick-menu && . scripts/_env.sh && dotnet build QuickMenu.slnx -c Release && scripts/link-mod.sh` [M]. Expect `Build succeeded`, `linked` or `already linked`.
0.2. `scripts/make-stress-mod.sh 2000 && scripts/link-stress-mod.sh`. Expect `linked`.
0.3. Move the old settings aside so the first-run notice shows: `mv ~/.config/unity3d/"Ludeon Studios"/"RimWorld by Ludeon Studios"/Config/Mod_QuickMenu_QuickMenuMod.xml{,.bak}` (the file may not exist; fine). You can move the `.bak` back at the very end.
0.4. Set launch options to **(a)S**. Say `done`.

### Part 1: (a)S fresh start, first run, settings (15 min)

*Agent reads*: self-test, log errors; screenshots.
1.1. Start the game, load or start the quick-test colony. Expect the first-run notice once, not blocking [M, T-67]. **Screenshot** (first-run notice). Dismiss it.
1.2. Open the menu with the trigger (Mouse 4). Expect an empty menu with the hint "Nothing pinned yet…" and an Edit hub [S, FR-16]. **Screenshot** (empty hint). Close with the trigger again.
1.3. Debug actions, **self-test**. *Agent reads*: `patched`, `failed=0`, `marker patch ... patched`, `dropdown marker patches ... map/row/grid patched`, the "own GizmoOnGUIInt" line, `mint=False`, `modCtorMs` (P-18 <= 150).
1.4. Options, Mod settings, Quick Menu. **Screenshot** (settings page) [T-70].
1.5. Click "Clear" on the binding. Expect `Unbound` and the trigger opens nothing [M, FR-1]. Click "Use middle mouse instead", test it opens, then rebind Mouse 4. Close settings.
1.6. Architect, Structure: pin Wall, Door. Orders: pin Mine. Expect a "Pinned … to the quick menu" message each time and the tool not activated [M, FR-8, FR-15]. Pin Wall again: "Removed". Pin it again.
1.7. Open the "stress A" tab: pin 6 stress buildings (keep one in mind: the first one). These make the missing-mod test in Part 16. [M]
1.8. Pin a research-locked building (one you cannot build yet). Open the menu: its slot is greyed with a lock. Hover: red reason line. **Screenshot** (greyed tooltip, T-70). Click it: reject sound and the reason message, menu stays open [M, FR-14]. If Ideology is on and a precept-gated build (e.g. Morbid slab) exists, pin it too and report its reason text [S].
1.9. Quit the game normally (not Alt+F4) after the agent has read the log. Start again with the same options, load the colony. Open the menu. Expect all pins still there in the same slots [M, FR-17].

### Part 2: (a)S core wheel and grid regression (15 min)

All [M]. Use the pins from Part 1.
2.1. Open at the screen centre and at all four corners. Expect the whole wheel on screen each time (FR-3). Time keeps running (FR-2).
2.2. Close it five ways, one per opening: trigger again, Esc, left-click outside, right-click, then select Mine. Nothing under the click happens; Esc does not open the pause menu (FR-2).
2.3. Click Wall. Expect the material ring in the same order each time; choose Steel, open again, choose Wood: the last used is highlighted, nothing moves (FR-12). Place a wall tile. Click Mine: the mining tool is selected (FR-13).
2.4. Pin a dropdown button (a stress dropdown or Floors dropdown), click it in the menu. Expect a sub-ring; for the fine-tile floors the options show stone block icons (FR-10, FR-11, FR-11a). Esc, right-click and the hub go back one level.
2.5. Edit: click the centre (Edit becomes Done). Drag one slot onto another (swap) and onto an empty slot (move); Ctrl+click a slot unpins it and the menu stays open; Done and Esc end editing (FR-9). Re-pin what you removed.
2.6. Settings: Shape Grid. Repeat 2.3 and 2.5 in the grid, plus the sub-grid and the material grid (FR-4). Back to Wheel.
2.7. Settings: Size Auto. Pin one more item: the menu grows. Unpin it: it shrinks (FR-5). Back to Fixed. Rings 1 and 3: the menu size follows next open (FR-6). Back to 2. Button size small and large: same.
2.8. Close the Architect tab, select a colonist, Ctrl+click Draft or any command-bar button. Expect its normal action and nothing pinned (FR-8, f.1).
2.9. With the Architect tab open, pin a building, an order and a dropdown entry: each pins and does not activate (FR-8, f.2, vanilla).

### Part 3: (a)S pin marker, vanilla (15 min, UI scale 1.0)

*Agent reads*: nothing; screenshots. The marker is a small pin at the top-right of a button, under the little "+" slot.
3.1. Architect, Structure. Wall (pinned in Part 1) shows a blue pin with a dark outline; unpinned buttons do not [M, FR-19].
3.2. Pin Wall (unpin). Expect the marker gone on the very next frame. Pin again: marker back next frame [M].
3.3. Orders tab: Mine shows the marker [M].
3.4. Go to a dropdown button (Floors tab, or a "stress" tab). Pin the dropdown button. Expect a full-size blue marker on the dropdown button and the list not opened [M].
3.5. Plain-click the dropdown button. In the open list **no entry** has a marker (whole-dropdown pin marks no entries) [M]. Esc.
3.6. Pin the dropdown button (unpin it, marker gone). Plain-click it, then pin one entry in the list. Vanilla closes the list. Expect on the button a **smaller, WHITE** marker [M, decision 14].
3.7. Plain-click the button again. Expect a small blue marker on that entry only [M]. **Screenshot** (list open, entry marker).
3.8. Pin the same entry in the list: it unpins, the list closes, the button's marker is gone. Reopen: no entry marker [M]. Pin the entry again.
3.9. **Screenshot** the Architect tab with at least 3 markers plus the dropdown button's white marker (UI 1.0).
3.10. Close the Architect. Select a colonist: no marker anywhere on its command bar. Select a built or planned thing whose type you pinned (e.g. a Wall) so its Copy/Build gizmo shows: no marker on it either [M, decision 12]. If no such thing exists, say `none on map`.
3.11. Settings: Fixed, Wheel, Rings 1 (6 slots). Make sure you have 8 pins. Open the Architect. A pinned button whose slot is **not visible** in the wheel has **no marker**; the others do [S, decision 13]. Set Rings back to 2; markers return.

### Part 4: (a)S UI scale 1.5 and 2.0 (12 min)

4.1. Options, Interface, UI scale 1.5. Settings, Rings 5, drag the Button size slider once, slowly, from one end to the other and let go [M, T-95]. Debug actions, **perf report**. *Agent reads*: `sized texture rebuilds` rose by about 1 per drag, last rebuild <= 5 ms with upload; no errors. Rings back to 2.
4.2. UI scale 2.0 (if the game refuses it, say so and use 1.75). Open the menu (wheel, then material ring, then Grid): sharp, on screen, readable [M, NFR-5]. **Screenshot** the wheel at this scale.
4.3. Architect: markers sharp, correctly placed on the button and in an open dropdown list, not covering the "+" slot [M, FR-19]. **Screenshot** (Architect with markers, scale 2.0) and **screenshot** (list open with entry marker).
4.4. Settings: Size Auto, Button size at its maximum. Pin items from a stress tab until a pin gives the message "The quick menu is full…" or the menu says "The menu is full." [M, FR-5, M5 carry-over]. Unpin one: room again. Back to Fixed, button size default.
4.5. UI scale back to 1.0.

### Part 5: (a)S performance and markers (12 min)

5.1. Debug actions, **self-test**. *Agent reads*: P-18 and no failed patches again.
5.2. Do **Block P** in the "stress A" tab (it has the pins from Part 1.7). [M for P-4, P-19]
5.3. Debug actions, **measure closed frames**. Leave the map idle for 3 min, hands off. *Agent reads*: closed-frame block, medianUs <= 5 (P-1), `allocatedBytesTotal=0` (P-2) [M].

### Part 6: (a)S fault injection, marker and pin sites (8 min)

Each fault is armed from Debug actions, **fault injection**, pick the item, then do what its line says. Expect exactly one error line in the log, then the feature stops and the game carries on. A site can be tested once per start. Perf measuring should be OFF.
6.1. Arm **PinMarkerPatch.GizmoOnGUIInt**. Open the Architect with pins: buttons keep drawing and working, no markers [M].
6.2. Arm **Textures.PinMarker**. Open the Architect: buttons draw, no markers (or markers stay off) [M].
6.3. Arm **DropdownMarkerPatch** (map). Plain-click a dropdown button: the list opens and works, no entry marker [M].
6.4. Arm **DropdownMarkerPatch.Draw**. Open a dropdown with a pinned entry: the list draws and works, no marker [M].
6.5. Arm **PinClickPatch.ProcessInput**. Pin a button in the Architect: the click is passed on and the tool activates as in vanilla; no pin [M].
6.6. Arm **DropdownEntryPatch.GetDesignatorSelectAction**. Open a dropdown, click an entry: it activates normally [M].
6.7. With all markers now off: the menu and its pins still work [M, NFR-6].
Say `done`; keep the game open for Part 7.

### Part 7: (a)S fault injection, remaining sites (12 min)

Same rules as Part 6. Do them in this order; the last ones switch the menu off.
7.1. **Settings.Page**: open mod settings; error label, the window closes normally [M].
7.2. **Settings.Preview**: open settings; the preview is missing, the rest works [M].
7.3. **BindingCaptureWindow**: click the binding box; the capture window closes itself [M].
7.4. **FirstRunNoticeWindow**: it opens the notice and closes it [M].
7.5. **Compat.Notice**: no visible effect; one error line only [M].
7.6. **Catalog.Ideology**: press the trigger or pin a button; vanilla and the menu still work [M].
7.7. **Textures.Sized**: open the menu; it opens without ring textures or closes; game continues [M].
7.8. **QuickMenuWindow.DoWindowContents**: press the trigger; the menu closes at once [M].
7.9. **MenuController.Open**: press the trigger; nothing opens [M].
7.10. **TriggerPoller.UIRootUpdate**: wait one frame; the trigger does nothing; pinning in the Architect still works [M].
7.11. Open the log window: no red errors besides one per armed site. Say `done`. Quit the game after the agent has read the log. Set launch options to **(b)S**.

### Part 8: (b)S Better Architect Menu, functional and markers (15 min)

8.1. Start, load. Open the menu: pins from Part 1 are all there [M]. Debug actions, **self-test**. *Agent reads*: `bam=True`, patches, `failed=0`, `modCtorMs` <= 300.
8.2. BAM Architect, Structure: pin Wall, Mine: pinned, tool not activated; pin again: unpinned [M, FR-8]. Move a building pin with BAM's own move button if you use it [S].
8.3. Markers on pinned buttons in each BAM grid: flat, grouped, and the orders page; set one pin per page [M, FR-19]. Unpin: gone next frame; pin: back next frame [M].
8.4. BAM Floors tab: a single entry (BAM shows entries as their own buttons) pinned shows a full marker on that button [M]. Pin a whole dropdown (stress group button, if BAM shows one): full marker on it [M].
8.5. Open a dropdown list (stress group or the one BAM keeps): pin one entry; reopen: small marker on that entry, and a smaller white marker on the dropdown button (if the button is drawn) [M]. **Screenshot**. If a BAM page has no dropdown list, say so; that is a known limit.
8.6. Close the Architect and select a colonist and a pinned building: no markers on command bars [M].
8.7. Menu: wheel, material ring, sub-ring work as in Part 2 [M, NFR-2]. **Screenshot** the BAM Architect with markers.

### Part 9: (b)S performance and BAM faults (12 min)

9.1. Do **Block P** in BAM's largest page (stress A) [M: P-4, P-3/P-5 with BAM, P-19].
9.2. Fault injection: **PinMarkerPatch.GizmoOnGUIInt**, then open BAM's Architect: buttons work, no markers [M]. Then **DropdownMarkerPatch.Draw** and open a list [M].
Say `done`. Quit after the agent has read the log. Set launch options to **(d)S**.

### Part 10: (d)S BAM + Dubs Mint Menus (15 min)

10.1. Start, load. If Mint's one-time notice appears, note it [M, T-67]. Self-test: `mint=True`, `effectiveModifier=Alt`, patches, `failed=0`.
10.2. Open Mint's architect window (its build menu, grid view). Alt+click Wall: pinned, not activated. Ctrl+click Wall: left to Mint, nothing pinned (with BAM loaded too) [M, FR-8, M5 carry-over]. Alt+click Wall again: unpinned.
10.3. Markers on pinned buttons in Mint's grid view; gone next frame after unpin, back after pin [M, FR-19]. Whole-dropdown pin: full marker; single-entry pin in its open list: marker on the entry and the white marker on the button, if Mint's list opens vanilla-style [S].
10.4. Switch Mint's view to **list view**: no markers there (known limit) [S, ADR 0009].
10.5. Open Mint's **wheel**: Alt+click pins/unpins there, but **no markers** are drawn in the wheel (by design) [M, f.2].
10.6. Select a colonist; open Mint's designator bars if enabled in its settings: no markers, no pinning there [S, f.3].
10.7. Mint-only checks: the Quick Menu wheel opens and works next to Mint's wheel [M, NFR-2]. **Screenshot** Mint's grid with markers.

### Part 11: (d)S performance (8 min)

11.1. **Block P** in Mint's architect grid (largest category); use Alt for the pin clicks in P.4 [M: P-3/P-4/P-5 with Mint, P-19].
11.2. Fault injection **PinMarkerPatch.GizmoOnGUIInt**, open Mint's window: works, no markers [S].
Say `done`. Quit after the log is read. Set launch options to **(c)S**.

### Part 12: (c)S Mint without BAM (8 min)

12.1. Start, load. Self-test: `mint=True bam=False`, `failed=0` [M].
12.2. Alt+click pins in Mint's architect window; markers show in the grid view; the wheel works; no marker in Mint's wheel [M].
12.3. Remaining sites left alone. Say `done`. Do not quit yet.

### Part 13: 8,000-def stress run (10 min)

13.1. Quit the game. Terminal: `scripts/make-stress-mod.sh 8000 && scripts/link-stress-mod.sh`. Start with **(c)S** launch options.
13.2. Load the quick-test map. Debug actions, **self-test**. *Agent reads*: catalog entries about 8,000+, `buildMs` (P-11c: <= 100, hard limit 200), `builds=1`, round trip 100 % [M, NFR-1].
13.3. Open the menu, open and close it 10 times: no stutter or errors. Open a stress tab in the Architect and scroll it: usable [S].
13.4. **Block P**, steps P.1 and P.2 only, in a stress tab: marker hook median <= 10 us [M].
13.5. Quit. Terminal: `scripts/make-stress-mod.sh 2000 && scripts/link-stress-mod.sh`. Set launch options to **(R)**.

### Part 14: (R) screenshots for the Preview and T-70 (15 min)

Use your real colony (an attractive base, daytime, clear weather, camera zoomed in). Pins: 6 to 8 varied ones (Wall, Floors dropdown, a chair/furniture dropdown, Mine, Door, a lamp), arranged by Edit.
14.1. Start, load your colony. Load a second save for a moment, then your colony again: the same pins show in both [M, FR-17].
14.2. Preview shots, each a clean wheel centred on a good spot, no debug windows, UI scale 1.0 [S]: (1) wheel with a sub-ring open; (2) the material ring of a wall; (3) the grid layout; (4) edit mode. Say `shot taken` after each.
14.3. T-70 shots: (5) edit-mode help panel (top centre) in the wheel; (6) a slot being dragged (drag ghost, hold the mouse); (7) last-used material highlighted while another slot is hovered; (8) grid with a sub-grid open [S].
14.4. Quick checks on the real list: the menu opens, a Wall can be placed, no red errors in the log; self-test `failed=0` [M, NFR-2].

### Part 15: (R) Frame Profiler cross-check (8 min)

15.1. Find the Frame Profiler in the dev tools (debug toolbar or dev menu). Set its filter to `QuickMenu!*`. If you cannot find it, say so: the cross-check is then recorded as not done [S].
15.2. Run it for 10 s with the menu closed and nothing selected. **Screenshot** the result. *Agent reads*: P-1 cost per frame of the trigger poller (Stopwatch median 0.2 us, expect the profiler to be the same order).
15.3. Open the Architect with markers and run it again, then with the menu open and a sub-ring. **Screenshots** [S].

### Part 16: (H) NFR-3 load test, missing pins, removal from a save (15 min)

16.1. Quit. Set launch options to **(H)** (Harmony and Quick Menu only; the stress mod is off).
16.2. Start. Expect the quick-test map loads, no red mod errors, no `[QuickMenu]` errors in the log [M, NFR-3]. Self-test: `failed=0`; About.xml needs nothing but Harmony.
16.3. Open the menu. Expect the six stress pins missing, kept (hidden) and the others shown [M, FR-14]. Mod settings: "N pins are from mods that are not loaded" with **Forget missing pins**.
16.4. Self-test: note `saves=` (settings writes). Click Forget missing pins. Expect a confirmation; Cancel does nothing. Click again, confirm. Expect the notice line goes away. Self-test again: `saves` rose by exactly 1 [M, T-66 carry-over].
16.5. Save the game as `qm-removal-test`. Quit.
16.6. Set launch options to `-mod=brrainz.harmony` (Quick Menu removed). Start, Load game, `qm-removal-test`. The game may warn that mods changed: accept. Expect it loads and runs for 30 s with no `QuickMenu` text and no red errors in the log [M, FR-17, NFR-6].
16.7. Quit. Put your launch options back to normal. Move `Mod_QuickMenu_QuickMenuMod.xml.bak` back if you want your old pins and settings. Run `scripts/link-stress-mod.sh --remove`. Say `done`.

### Not run

- Setup (e) Multiplayer: blocked (no 1.7 build).

### Time estimate

| Part | Setup | Min |
|---|---|---|
| 0 Prep | terminal | 5 |
| 1 Fresh start, settings | (a)S | 15 |
| 2 Core wheel and grid | (a)S | 15 |
| 3 Pin marker, vanilla | (a)S | 15 |
| 4 UI scale 1.5 / 2.0 | (a)S | 12 |
| 5 Performance, closed frames | (a)S | 12 |
| 6 Faults: marker and pin sites | (a)S | 8 |
| 7 Faults: remaining sites | (a)S | 12 |
| 8 BAM functional and markers | (b)S | 15 |
| 9 BAM performance, faults | (b)S | 12 |
| 10 BAM + Mint | (d)S | 15 |
| 11 Mint performance | (d)S | 8 |
| 12 Mint only | (c)S | 8 |
| 13 8,000-def stress | (c)S | 10 |
| 14 Preview and T-70 shots | (R) | 15 |
| 15 Frame Profiler | (R) | 8 |
| 16 NFR-3, Forget, removal | (H) | 15 |
| **Total** | 7 restarts | **about 200 (3.5 h)** |

### Pass/fail marks the agent will read

| Target | Where | Pass |
|---|---|---|
| Patches | self-test, every setup | `failed=0`; marker, map, row and grid patches `patched` |
| P-1 closed poller | closed-frame block | median <= 5 us; P-2 `allocatedBytesTotal=0` |
| P-3 plain-click prefix | perf report, vanilla, BAM, Mint | about 100 ns, 0 bytes |
| P-4 idle prefix calls | perf report after hover | `calls=0` in vanilla, BAM and Mint |
| P-5 pin click | perf report after 20 pins | median <= 1 ms, <= 2 KB |
| P-19 marker hook | perf report, largest category; colonist bar | median <= 10 us per frame, 0 bytes; dropdown-entry hooks `calls=0` after the float-menu check |
| P-11c catalog | self-test at 2,000 and 8,000 defs | <= 100 ms (hard 200), `builds=1`, round trip 100 % |
| P-18 startup | self-test | <= 150 ms (a); <= 300 ms (b)/(d) |
| Faults (NFR-6) | log, all 16 sites | one error line per armed site, site disabled, game continues |
| Slider rebuild | perf report at UI 1.5, 5 rings | about 1 per drag, <= 5 ms |
| Forget missing | self-test `saves=` | +1 exactly |
| Settings writes | self-test `saves=` | no growth while idle |
| FR-19 | your reports and screenshots | marker where listed, none on command bars, Mint wheel and list view; white marker on a partial dropdown; next-frame update |
| NFR-3 / FR-17 | Part 16 log | loads with Harmony + Quick Menu only; no errors after removal |
| NFR-5 | screenshots at 2.0 (or 1.75) | sharp, on screen |
| Multiplayer (e) | none | blocked, not failed |
