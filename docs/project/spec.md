# Product Specification: rimworld-quick-menu

- **Status**: approved (2026-10-18)
- **Source idea**: [idea.md](idea.md)

## 1. Summary
A standalone RimWorld mod that provides a radial "quick menu" for architect designators, modelled on the Mint wheel from Dubs Mint Menus. It does not require Mint Menus. It exists so players who only want the wheel get it without the rest of Mint Menus, with problems fixed and a different UX. In particular it works with alternative architect menus such as Better Architect Menu, and it can hold whole sub-menus.

## 2. Problem and goals
- **Problem**: The Mint wheel is only available as part of Dubs Mint Menus, which bundles many other menus. In Mint Menus you add items to the wheel by ctrl-clicking them in the architect menu, which does not work with alternative architect menu mods such as Better Architect Menu. Only single designators can be added: a mod's sub-menu designator (a dropdown, for example a group of chairs) can only be pinned by its top item.
- **Goals** (measurable):
  - Any designator or buildable object reachable from the architect menu can be pinned to the menu, whichever architect menu UI is installed (vanilla, Better Architect Menu, …).
  - Sub-menu designators (dropdowns) can be pinned as a whole menu, not just their top item.
  - v1 is a finished, usable quick menu on RimWorld 1.7.
- **Non-goals** (explicitly out of scope):
  - Mint Menus' other features: recipe/health bill lists, plant selection UI, research menu, build menu, resource pinning.
  - Depending on, patching, or integrating with Dubs Mint Menus.
  - Pawn, building and other non-architect gizmos (orders such as Mine are architect designators and are in scope).
  - Per-save or multiple profiles/wheels.
  - RimWorld versions other than 1.7 (for now).

## 3. Users and personas
| Persona | Description | Primary needs |
|---|---|---|
| Heavily modded builder | Plays with many mods, so the architect menu is long; may use Better Architect Menu | Fast access to the 10–40 things they build most, from any architect UI |
| Mint wheel refugee | Used the Mint wheel but wants it without the rest of Mint Menus, or with BAM | Familiar wheel behaviour and look |

## 4. User journeys
1. **First run**: player binds a key or mouse button in mod settings → presses it on a colony map → an empty menu opens with a hint and an edit button.
2. **Pin from picker**: opens menu → clicks edit → opens the picker → searches "chair" → picks a mod's chair dropdown → it fills a slot as a whole sub-menu.
3. **Pin by click**: while browsing the architect menu (vanilla or BAM), Ctrl+clicks (Alt+click if Mint is installed) a designator → it is pinned (a message confirms; or says the menu is full).
4. **Use**: presses trigger → menu opens centred on the mouse → clicks the chair slot → sub-ring fans out → clicks an armchair → material sub-ring appears → picks wood → the designator is selected ready to place → menu closes.
5. **Edit**: in edit mode drags slots to swap them, right-clicks to clear one, changes layout to grid in settings.
6. **Mod removed**: player removes a mod whose items were pinned → those slots are hidden but kept; re-adding the mod brings them back.

