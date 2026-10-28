# 0006. Designator catalog from DesignationCategoryDefs; activation through the game's own select path

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

> **Superseded in part (2026-10-19).** The **picker** part of this ADR is superseded: spec FR-7 is now Won't (user decision after UC-2), so there is no picker window, no `SearchEntry` records and no `SearchIndex`. References below to "the picker", FR-7, search entries and the dev-only filter for the picker are historical. **The catalog decision stands**: the `DesignatorCatalog` is still built once at startup from `DesignationCategoryDef`s (designator table and key map), with the Ideology refresh, and it still resolves pins, feeds `AvailabilityResolver`, `StuffOptions` and the self-test. Activation is unchanged. See architecture.md section 3.1.

> **Update (2026-10-19, UC-3).** FR-12 now keeps the architect's **fixed material order** (never re-sorted); the last-used material is only highlighted, so "last-used first" below is historical. FR-11a: a dropdown whose entries are one item in different materials (e.g. fine stone tiles) shows each option with its material's icon (`MaterialVariants`, `MaterialVariantIcons`). Activation is unchanged.

## Context
- FR-7: the picker lists every architect designator, whichever architect UI is installed.
- FR-13: selecting a slot activates the designator exactly as the architect menu would.
- FR-12: stuff-made builds show a material ring with the materials the architect would offer, last-used first.
- FR-14: unavailable designators are greyed with a reason.
- FR-18: everything must stay Multiplayer-safe.

Findings (1.7.4982):
- All architect designators live in `DesignationCategoryDef` instances (`AllResolvedAndIdeoDesignators`; `ResolvedAllowedDesignators` filters the allowed ones). BAM regroups and adds categories but still uses `DesignationCategoryDef` and `ResolvedAllowedDesignators`.
- `Designator_Build.ProcessInput` for stuff builds: it lists the stuffs found in `Map.resourceCounter.AllCountedAmounts` that `IsStuff` and `stuffProps.CanMake(def)`, and that are present on the map (or god mode is on). It sorts them, makes a float menu whose option calls `SetStuffDef(stuff)` and then the base select path, and shows "NoStuffsToBuildWith" if the list is empty. BAM transpiles this method to change the material menu.
- `Designator.ProcessInput` (base) = `CheckCanInteract` (tutor) → `Command.ProcessInput` (plays the activate sound) → `Find.DesignatorManager.Select(this)`.
- `Designator_Build.Visible` hides builds for: tech level range, research not finished, monolith level (Anomaly), difficulty (`AllowedToBuild`), `PlaceWorker.IsBuildDesignatorVisible`, building prerequisites, discovery prerequisites (hidden items), and the inspected grav engine (Odyssey). None of these produces a reason string.
- Multiplayer (rwmt) syncs the `Designate*` calls and designator state at designation time. Selection and UI are local. (General knowledge of the mod; it is not installed and not known to support 1.7, see A-2.)

## Options considered
Activation:
1. **Call `designator.ProcessInput(Event.current)` for everything**: identical to the architect. But for stuff builds it opens the vanilla (or BAM-modified) float menu instead of our ring, and for dropdowns it opens the vanilla dropdown menu instead of our sub-ring.
2. **Plain designators via `ProcessInput`; stuff builds via `SetStuffDef(stuff)` plus the same steps as base `ProcessInput` (interaction check, `CurActivateSound`, `DesignatorManager.Select`); dropdowns expand in our UI, and the chosen element is then activated the same way** (chosen): the end state is identical to the architect, and our ring replaces only the menus the spec replaces.
3. **Construct new `Designator_Build` instances**: breaks "exactly as the architect" (per-instance state such as last stuff and style is lost). Rejected.

Catalog:
1. **Enumerate `DefDatabase<DesignationCategoryDef>` → `AllResolvedAndIdeoDesignators`, flattening dropdown children** (chosen): independent of the architect UI, the same instances the architect uses, and it includes Ideology designators.
2. **Read BAM's or vanilla's tab caches**: tied to one UI. Rejected.

