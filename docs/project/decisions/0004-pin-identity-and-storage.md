# 0004. Pin identity keys, fallbacks and global storage (resolves Q-1)

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

## Context
Pins must survive game restarts, apply across all saves (FR-17), never duplicate (FR-15), represent whole dropdowns (FR-10), and be kept when their mod is removed so they return in place when it is re-added (FR-14). Designator instances are rebuilt each session, so pins need a stable string identity.

How 1.7 builds the architect designators (from `DesignationCategoryDef.ResolveDesignators`):
- For each `specialDesignatorClasses` entry it creates an instance with `Activator.CreateInstance` (orders, zones, areas, plans, and mod tools).
- For each `ThingDef` and `TerrainDef` whose `designationCategory` is this category: if `designatorDropdown` (a `DesignatorDropdownGroupDef`) is set, the build goes into one shared `Designator_Dropdown` per group def; otherwise it becomes a standalone `Designator_Build`.
- `Designator_Dropdown` has **no field referring to its group def**. The group can only be found from its elements' `PlacingDef.designatorDropdown`.
- Ideology adds per-ideo build designators and dropdowns through `AllResolvedAndIdeoDesignators`.
- Mods can add `Designator_Dropdown` instances (or subclasses) that are not tied to any def, for example by patching `ResolveDesignators` or by using a special designator class.
- `BuildCopyCommandUtility.FindAllowedDesignator(BuildableDef)` finds the architect designator for a def anywhere, including inside dropdowns.

## Options considered
1. **Def names only** (what the spec sketched): simple and stable, but it cannot identify orders (no def), dropdowns (no def reference), or mod-made dropdowns.
2. **Category + index in `resolvedDesignators`**: covers everything, but breaks whenever any mod adds or removes a designator or BAM reorders. Rejected.
3. **Typed key per designator kind with ordered fallbacks** (chosen): stable for defs and types, with a fuzzy fallback only for the one case that has no stable identity.
4. **Store a hash of the label**: breaks on translation and label changes. Rejected.

## Decision
Key grammar (implemented in `Core.Pins.PinKey`, round-trip tested):

| Kind | Key | Applies to |
|---|---|---|
| Build | `build:thing/<defName>` or `build:terrain/<defName>` | `Designator_Build` (`ThingDef` and `TerrainDef` have separate defName spaces, hence the sub-kind) |
| Def dropdown | `dropdown:<DesignatorDropdownGroupDef.defName>` | `Designator_Dropdown` whose elements are all `Designator_Build` with the same `designatorDropdown` |
| Order / special | `order:<Type.FullName>` plus `#<n>` only when the same type appears more than once (n = position in category order, then category order of defs) | any other `Designator` (Mine, Deconstruct, zones, areas, mod tools) |
| Mod dropdown | `moddrop:<Type.FullName>@<categoryDefName>~<childKey>,<childKey>,...` (children sorted) | `Designator_Dropdown` or subclasses not matching "Def dropdown" |

Resolution (adapter `DesignatorCatalog.Resolve(key)`):
1. Exact lookup in the session's key → designator map (built from the catalog).
2. `build:`: if the def exists but has no exact entry, `BuildCopyCommandUtility.FindAllowedDesignator(def)`. This covers builds moved or regrouped by other mods.
3. `moddrop:`: `ModDropdownMatcher`. Same type is required. Score = Jaccard overlap of child keys, at least 0.5. Same category breaks ties, then the first match.
4. Otherwise the pin is **unresolved**. It is kept unchanged and written back unchanged. Display by sizing:
   - **Fixed**: the slot is reserved (pin-by-click and the picker skip it). It draws as empty in use mode. In edit mode it shows "?" with the stored label and mod in the tooltip.
   - **Auto**: hidden (left out of the layout), keeping its order.
   - In both modes, settings shows the number of missing pins and a **"Forget missing pins"** button. It calls `PinBoard.ForgetUnresolved`, which removes only unresolved pins, after a confirmation dialog, then writes settings once. Right-click clear in edit mode removes a single one.

Storage:
- `QuickMenuSettings : ModSettings` (global XML in the game's Config folder). `pins` is a `List<string>` of v1 lines `slot|key|packageId|label`, with `^`-escaping: `^`, `|` and `\` are written `^^`, `^|` and `^/` in each field. A backslash never appears in a stored line because the game's string loader (`ParseHelper.ParseString`, verified in the 1.7.4982 assembly) rewrites backslash + `n` to a newline, which would corrupt a label. `schemaVersion` = 1.
- Numbers, enums and flags are scribed as text (enums by name) and parsed in `QuickMenuSettings`. One corrupt value is one repaired setting and one warning, not a red error per field from the game's own parser. Format 1 has not been released, so this needed no migration.
- On save, `schemaVersion` is always the current `SettingsRules.SchemaVersion`, never a newer value read from the file. Saving with an older mod after a newer one therefore stamps the older version, and the newer version's data may be lost on that downgrade (pin lines it wrote stay as opaque lines and survive). Writing the loaded version would claim a format this build did not write.
- Pins resolve at startup by exact key only. The `FindAllowedDesignator` fallback needs a loaded game (it evaluates designator visibility), so it runs only once `Current.Game` exists. `PinService.Refresh()` (also triggered by a catalog version change or a game load) resolves again. `PinService.IsReady` is false until pins are resolved against a non-empty catalog. While it is false, the missing-pin count and "Forget missing pins" are disabled.
- `packageId` comes from the def's `modContentPack`, or for types from the mod that owns the assembly. It is used only for the missing-pin marker.
- No GameComponent, WorldComponent or MapComponent, and no defs referenced from saves (FR-17).
- Duplicate check (FR-15) is on the key, so pinning a resolved-by-fallback designator whose key differs from a stored unresolved key does not create a visible duplicate. On resolve, the stored key is kept, not rewritten.

## Consequences
- Every architect designator gets a key. Def- and type-based keys are stable across mod-list changes.
- Mod dropdowns may be matched fuzzily. In the worst case a pin shows as missing, but it is never lost.
- Spike S-4 runs the round-trip self-test on the user's large mod list to measure uniqueness and the resolve rate.
- The line format is unit-tested independently of Scribe.

## Spike result (2026-10-19)

UC-1 session on RimWorld 1.7.4982, Harmony mod v3.0.1.0 (library 3.0.0-rc.3). Full table: `docs/project/spikes/uc-1-results.md`.

- **S-4 PASS.** The `Format -> TryParse -> Resolve` round trip returned the same designator for every entry: 454 entries (Harmony + 5 DLC + Quick Menu) and 718 entries (plus BAM). 0 failures, 0 duplicate keys, `complete=True`, 0 skipped.
- No key-scheme change is needed, and no follow-up task is added.
