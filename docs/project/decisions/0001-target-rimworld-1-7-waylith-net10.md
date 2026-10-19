# 0001. Target RimWorld 1.7 (Waylith, .NET 10) with reference packages and Harmony 3

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

## Context
The spec targets RimWorld 1.7 only and uses C# and Harmony (spec §11, NFR-3). We inspected the installed game (`~/.steam/steam/steamapps/common/RimWorld`):

- `Version.txt`: `1.7.4982 rev1280`.
- `RimWorld.runtimeconfig.json`: `"tfm": "net10.0"`, framework `Microsoft.NETCore.App 10.0.13-waylithclr.dev`. The game runs on a patched CoreCLR, not Unity/Mono. Config flags include `AssemblyNameRedirects: UnityEngine*=UnityStubs`, `LaxAssemblyBinding`, and `JitPatchableAssemblies: Assembly-CSharp`.
- Managed game assemblies are in the install root: `Assembly-CSharp.dll` (game code, `Verse`/`RimWorld` namespaces), `UnityStubs.dll` (Unity API shim), `Waylith.dll` (engine), `Waylith.UnityCompat.dll`, and `RimWorld.dll` (host: bootstrap, IMGUI dispatch).
- The wiki page "RimWorld 1.7 Mod Updates" and Ludeon's Modder Primer say: target `net10.0` in an SDK-style csproj; remove Unity references; reference `Assembly-CSharp`, `UnityStubs` and `Waylith` (`RimWorld.dll` only if needed); set `AppendTargetFrameworkToOutputPath=false` and `GenerateDependencyFile=false`; 1.7 requires **Harmony 3** (NuGet `3.0-rc2` or the workshop mod); Harmony 3 type-checks `__instance` and `__result`.
- Workshop `brrainz.harmony` (2009463077) has a `1.7/Assemblies/0Harmony.dll` with version `3.0.0.0-rc.3` built for `.NETCoreApp,Version=v10.0`. NuGet has `Lib.Harmony 3.0.0-rc.3` (15 Oct 2026) with a `net10.0` group.
- NuGet has `Krafs.Rimworld.Ref 1.7.4982-unstable` (a `net10.0` group). It matches the installed build exactly.

## Options considered
1. **Compile against the local game DLLs** (`HintPath` to `$(RIMWORLD_DIR)`): always matches the installed game. But CI cannot build (the game is proprietary and not on CI runners), and every developer needs the game in a known path.
2. **Compile against `Krafs.Rimworld.Ref`**, with local DLLs as a fallback: CI builds without the game; versions are pinned in the project file. But the "unstable" package can lag a game patch by days, and we must confirm it contains `UnityStubs` and `Waylith` reference assemblies.
3. **Stay on the 1.6 setup (net472 + UnityEngine refs) and rely on the shim**: 1.6 mods mostly still load, but Ludeon's guidance is to retarget, Harmony 3 is required, and it would block use of any net10 API. Rejected.

For Harmony:
1. **`Lib.Harmony` NuGet, compile-only** (`ExcludeAssets=runtime`, `PrivateAssets=all`): the dependency is pinned and the runtime copy comes from the Harmony mod (NFR-3). The only available 3.x builds are release candidates.
2. **Reference the workshop `0Harmony.dll` directly**: same binary as at runtime, but not reproducible in CI.

## Decision
- Target `net10.0` with SDK-style projects (C# 14 default) and the .NET 10 SDK (10.0.1xx, pinned in `global.json`).
- Reference the game through `Krafs.Rimworld.Ref` `1.7.4982-unstable` (`PrivateAssets=all`, `ExcludeAssets=runtime`). A `UseLocalGameRefs` MSBuild switch replaces it with `HintPath` references to `$(RIMWORLD_DIR)/{Assembly-CSharp,UnityStubs,Waylith}.dll` (`Private=false`) for days when the package lags the game.
- Use `Lib.Harmony` `3.0.0-rc.3` compile-only (user decision U-5: stay on rc.3 until Harmony 3.0 final ships, then bump). Declare `brrainz.harmony` in About.xml `modDependencies` and `loadAfter`. Do not ship `0Harmony.dll`.
- Use the Unity API surface through UnityStubs (`UnityEngine.Input`, `Event`, `GUI`, `Texture2D`). Do not call native `Waylith.*` APIs in v1. The Modder Primer says there is no plan to move mods off UnityStubs.
- The environment-engineer verifies that the Krafs package contains UnityStubs and Waylith references. If it does not, local references become the default and CI builds Core plus tests only.

## Consequences
- CI can build and test without the game. Releases are reproducible.
- We depend on prerelease software (1.7 unstable branch, Harmony 3 rc). Each game update needs a version bump of the reference package, a rebuild, and the in-game self-test.
- Visual Studio 2022 and older Rider cannot build net10. The `dotnet` CLI (or VS2026 / Rider 2026) is required.
- No `Source/` folder ships in the mod; sources live in `src/`.

## Spike result (2026-10-19)

UC-1 session on RimWorld 1.7.4982, Harmony mod v3.0.1.0 (library 3.0.0-rc.3). Full table: `docs/project/spikes/uc-1-results.md`.

- **S-1 PASS.** The shared `ProcessInput` prefix patched 8 overrides with 0 failures, in 19-24 ms, both with only the DLCs and with Better Architect Menu (BAM) loaded. All 8 overrides are declared in `Assembly-CSharp`; BAM adds none.
- Harmony 3.0.0-rc.3 compile-only against the `brrainz.harmony` mod (v3.0.1.0) works on the patched CoreCLR. No change to this decision.
- Not yet shown: patching an override declared in a third-party mod assembly (no such override exists on the tested mod list). The per-method try/catch stays.
