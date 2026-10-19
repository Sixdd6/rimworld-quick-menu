# 0007. Host the menu in a full-screen transparent Verse.Window drawn with IMGUI

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

## Context
The menu opens centred on the mouse, does not pause, closes on Esc, on an outside click, after a selection, or when leaving the map. Sub-rings and the material ring can extend beyond the main wheel. Edit mode needs drag and right-click. NFR-1 asks for zero cost while closed and opening within one frame. On 1.7, UI is IMGUI implemented by Ludeon's host (`RimWorldWaylith.Imgui`). Ludeon advises against migrating Imgui to native Waylith. Both BAM and Mint patch `GUIUtility.RotateAroundPivot`, which suggests rotation is fragile under the shim.

## Options considered
1. **Draw from a Harmony postfix on an OnGUI root (for example `MapInterfaceOnGUI_AfterMainTabs`)**: no window object, but we would have to handle input absorption, layering, Esc and outside-click ourselves, and the hook runs every frame even when closed (an early return, but still a call).
2. **A `Verse.Window` sized to the wheel's bounding box**: the window stack handles Esc and click-outside. But sub-rings outside the rect need resizing, and "outside" clicks on sub-rings would close the menu.
3. **A full-screen, transparent `Verse.Window`** (`doWindowBackground=false`, `drawShadow=false`, `forcePause=false`, `closeOnCancel=true`, `absorbInputAroundWindow=false`, no title or close button, `layer = WindowLayer.GameUI` or Super, decided in S-6): one rect covers every ring. Our Core hit test decides between slot and outside, and an outside click closes. Map clicks are absorbed while open, so they do not designate. It exists only while open.

## Decision
- Option 3. `QuickMenuWindow` is added to `Find.WindowStack` in the same frame as the trigger. `DoWindowContents` draws from a cached `SlotPlacement[]`. `WindowUpdate` handles the hold-mode release poll and closes when `Find.CurrentMap` changes or the world view opens.
- All drawing uses vanilla IMGUI helpers (`Widgets`, `GUI.DrawTexture`, `TooltipHandler.TipRegion`, `Command.DrawIcon` for designator icons so icon proportions and stuff colour match the architect). No custom shaders. No rotated IMGUI drawing (`GUI.matrix` / `RotateAroundPivot`).
- Round shapes (slot discs, rings, highlights, rounded panels, badges) are **grayscale masks generated at runtime by a Core rasterizer** (`QuickMenu.Core.Render.MaskRasterizer`, pure and unit-tested). The adapter creates `Texture2D`s from them at the needed size and tints them with `GUI.color`. Fallback: **shipped PNG masks under `mod/Textures/QuickMenu/UI/`**, loaded via `ContentFinder<Texture2D>` and listed in `assets.md`. Last resort: the game's **asset baker**. Spike **S-7** checks runtime texture creation, loose PNG loading and 9-slice drawing on 1.7, and decides which level is used.
- `PickerWindow` is a normal vanilla-style dialog with the vanilla `QuickSearchWidget`.

## Consequences
- Simple, robust input handling with no per-frame cost while closed.
- While the menu is open, other UI under it does not get hover tooltips. This is acceptable for a transient menu.
- Spike S-6 confirms window layering, map-click absorption and same-frame opening on 1.7.
- Generated masks have no external asset licensing and scale cleanly with button size and UI scale. The fallback PNGs must stay in sync with the rasterizer (they can be produced by it).
