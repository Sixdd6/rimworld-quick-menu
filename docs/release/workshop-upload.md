# Uploading Quick Menu 1.0.0 to the Steam Workshop (manual)

The upload is done by you, through the game's built-in uploader. Nothing here is automated, and no agent uploads anything.

How the 1.7 uploader works (checked by decompiling `Assembly-CSharp.dll`: `RimWorld.Page_ModsConfig`, `Verse.Steam.Workshop`, `Verse.ModMetaData`):

- It uploads the **whole folder of the mod as it sits in `Mods/`**. Your `Mods/QuickMenu` is a symlink to the repo `mod/` folder, which also holds the build output (`mod/1.7/Assemblies`) and could hold stray files. So upload a clean copy instead.
- The button is in the Mods screen, under **More actions**, and only shows when **dev mode is on**, Steam is running and the mod is in the `Mods/` folder (not the Workshop folder).
- On the first upload the game creates the item, sends the title, the `About.xml` description, `About/Preview.png` and the tags `Mod` and `1.7`, then writes `About/PublishedFileId.txt` into the uploaded folder. The game never sets visibility, so Steam creates the item as **Private**.
- Later uploads read `PublishedFileId.txt` and update the same item (the description is not re-sent).

Files for the upload are ready:

- `dist/workshop/QuickMenu/` is the clean folder to upload (the contents of `dist/QuickMenu-1.0.0.zip`: About, LoadFolders.xml, Languages, `1.7/Assemblies/{QuickMenu.dll,QuickMenu.Core.dll}`, LICENSE; no `0Harmony.dll`, no `.pdb`, no `PublishedFileId.txt`).
- To rebuild it: `scripts/package.sh`, then unzip into an empty `dist/workshop/`.

## Steps

All paths: `GAME=~/.steam/steam/steamapps/common/RimWorld`.

1. Close RimWorld and RimSort. Check that Steam is running and logged in as the account that should own the item.
2. Swap the symlink for the clean folder (the repo is not touched, and the packageId must exist only once in `Mods/`):
   ```
   rm "$GAME/Mods/QuickMenu"            # removes the symlink only, not the repo
   cp -r dist/workshop/QuickMenu "$GAME/Mods/QuickMenu"
   ```
   Run `rm` on the link without a trailing slash. Check with `ls -ld "$GAME/Mods/QuickMenu"` that it is now a real directory.
3. Start RimWorld from Steam with the mods `brrainz.harmony` and `sixdd.QuickMenu` active (RimSort can launch it), and turn on **Development mode** in the options.
4. Open **Options -> Mods** (the mod list screen). Select **Quick Menu**. Click **More actions -> Upload to Steam Workshop**.
5. In the confirmation dialog leave "Tag as translation" off. Untick **Build asset pack** if it is ticked (Quick Menu has no textures to bake; this uploads the files exactly as they are). Click **Confirm**, then answer **Yes** to "Did you create this content yourself?".
6. Wait for the "uploaded" message. The game opens the item's Workshop page in Steam.
7. On that page (it is **Private** until you change it):
   - Replace the description with the text of `docs/release/workshop-description.txt` (Steam BBCode; paste it as it is). The game sent only the short `About.xml` text.
   - Check that the preview image is the one from `About/Preview.png`.
   - Tags: the game set `Mod` and `1.7`. Add none, or any extra Steam tags you want.
   - Set **Visibility** (Public, Friends-only, Unlisted or Private) as you decide. Accept the Workshop legal agreement if Steam asks.
8. Quit the game. Copy the id file back so later updates go to the same item, and commit it:
   ```
   cp "$GAME/Mods/QuickMenu/About/PublishedFileId.txt" mod/About/PublishedFileId.txt
   git add mod/About/PublishedFileId.txt
   git commit -m "chore(release): record Workshop item id"
   ```
   The id is public (it is in the item URL), so committing it is fine. Note that `scripts/package.sh` will then ship this file in later zips. That is correct for Workshop updates. Do not copy it into the GitHub zip if you do not want it there.
9. Restore the dev setup:
   ```
   rm -r "$GAME/Mods/QuickMenu"         # the copied folder, never the repo
   scripts/link-mod.sh
   ```
10. Verify: subscribe to the item from a second Steam account or check the Workshop page, start the game with the Workshop copy only (disable the local mod first to avoid a duplicate packageId), load a save and check that the menu opens.

## Updating later

Bump `modVersion` and `Directory.Build.props` `Version`, add a CHANGELOG entry, run `scripts/package.sh`, repeat steps 2 to 4 (the button then reads **Update on Steam Workshop**), and step 8 is not needed again. Put the change notes in the item's Change Notes tab on Steam (the game sends a generic text only).
