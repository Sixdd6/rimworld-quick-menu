# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/).

## [1.0.1] - 2026-10-27

Fixes found by testing with a large mod list (about 860 mods).

### Fixed

- Cancel, Deconstruct and other order buttons pinned from Better Architect Menu now appear in the quick menu. Before, they showed the "pinned" message but no button. Pins made with 1.0.0 are repaired automatically.
- Pin markers now show on Better Architect Menu's own copies of buttons: order buttons and whole dropdowns (for example cobblestone walls). Clicking such a copy pins and unpins the same pin.
- Pin markers now show on mod buttons that draw themselves, such as More Planning's Cut, Copy, Paste, Plan and Remove plans.
- More Planning's Plan and Remove plans show their own icons in the quick menu instead of a red cross. Other buttons without an icon show a grey dot.
- Labels in the quick menu are up to date, for example More Planning's "Opacity: 60%".
- Pins keep working after a mod rebuilds the architect menus while the game runs (for example RIMMSqol when its settings are applied).

### Changed

- More Planning's colour squares can't be pinned. They are settings, not tools; pin its Plan tool, which uses the selected colour.

### Known issues

- More Planning: after RIMMSqol rebuilds the architect menus, Planning Visibility has no icon until it is clicked once. More Planning sets that icon only when a game loads or when it is toggled.

## [1.0.0] - 2026-10-21

Initial release, for RimWorld 1.7.

### Added

- Quick menu that opens around the mouse as a wheel of rings or a grid of rows and columns.
- Open with Mouse 4 (default), Mouse 5, middle mouse, or any key. One-click "Use middle mouse instead" in the first-run message and in settings.
- Pin any build or order button with Ctrl+click in the architect menu. Ctrl+click again to unpin, or Ctrl+click a slot in the menu.
- Pin modifier setting: Ctrl, Alt or Shift. Alt is the default when Dubs Mint Menus is loaded, with a one-time notice.
- Edit mode from the centre button: drag to swap or move slots, then Done.
- Material ring for buildings made from stuff, with the last-used material highlighted.
- Sub-rings (wheel) and sub-grids (grid) for pinned dropdowns.
- Greyed-out pins with a tooltip that gives the reason (research, buildings, tech level, ideology, and so on).
- Pins from mods that are not loaded are kept and return with the mod. "Forget missing pins" removes them.
- Fixed and Auto sizing; settings for rings, columns, rows and button size; "Clear all pins..." and "Reset to defaults".
- Pin markers on pinned architect buttons, on dropdowns with only single entries pinned, and on pinned entries in open dropdown lists.
- Support for the vanilla architect menu, Better Architect Menu, and the Dubs Mint Menus build menu and wheel (pinning).
- Nothing is written to save files, so the mod can be added or removed mid-save.

### Known issues

- Dubs Mint Menus: no pin markers in the list view or the wheel. Mint's designator bars are not supported.
- Better Architect Menu logs its own error "InitializeDubsMintMenusReflection: toggleDesMethod is null". It is not caused by Quick Menu.
- Multiplayer is not supported yet. There is no Multiplayer build for RimWorld 1.7.

[1.0.0]: https://github.com/Sixdd6/rimworld-quick-menu/releases/tag/v1.0.0
