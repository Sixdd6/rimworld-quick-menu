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
