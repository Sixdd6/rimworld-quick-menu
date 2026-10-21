# Release audit (T-74)

Date: 2026-10-19. Scope: NFR-7 (clean-room), NFR-3 (dependencies), assets.

## Clean-room (NFR-7)
- `git ls-files reference` is empty; `git check-ignore reference` matches `.gitignore:25`.
- Keyed diff: our 84 keys (`mod/Languages/English/Keyed/*.xml`) against Mint's 124 (`reference/Languages/English/Keyed/MintMenus.xml`), parsed with a script, comparing keys, normalised values, and a 0.8 similarity ratio.
  - Key names shared: 0 (ours are `QuickMenu.`-prefixed).
  - Identical values: 6, all single generic UI words that describe the same setting: Layout, Grid, Wheel, Size, Auto, Fixed. Not copyrightable text. No longer string is identical.
  - Near duplicates: 1 ("Button size" vs "Button size: {0}"), a generic label.
- Mint is referenced only by packageId (`Dubwise.DubsMintMenus`, `ModIds.cs`), two window type names looked up by reflection (`DubsMintMenus.MainTabWindow_MintArchitect`, `...MainTabWindow_MayaMenu`) and our own notice text.
- Textures: the repository holds no texture, font or sound in `mod/`. Masks and icons are generated at runtime.

## Dependencies (NFR-3)
- `About.xml`: `modDependencies` has only `brrainz.harmony`; `loadAfter` `brrainz.harmony`; `supportedVersions` 1.7; packageId `sixdd.QuickMenu`; author, url (public GitHub repo) and description present.
- Recommendation (not applied): no `loadAfter` for Mint or BAM. We reach Mint by reflection at runtime and patch only vanilla methods, so load order does not matter. Add it only if UC-6 shows an order problem.
- `dotnet list package --vulnerable --include-transitive`: no vulnerable packages in any of the four projects.
- Licences: Lib.Harmony MIT; xunit.v3 Apache-2.0; BenchmarkDotNet MIT; Microsoft.Testing.Extensions.* Microsoft licence; Krafs.Rimworld.Ref "reference purposes" (Ludeon). The last four are test or compile-only and none is shipped. Harmony is `ExcludeAssets=runtime`.

## Package (`scripts/package.sh`, dist/QuickMenu-0.1.0.zip, 18 entries)
- Contents: `QuickMenu.dll`, `QuickMenu.Core.dll`, `LICENSE`, `LoadFolders.xml`, `About/About.xml`, 6 Keyed XML files. No `0Harmony`, pdb, stress-mod files, textures or Preview.png.
- Script guards passed: no `0Harmony.dll`, and no `$HOME` or `/home/` string in either DLL (ASCII and UTF-16).

## Assets
- Every tracked non-code file outside `.claude/` is in `assets.md` (mockups, spike screenshots, LICENSE). No fonts, audio or models are tracked.

## Fixes made
- Added root `LICENSE` (MIT, Sixdd6); `package.sh` ships it as `QuickMenu/LICENSE`; assets.md row added.

## Open items for later tasks
- `mod/About/Preview.png` does not exist yet (T-75..T-80 release prep); add its row to assets.md then.
- `About.xml` description is minimal; the Workshop description is a later task.
