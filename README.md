# Quick Menu

A radial wheel (or grid) of your favourite build and order buttons that opens around the mouse in RimWorld 1.7.

Pin the things you place most. Press one button, pick, and place. No more digging through architect tabs.

## Features

- **Wheel or grid.** Rings around the mouse, or rows and columns.
- **Pin from the architect menu.** Ctrl+click any build or order button to pin it. Ctrl+click again to unpin.
- **Material ring.** Pick a stuff-made building and a ring of materials opens. Choose one and the designator is ready to place.
- **Dropdown sub-rings.** Pinned dropdowns (floors, zones, and so on) fan out into a sub-ring (wheel) or sub-grid (grid).
- **Edit mode.** The centre button is Edit. Drag a slot onto another to swap, or onto an empty slot to move it. It becomes Done.
- **Unavailable pins stay.** A pin you cannot use yet is greyed out, and its tooltip says why (missing research, building, tech level, and so on).
- **Pin markers.** Pinned buttons in the architect menu show a marker: a blue marker on a pinned button, a small white marker on a dropdown that has only single entries pinned, and a small marker on pinned entries inside an open dropdown list.
- **Fixed or auto size.** Fixed keeps every slot in place and shows empty ones. Auto grows with your pins.
- **Configurable.** Open with a mouse button or any key. Change rings, columns, rows and button size.
- **Safe to add or remove mid-save.** Quick Menu writes nothing into save files. Your pins live in the mod settings.

## Install

Quick Menu needs [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).

1. Subscribe to Harmony and to Quick Menu on the Steam Workshop. Or download the zip from [Releases](https://github.com/Sixdd6/rimworld-quick-menu/releases) and unpack the `QuickMenu` folder into your RimWorld `Mods` folder.
2. Enable both in the mod list. Put Harmony first, then Quick Menu below it.

## How to use

1. Open a colony map and press **Mouse 4** (the default). No side button? Press **middle mouse**, or set any key in the mod settings. A first-run message offers a one-click switch to middle mouse.
2. Open the architect menu. **Ctrl+click** a build or order button to pin it. It goes into the first empty slot (Fixed) or the end of the menu (Auto). If Dubs Mint Menus is loaded, use **Alt+click** instead.
3. Press your menu button and click a pin. Materials and dropdown entries open as a ring beside it.
4. **Right-click**, click outside the menu, press Esc, or press the trigger again to close it.

To unpin: Ctrl+click the button again in the architect menu, or Ctrl+click the slot in the wheel (Alt with Mint). In edit mode you can also drag slots to rearrange them.

The pin modifier (Ctrl, Alt or Shift) is a setting.

## Settings

Open **Options > Mod settings > Quick Menu**.

| Setting | Notes |
|---|---|
| Open menu with | Mouse 4, Mouse 5, middle mouse, or any key. Default: Mouse 4. "Use middle mouse instead" is one click. Left and right click are not allowed. |
| Pin with | Ctrl, Alt or Shift, plus click. Default: Ctrl (Alt when Dubs Mint Menus is loaded). |
| Clear all pins... | Removes every pin, after confirmation. |
| Shape | Wheel or Grid. Default: Wheel. |
| Size | Fixed or Auto. Default: Fixed. |
| Rings | Wheel size. Default: 2. |
| Columns, Rows | Grid size. Default: 5 x 3. |
| Button size | Default: 56. |
| Forget missing pins | Removes pins from mods that are not loaded. Without it, those pins come back when you add the mod back. |
| Reset to defaults | Restores the options. Your pins are kept. |

## Compatibility

- **Vanilla architect menu**: pinning, markers.
- **Better Architect Menu**: pinning and markers, in the menu it draws inside the Architect tab.
- **Dubs Mint Menus**: pinning from its build menu and from its wheel, with Alt+click. See the known limits.
- **Harmony**: required, loaded before Quick Menu.

Pinning works only in architect menus. It never applies to the command buttons of selected things.

## Known limits

- Dubs Mint Menus: the list view shows no pin markers. The wheel shows no markers either. Mint's designator bars are not supported.
- Better Architect Menu logs its own error, "InitializeDubsMintMenusReflection: toggleDesMethod is null". That message comes from Better Architect Menu, not from Quick Menu.
- Multiplayer is not supported yet. There is no Multiplayer build for RimWorld 1.7.
- Pins are stored in the mod settings, not in the save. They are shared by all your games.

## Build from source

You need git and a Linux, macOS or Windows shell. The setup script installs the .NET 10 SDK to `~/.dotnet` if it is missing.

```sh
scripts/setup.sh                          # install SDK if needed, restore
dotnet build QuickMenu.slnx -c Release    # output goes to mod/1.7/Assemblies/
scripts/test.sh                           # unit tests plus the 90 % Core coverage gate
scripts/perf.sh                           # performance gates
scripts/link-mod.sh                       # symlink mod/ into the game's Mods folder
scripts/package.sh                        # writes dist/QuickMenu-<version>.zip
```

Details are in [docs/project/environment.md](docs/project/environment.md), and the design is in [docs/project/architecture.md](docs/project/architecture.md). Game references come from the `Krafs.Rimworld.Ref` package, so you do not need RimWorld installed to build.

To test in game, launch RimWorld with `-mod=brrainz.harmony,sixdd.QuickMenu -quicktest`.

## License

[MIT](LICENSE). The release changelog is in [CHANGELOG.md](CHANGELOG.md).

## Credits

Inspired by the quick menu of Dubs Mint Menus. Quick Menu is a clean-room implementation: no code, art or text was reused. Thanks to Andreas Pardeike for Harmony.
