# 0003. Custom trigger binding and per-frame input polling (resolves Q-2)

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

## Context
FR-1 asks for a trigger that is any keyboard key or mouse button 3–5 (default mouse button 4), plus an "unbound" state. Q-2 asked whether the game's `KeyBindingDef` system can capture mouse buttons. NFR-1 forbids measurable per-frame cost while the menu is closed.

Findings from the 1.7.4982 binaries (static IL inspection of `Assembly-CSharp.dll`, `UnityStubs.dll` and `RimWorld.dll`):
- `RimWorld.Dialog_DefineBinding.DoWindowContents` accepts a binding only when `Event.current.isKey && Event.current.type == KeyDown`. Mouse buttons can never be captured by the vanilla rebinding dialog.
- `KeyBindingDef.JustPressed` uses `Input.GetKeyDown(KeyCode)`. In UnityStubs that goes `GetKeyDownRaw → WasPressed → TryGetKeyControl`, which maps **keyboard keys only** to `Keyboard` controls. Even a `KeyCode.Mouse3` written into KeyPrefs would never fire. `KeyDownEvent` checks for a `KeyDown` event as well.
- `Input.GetMouseButtonDown(int)` maps indices 0–4 to `Mouse.leftButton, rightButton, middleButton, forwardButton, backButton` (Waylith `MouseButton`: Left, Right, Middle, Forward, Back). Five buttons are supported, so 1-based "mouse 3–5" are indices 2–4.
- `RimWorldWaylith.Imgui.ImguiDispatch.DispatchInput` creates IMGUI `MouseDown`, `MouseDrag` and `MouseUp` events only for buttons `0..2` (loop `b < 3`). Side buttons never reach `Event.current`. They are visible only through `Input.GetMouseButton*`.

**Answer to Q-2: no.** A custom binding control and polling are required.

## Options considered
1. **`KeyBindingDef` for keys + a separate mouse option**: keys appear in the vanilla Controls menu with conflict checks. But that means two settings for one trigger, the "unbound" handling is split, and mouse buttons still need custom code.
2. **Custom `TriggerBinding` (None / Key / Mouse) with our own capture dialog, polled once per frame**: one control, as the spec describes ("custom control accepting keys and mouse buttons"). We must check key conflicts ourselves.
3. **Poll native `Waylith.Input.InputState`**: direct and complete, but outside the supported UnityStubs surface, and it ties us to engine internals. Rejected for v1.

Polling site:
1. **Harmony postfix on `RimWorld.UIRoot_Play.UIRootUpdate`**: runs once per frame and only in play (not the main menu). Needs no save data.
2. **`GameComponent.GameComponentUpdate`**: also per frame, but GameComponents are written into save files, which violates FR-17 (removing the mod would cause load errors).
3. **`GameComponentOnGUI` or an OnGUI patch**: runs several times per frame, so the same press would need a frame guard.

## Decision
- `TriggerBinding` (Core) is `None`, `Key(int keyCode)` or `Mouse(int index)` with index 2–4. Its label is "Middle mouse" (index 2), "Mouse 4" (index 3), "Mouse 5" (index 4), or the key name. The default is `Mouse(3)` ("Mouse 4"). Left and right buttons cannot be bound.
- `BindingCaptureWindow`: keys come from IMGUI `KeyDown` events (Esc cancels). Mouse buttons come from polling `Input.GetMouseButtonDown(2..4)` in `WindowUpdate`. If the key is bound to a game `KeyBindingDef`, a conflict warning is shown, but the binding is not blocked. Settings has a "Clear" button (unbound) and a one-click "Use middle mouse" button. The first-run message offers the same option.
- `TriggerPoller` is a postfix on `UIRoot_Play.UIRootUpdate`. Body: an early return if `None` or the patch is disabled, then one call to `Input.GetMouseButtonDown(i)` or `Input.GetKeyDown(code)` (which already ignores keys typed into text fields). It opens the menu only when `Find.CurrentMap != null`, the world view is not shown, and no modal window is open. There are no allocations.
- Hold mode: while the menu is open, `QuickMenuWindow.WindowUpdate` polls `Input.GetMouseButtonUp(i)` / `Input.GetKeyUp(code)` for the release.

## Consequences
- Side buttons work even though IMGUI never sees them.
- Our trigger does not appear in the game's Controls menu. Conflicts are only warned about.
- Spike S-2 confirms in-game which physical side button is index 3 vs 4. Capture-by-press makes the mapping irrelevant to correctness; it matters only for the default's label.
- Middle mouse as trigger also starts vanilla camera drag. This is accepted, and documented in the settings tooltip.
- With middle mouse as the trigger the menu window keeps `preventCameraMotion = true`, because the camera drag would otherwise fight the menu. While the menu is open, keyboard and edge panning are frozen too. This is acceptable because the menu is open only briefly.

## Spike result (2026-10-19)

UC-1 session on RimWorld 1.7.4982, Harmony mod v3.0.1.0 (library 3.0.0-rc.3). Full table: `docs/project/spikes/uc-1-results.md`.

- **S-2 PASS.** Polling `Input.GetMouseButtonDown/Up` saw indices 0-4, both down and up, so hold mode is viable. A keyboard key was seen too.
- IMGUI delivered no events for the side buttons, so polling is required (as decided).
- Mapping on the user's mouse: back side button = index 3 = "Mouse 4"; front side button = index 4 = "Mouse 5". The default stays `Mouse(3)` labelled "Mouse 4". Capture-by-press still makes the mapping irrelevant to correctness.
