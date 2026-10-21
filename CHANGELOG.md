# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/).

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
