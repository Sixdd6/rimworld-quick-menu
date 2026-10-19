# 0006. Designator catalog from DesignationCategoryDefs; activation through the game's own select path

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

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
  - order / non-stuff build / mod designator: `designator.ProcessInput(Event.current)`. Our pin prefix ignores it because no modifier click is involved.
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