## Decision
- `DesignatorCatalog` is built **once at startup**, in a `[StaticConstructorOnStartup]` class, during the loading screen after defs and designators are resolved. Each entry holds: designator, `PinKey`, label, category label, mod name, parent dropdown (if any), and dev-only flag (Spike S-5). The same pass builds the key → designator map and the Core `SearchIndex` from plain `SearchEntry` records (no game types in Core). Stored pins are resolved against the map at the end of that pass. Opening the menu or the picker therefore does no catalog work, and NFR-1's "opens within one frame" also holds for the first open.
- The **Ideology part** (`AllIdeoDesignators`, which depends on the loaded game and the player's ideoligion) is not built at startup, because no game exists yet. On each open the catalog compares an O(1) signature: (`Current.Game` reference, player's primary `Ideo` reference, that ideo's precept count). It rebuilds the Ideology entries only when the signature changes, which happens once per game load or ideo change (budget P-7c applies to the check only).
- Alternatives recorded:
  - Lazy build on first open: cheapest at startup, but the first open could take up to the 200 ms index budget, so it is not "within one frame". Rejected after the performance-engineer's review (C-1).
  - A background-thread build: game defs are not thread-safe. Rejected.
- Startup cost: the build counts toward loading time (P-11c, at most 200 ms on a large list), not toward a gameplay frame. The self-test reports it.
- `DesignatorActivator.Activate(entry, stuff?)`:
  - order / non-stuff build / mod designator: `designator.ProcessInput(Event.current)`. The menu wraps this call in `MenuActivation.Depth++/--` (try/finally); the pin prefix returns `true` immediately while `Depth > 0`, so a Ctrl/Alt held during activation (hold mode, Alt trigger) is never treated as a pin gesture.
  - stuff build: if `stuff` is given, `SetStuffDef(stuff)`, then `CheckCanInteract` → play `CurActivateSound` → `Find.DesignatorManager.Select(designator)`.
  - dropdown: expands in our UI (FR-11). The chosen element is activated as above. `SetActiveDesignator` is called on the dropdown so the architect shows the same "last used" element.
- `StuffOptions` reproduces the vanilla availability rule described above (behaviour, not code). Last-used = the designator's current `StuffDefRaw`, so the order is shared with the architect menu. One option skips the ring. Zero options greys the slot with the vanilla "NoStuffsToBuildWith" text.
- `AvailabilityResolver`: if `Disabled`, use `disabledReason`. If not `Visible`, compose the reason from the causes listed above using game strings where they exist (for example research project labels), otherwise the keyed `QuickMenu.Unavailable` fallback, "Not available right now" (user decision U-4). Clicking a greyed slot plays `SoundDefOf.ClickReject` and shows the reason as a `RejectInput` message.
- Multiplayer: no use of the Multiplayer API, no sync workers. The menu is a local `Window`. Only selection happens through our code, and placement goes through the game's own `Designate*` methods.

## Consequences
- Loading takes slightly longer (budgeted, measured by the self-test). In exchange, every open, including the first, is a cached-data operation.
- If another mod adds architect designators after startup (rare; for example by calling `ResolveDesignators` again), they appear only after a restart. The self-test reports catalog size versus live category contents to detect this.
- The placement experience after selection is the vanilla one (or the one changed by other mods).
- When BAM's material-menu changes apply to `Designator_Build.ProcessInput`, our ring bypasses them by design (the ring is a spec feature).
- If Multiplayer turns out to sync `SetStuffDef` or `Select` differently on 1.7, FR-18 needs a revisit. It is verified when Multiplayer supports 1.7 (A-2).

## Spike result (2026-10-19)

UC-1 session on RimWorld 1.7.4982, Harmony mod v3.0.1.0 (library 3.0.0-rc.3). Full table: `docs/project/spikes/uc-1-results.md`.

- **S-4:** 100 % round trip, 0 duplicate keys, 454 entries (Harmony + 5 DLC) and 718 (plus BAM). No key fixes needed.
- **S-5, dev-only rule:** no designator in either list is dev-only by type name (`DevNameHint`). `Designator_Build.Visible` returns true under god mode before its research/tech/prerequisite checks, so "visible only in god mode" does not mean dev-only: 243 god-only entries are explained by research, research prerequisites, place workers or tech level. Six god-only entries have no known cause (`build:terrain/Substructure`, `build:thing/HoldingSpot`, `dropdown:Floor_Tile_Metal`, `dropdown:Floor_Carpet`, `dropdown:Floor_Carpet_Fine`, `order:RimWorld.Designator_ZoneAdd_Fishing`); they are probably DLC or other gates, not dev tools. **Rule:** hide an entry from the picker (FR-7) as dev-only only when its designator type is clearly a debug type (type-name hint); every other god-only entry is "unavailable" and gets a composed reason.
- **S-5, reasons:** `disabledReason` was never set on any entry, so `AvailabilityResolver` must compose every reason itself. Sources: `researchPrerequisites` / `IsResearchFinished` (research project labels), `buildingPrerequisites`, `PlaceWorkers`, and `minTechLevelToBuild`/`maxTechLevelToBuild`. Anything else falls back to the keyed "Not available right now" (U-4).
- Catalog build 5.2 ms (718 entries: 5.3 ms), one build per session (P-11c, P-12 met).

## Multiplayer audit (2026-10-19)

T-68, FR-18, A-2. Read-only audit of `src/QuickMenu/**` (Core references no game assembly, so it cannot touch game state). Command used:

```
grep -rnE "Designate(SingleCell|MultiCell|Thing)|GenSpawn|GenConstruct|PlaceBlueprint|designationManager|AddDesignation|Rand\.|System\.Random|Time\.|TickManager|TicksGame|ProcessInput|SetStuffDef|SetActiveDesignator|DesignatorManager|Multiplayer|Scribe|GameComponent|MapComponent|WorldComponent|forcePause|Find\.Selector" --include=*.cs src/QuickMenu
```

**Result: no finding blocks FR-18.**

| Check | Result |
|---|---|
| `Designate*`, `GenSpawn`, `GenConstruct`, blueprint placement, `designationManager` | **None.** Placement and designation happen later in the game's own `Designate*` methods, called by `DesignatorManager` when the player clicks the map. |
| Multiplayer API, sync workers, `rwmt` references | **None.** |
| Game state written by our code | Only three calls, all in `Activation/DesignatorActivator.cs`, all made by the architect itself: `Designator_Build.SetStuffDef` (the shared designator's chosen material), `Find.DesignatorManager.Select` (local selection), `Designator_Dropdown.SetActiveDesignator` (the dropdown's last-used element). The plain path calls `designator.ProcessInput(Event.current)`, which is exactly the architect's click. `CheckCanInteract` (tutor gate) is invoked through reflection and only reads. |
| Save file | **Nothing written.** No `GameComponent`/`MapComponent`/`WorldComponent`. `Scribe` is used only in `QuickMenuSettings.ExposeData` (the per-player mod settings file). Pins therefore differ per player, which is correct for local UI. |
| Randomness / time | No `Rand`, no `System.Random`. `Time.frameCount` and `Time.realtimeSinceStartup` feed only the pin-click deduper, the first-frame log, perf counters and the control-run window: local UI and diagnostics, never game logic. No tick or game-speed access; every window has `forcePause = false`. |
| Reads of game state | `Find.CurrentMap`, `map.resourceCounter` (material ring), `Faction.OfPlayer` and its ideos (Ideology refresh, unavailable reasons), `DesignationCategoryDef` designator lists, `Current.Game`. Read-only. |
| Pin prefix on `Designator.ProcessInput` returning `false` | Only on a modifier+left-click gesture, and then it skips the designator's own `ProcessInput` (selection or opening its float menu), which is local UI. It toggles a pin in the settings file, shows a non-historical `Messages.Message` and plays a sound: all local. Multiplayer does not patch `Designator.ProcessInput` (it syncs `Designate*`), so the two do not interact. The dropdown-entry postfix is the same: on the gesture it skips the entry's select action. |
| Local feedback | `Messages.Message(..., historical: false)` and `PlayOneShotOnCamera` only, both client-local. |

Multiplayer behaviour checked against its source (`rwmt/Multiplayer` default branch, commit `4a3be27`, 2026-04-29, About.xml lists 1.6 only, "Requires RimWorld >= 1.6.4491"):
- It prefixes `Designator.DesignateSingleCell`/`DesignateMultiCell`/`DesignateThing` and sends the designator as a sync command. The `Designator_Build` sync worker writes `PlacingDef`, `placingRot`, `stuffDef` (for stuff builds) and `sourcePrecept`, and builds a **new** `Designator_Build` from them on every client. So the material we set with `SetStuffDef` travels with the placement, and the designator instance we hold in the catalog does not matter to the other clients.
- It does not patch `ProcessInput`, `DesignatorManager.Select`, `SetStuffDef` or `SetActiveDesignator` for architect designators, so our local selection path needs nothing from it. (It does sync a lambda inside `Designator_MechControlGroup.ProcessInput`, a pawn-command designator that is not in our catalog. Our pin prefix now rejects it, so it behaves normally and our code never touches it. Pin gestures are also accepted only while the Architect tab is open.)
- It syncs **every dev-mode debug action** through its `DebugActionNode.Enter` patch. Our "Quick Menu" debug actions (self-test, catalog dump, perf measuring, perf report, control window) would then run on every client. They only log, measure or open a local window and change no game state, so this cannot desync, but other players would see the log output or window. Accepted (dev mode only); revisit if it annoys anyone.

Installed: **not installed** on the user's machine. No workshop item under `~/.steam/steam/steamapps/workshop/content/294100` has packageId `rwmt.Multiplayer` (seven other mods only mention it in their load-order lists). Latest release v0.11.5 and the "Continuous" snapshot both target 1.6; there is no 1.7 build (checked on github.com/rwmt/Multiplayer/releases on 2026-10-19).

Status: FR-18 is **blocked, not failed** (A-2). The in-game check is item FR-18 in `docs/project/test-checklist.md` (setup (e)), to run when Multiplayer lists 1.7. Residual risk: Multiplayer's 1.7 port could change how it identifies designators. The checklist steps cover stuff builds, a dropdown entry, an order and an Ideology building so that any such change shows up.

## Amendment 2026-10-27: following categories that another mod rebuilds

With the user's full mod list (859 mods) the self-test found every static entry stale: after startup some mod resolved the
architect categories again, so `AllResolvedDesignators` held new instances and the catalog kept detached ones. The menu
then activated copies the game no longer drew, and state a mod sets on its live instance (More Planning's visibility icon)
never reached them. The catalog now keeps a snapshot per category (its list, count, first and last instance) and compares it
from `RefreshIdeology` (O(category count), no allocation). A mismatch rebuilds the static part (about 22 ms with 859 mods);
pin keys are stable, so pins resolve again. At most `MaxStaleRebuilds` (10) such rebuilds per session, then the catalog stays
as it is and logs once. The self-test reports `staleRebuilds` and lists stale entries.