## 5. Functional requirements
| ID | Requirement | Priority | Acceptance criteria (Given / When / Then) |
|---|---|---|---|
| FR-1 | Open the menu with a user-bindable trigger: any keyboard key or mouse button (including mouse buttons 3–5) | Must | Given a key or mouse button is bound in settings, when the player presses it on a colony map, then the menu opens. Default binding is mouse button 4. Mice do not report how many buttons they have, so the game cannot detect a missing side button: the mod settings and the first-run message offer a one-click "use middle mouse" option. Given the binding is cleared, then nothing opens and settings show "unbound". |
| FR-2 | Trigger mode setting: press-to-toggle (default) or hold-and-release | Must | Given toggle mode, when the trigger is pressed, then the menu opens; pressing again, Esc, or clicking outside closes it. Given hold mode, when the trigger is released over a slot, then that slot is activated; released elsewhere, the menu closes. While held, hovering a sub-menu or material slot opens its next ring (hover-open applies only in hold mode); releasing over an option picks it. |
| FR-3 | The menu opens centred on the mouse and dodges screen edges | Must | Given the mouse is near a screen edge, when the menu opens, then it is shifted the minimum amount so no slot is off-screen. Given the menu is bigger than the screen, then it is centred on the screen. |
| FR-4 | Two layouts, wheel (rings around the mouse) and grid (rows/columns around the mouse), with the same features | Must | Given either layout is selected, when the player uses any menu feature (pin, select, sub-menu, material ring, rearrange, edit), then it works in both. |
| FR-5 | Two sizing models: Fixed (set number of slots that keep their place; empty ones shown) and Auto (grows as items are added) | Must | Given Fixed, when a slot is cleared, then other slots keep their place and an empty slot is shown. Given Auto, when an item is added, then the menu grows (new ring/row as needed) until it would go off-screen, after which it counts as full. Given Fixed and full, when pinning, then a "menu is full" message appears. |
| FR-6 | Size settings: rings (wheel), rows/columns (grid), button size | Must | Given the player changes a size setting, when the menu next opens, then it uses the new size. |
| FR-7 | Pin from a searchable picker listing every architect-menu designator, independent of which architect UI is installed | Must | Given Better Architect Menu (or vanilla) is installed, when the player opens the picker from edit mode and chooses an item, then it is pinned. Search matches label, category and mod name. Locked items show greyed; dev-only items show only in dev mode. |
| FR-8 | Pin by modifier+click (default Ctrl; default switches to Alt when Dubs Mint Menus is detected, with a one-time notice; configurable) on any designator gizmo, at any time, via a hook on the designator rather than the vanilla architect window | Must | Given Better Architect Menu or vanilla, when the player Ctrl+clicks a designator, then it is pinned (or unpinned if already pinned) and the normal click action does not happen. Given Mint Menus is installed and the modifier was not changed, then Alt+click pins and Ctrl+click is left to Mint. |
| FR-9 | Edit mode inside the open menu only: the **centre button of the wheel/grid is the Edit button** (it becomes Done in edit mode; Done or Esc ends it). In edit mode: add via the picker (FR-7), drag to swap slots, **pin-modifier+click (Ctrl by default) on a slot unpins it** (same gesture as in the architect menu; right-click does not unpin), click a slot first to insert the next pins in front of it (Auto) or into it (Fixed). The menu is closed with Esc, a click outside, right-click or the trigger — the centre does not close it (user, UC-2 2026-10-19). | Must | Given the menu is open, when the player clicks the centre, then edit mode starts and the centre shows Done. Given edit mode, when the player drags slot A onto slot B, then they swap; when they Ctrl+click a slot, then it is unpinned. |
| FR-10 | Pin a sub-menu (dropdown) designator as a whole | Must | Given a mod or vanilla adds a dropdown designator (e.g. chairs), when the player pins it, then the slot represents the whole dropdown, not its top item. |
| FR-11 | A pinned sub-menu expands into a sub-ring (wheel) or sub-grid next to the slot (grid) | Must | Given a pinned dropdown slot, when the player clicks it (wheel or grid), then its options fan out as a sub-ring (wheel) or a sub-grid beside the slot (grid) and any option can be selected. |
| FR-12 | Build designators made from materials (stuff) open a material sub-ring to choose the material | Must | Given a stuff-made building slot, when the player selects it, then a sub-ring of the materials the architect menu would offer appears, last-used first and highlighted; choosing one selects the designator with that material. Given only one material is available, then the ring is skipped. Given none is available, then the slot is greyed with a "no materials" tooltip. Buildings without materials are selected directly. Ideology style variants use the game's default style. |
| FR-13 | Selecting a slot activates its designator exactly as the architect menu would (sounds, tooltips, selection) | Must | Given a pinned designator, when the player selects it, then the designator is selected and ready to place/designate, and the menu closes. |
| FR-14 | Unavailable pins: any hidden or unavailable designator (research, Ideology precept or ideoligion building, Biotech mechanitor, missing resources, mod visibility check) is always shown greyed with the reason — never hidden or shown as an empty slot (user, UC-2 2026-10-19); pins whose mod is missing are hidden but kept | Must | Given a pin is unavailable, then its slot is greyed and the tooltip gives the game's reason; clicking it plays the vanilla reject sound and shows that reason. Given its mod is removed, then the slot is hidden; when the mod is re-added, then the pin returns in its place. |
| FR-15 | No duplicate pins | Must | Given an item is already pinned, when the player pins it again, then it is unpinned (toggle) rather than duplicated. |
| FR-16 | Empty-menu state shows a hint and the edit button | Should | Given nothing is pinned, when the menu opens, then a hint explains how to pin. |
| FR-17 | Pins and settings are global mod settings, shared across saves; the mod writes nothing into save files | Must | Given pins were made in save A, when save B is loaded, then the same pins appear. Removing the mod from a save causes no errors. |
| FR-18 | Works with Multiplayer: the menu is local UI only; designator actions go through the game's normal paths so Multiplayer syncs them | Should | Given a Multiplayer session, when a player uses the menu, then placements sync like architect-menu placements and no desync occurs. |

## 6. Non-functional requirements
| ID | Category | Requirement | How it is verified |
|---|---|---|---|
| NFR-1 | Performance | No measurable per-frame cost while the menu is closed (no per-frame allocations); opens within one frame; picker index built once in under 200 ms with a large mod list | Profiling in-game (dev mode / Dubs Performance Analyzer) on the test setup |
| NFR-2 | Compatibility | Works on RimWorld 1.7 with vanilla architect menu, with Better Architect Menu, and with Dubs Mint Menus installed at the same time; supports DLC designators where present | Manual in-game test checklist on each setup |
| NFR-3 | Compatibility | Harmony is the only dependency, declared in About.xml | About.xml review; load test without other mods |
| NFR-4 | i18n | English only in v1; every player-facing string in translatable Keyed XML | Grep for hard-coded strings in review |
| NFR-5 | Accessibility | Button size adjustable; respects the game's UI scale setting | Manual check at UI scale 1.0 and 2.0 |
| NFR-6 | Reliability | Missing defs, removed mods, or a failing hook never throw errors that break the game; errors log once | Manual test with a pinned mod removed; log review |
| NFR-7 | Legal | Clean-room: Mint Menus is used to study behaviour only; no decompiled code or Mint assets are copied | Code review |
| NFR-8 | Testability | Layout maths (ring/grid placement, edge dodging, sizing) and pin identity/serialisation are unit-tested outside the game | Unit test suite |

