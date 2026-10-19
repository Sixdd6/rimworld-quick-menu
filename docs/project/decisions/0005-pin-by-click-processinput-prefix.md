# 0005. Pin by modifier+click through a Harmony prefix on every Designator.ProcessInput override

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

## Context
FR-8: modifier+click on any designator gizmo pins or unpins it. It must work in any architect UI (vanilla, Better Architect Menu, Mint's architect window) through a hook on the designator, and the normal click action must not happen. The default modifier is Ctrl, or Alt when Dubs Mint Menus is detected (with a one-time notice), and it is configurable.

Findings (1.7.4982 binaries and BAM 1.6 assembly, which is what loads on 1.7):
- Vanilla: `ArchitectCategoryTab.DesignationTabOnGUI` → `GizmoGridDrawer.DrawGizmoGrid`. After `Command.GizmoOnGUIInt` reports an interaction, it calls the **virtual** `Gizmo.ProcessInput(Event)`.
- BAM: its own `DrawFlatGrid`, `DrawGroupedGrid` and `DrawOrdersPanel` call `Gizmo.GizmoOnGUI` and then `ProcessGizmoInteractions`, which calls virtual `Gizmo.ProcessInput(Event)`. Exception: when Mint is active and Mint's wheel edit mode is on, BAM routes the click to Mint and uses the event first.
- Mint (behaviour only): its architect window and its wheel both activate designators by calling `ProcessInput`.
- Overrides: `Designator.ProcessInput` (base: interaction check → `Command.ProcessInput` (sound) → `DesignatorManager.Select`). `Designator_Build` (stuff float menu, then base), `Designator_Dropdown` (opens the float/grid menu, selects the active element), `Designator_AreaAllowed`, `Designator_Install` and `Designator_Plan_Add` override it. Mods can override it too. A virtual call dispatches straight to the most-derived override, so patching only the base method would miss `Designator_Build` and `Designator_Dropdown`, the two most important cases.

## Options considered
1. **Prefix on each declared `ProcessInput` override in the Designator hierarchy** (all loaded assemblies, found at startup by reflection): catches every path that "clicks" a designator, in any UI, because every UI converges on this virtual call. Cost: N patches (about 6 vanilla plus mods), applied once at startup.
2. **Prefix on `Designator.GizmoOnGUI` (and its overrides) that detects a modifier+MouseDown inside the gizmo rect**: independent of how the click is dispatched, but the rect must be reconstructed from `topLeft` and `GetWidth`. That is fragile when a UI draws gizmos at custom sizes. It also runs on every gizmo draw, every frame, which hurts NFR-1.
3. **Transpiler or prefix on `GizmoGridDrawer.DrawGizmoGrid`**: misses BAM and Mint. Rejected.
4. **Integrate with BAM directly** (like BAM's own Mint integration): couples us to BAM internals. Rejected (spec: work through the designator).

## Decision
- At startup, collect `typeof(Designator)` and all subclasses (`GenTypes.AllSubclasses`). For each, get `ProcessInput(Event)` with `DeclaredOnly`, skip abstract methods, de-duplicate, and patch each with one shared prefix `static bool Prefix(Designator __instance, Event ev)`. Each patch is wrapped in try/catch; failures are logged once and skipped.
- Prefix logic (allocation-free on the fast path):
  1. Return `true` (run the original) unless the effective modifier is held. The modifier is checked through `ev.control`/`ev.alt`/`ev.shift`, falling back to `Input.GetKey(Left/Right Ctrl|Alt|Shift)`.
  2. Return `true` if the event is a key event (hotkey activation) or not a left mouse button event (`ev.rawType` MouseDown/MouseUp, `ev.button == 0`). Spike S-3 confirms which fields are reliable after the event is `Used`.
  3. Re-entrancy guard: base `ProcessInput` reached from an already-handled override does nothing.
  4. `PinService.Toggle(PinKey.For(__instance))`, play a vanilla UI sound, show a message (Pinned / Unpinned / Menu is full), and return `false` so the original never runs (no float menu, no selection).
- **User decision U-3**: modifier+click on the dropdown **button** pins the whole dropdown (FR-10). Modifier+click on an **entry inside the open dropdown list** (vanilla float or grid menu) pins that single entry.
  - If spike S-3 shows the entry path calls the element's `ProcessInput`, the existing prefix covers it.
  - If not, add one conditional patch: a postfix on `RimWorld.Designator_Dropdown.GetDesignatorSelectAction` that wraps the returned action so a modifier click toggles the entry's pin instead of running the action. This would be the third patch site; it is listed in architecture §6 only if S-3 needs it.
- `ModifierPolicy` (Core): if `pinModifierUserSet` is true, use the user's choice; else use Alt if `ModsConfig.IsActive("Dubwise.DubsMintMenus")`, otherwise Ctrl. Mint detection runs once at startup, since the mod list cannot change without a restart. The effective modifier is cached in a static field and recomputed only when settings change, so the prefix fast path is a cached-field read plus the event checks. A one-time notice is shown the first time Mint is detected.

## Consequences
- Works in vanilla, BAM and Mint's architect without referencing any of them.
- The patch count grows with mods that override `ProcessInput`. The self-test reports it.
- If a mod's designator UI never calls `ProcessInput`, pin-by-click will not work for it. The picker (FR-7) still does.
- The JIT inlining risk (`JitPatchableAssemblies` lists only Assembly-CSharp) is low for virtual methods. Spike S-1 confirms that a mod-assembly override can be patched.