## 7. Interface and UX
- **Look and behaviour**: based on the Mint wheel in Dubs Mint Menus. The user is happy with the reference look for now; style changes may come later.
- **Layouts**: wheel (rings around the mouse) and grid (rows/columns around the mouse), with identical features.
- **Opening**: centred on the mouse position, dodging screen edges. Only on a colony map; does not pause the game. Closes on Esc, click outside, after a selection, or when leaving the map.
- **Pinning**: searchable picker in edit mode (edit mode lives inside the open menu) + modifier+click hook on designators at any time (Ctrl, or Alt when Mint is installed). Pinned sub-menus expand on click as a sub-ring/sub-grid; stuff-made builds open a material sub-ring.
- **Mod settings window**: trigger binding (custom control accepting keys and mouse buttons), trigger mode, layout, sizing, rings/rows/columns, button size, pin modifier key.
- **Defaults**: mouse button 4 trigger (one-click switch to middle mouse), toggle mode, wheel layout, Fixed sizing, 2 rings, reference-like button size.

## 8. Media and assets
- **Visual style**: close to the Mint wheel reference, using vanilla RimWorld UI style.
- **Graphics needed**: slot backgrounds, highlight, edit/close icons. Designator icons come from the game.
- **Audio needed**: none beyond vanilla UI sounds.
- **Asset source**: the game's own textures and code-drawn shapes. A few small icons may be made by the team. No Mint Menus assets.
- **Constraints**: anything new is MIT-compatible or made by us; logged in `assets.md`.

## 9. Data
- **Pin**: a stable identifier for the pinned item — the building def name for build designators, the dropdown def / designator type for sub-menus and orders — plus its slot position. Exact identity scheme for mod-made dropdowns that are not defs is to be decided in architecture (spike).
- **Settings**: trigger binding, trigger mode, layout, sizing, sizes, modifier key.
- Stored in RimWorld global mod settings (XML). No personal data. Unknown pins are kept as-is so they return if their mod returns.

## 10. Integrations and external services
| Service | Purpose | Auth | Cost / limits |
|---|---|---|---|
| Harmony (library) | Patching designator click handling for Ctrl+click pinning | — | Free |
| Better Architect Menu (mod, optional) | Compatibility target | — | — |
| Multiplayer (mod, optional) | Compatibility target | — | — |
| Steam Workshop | Distribution | User's Steam account | Free |

## 11. Constraints and preferences
- Standalone: must not require Dubs Mint Menus. Mint Menus (in `reference/`, gitignored) is a design reference only; no code or assets are copied from it.
- Target RimWorld **1.7** first; later versions added as the game updates.
- C# / .NET Framework as RimWorld mods require; Harmony for patches.

## 12. Release and distribution
- Steam Workshop for players; public GitHub repository for source.
- License: MIT.
- Semantic versioning; About.xml with packageId, Harmony dependency, supported version 1.7.

## 13. Success criteria
A finished and usable quick menu: the user can pin anything from the architect menu (including whole sub-menus), with Better Architect Menu installed, open the menu in play and use it to place builds and designations, in both wheel and grid layouts.

## 14. Future scope
- Ideology style picker for pinned builds.
- Edit mode as a persistent overlay that allows click-to-pin in the architect menu.
- Visual style changes.
- Other RimWorld versions.
- Designator bars, multiple wheels/profiles, per-save overrides.
- Translations.

## 15. Open questions and assumptions
| ID | Question / assumption | Status |
|---|---|---|
| A-1 | Close behaviour, defaults, sub-menu click, picker scope, material ring, assets and test approach as written in §5–§8 | confirmed by user 2026-10-18 |
| Q-1 | Stable identity for mod-made dropdown designators that are not defs | open — architecture spike |
| A-2 | Better Architect Menu and Multiplayer may not have 1.7 releases yet (1.7 appears to be an unstable beta). If BAM is unavailable, its check runs on the newest build that loads on 1.7, or is recorded as blocked (not failed). If Multiplayer is unavailable, FR-18 is verified when it supports 1.7; meanwhile all actions go through the game's normal designator paths. | confirmed 2026-10-18 |
| A-3 | Workshop upload from the user's Steam account, with an original preview image in About/ | confirmed 2026-10-18 |
| Q-2 | Can the vanilla key-binding system capture mouse buttons 3–5, or is a custom binding control needed | open — architecture spike |
