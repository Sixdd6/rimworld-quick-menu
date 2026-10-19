# 0008. Repository layout: shipped `mod/` folder, sources in `src/`, CI without the game

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

## Context
The mod ships to Steam Workshop (the in-game uploader sends the whole mod folder) and to GitHub (source plus release zip), under the MIT license. The repository also holds agent docs (`docs/`, `.claude/`) and the gitignored `reference/`, none of which may ship. RimWorld 1.7 loads assemblies from version folders through `LoadFolders.xml`. Development needs a fast build-and-launch loop on Linux.

## Options considered
1. **The repo root is the mod folder** (common in RimWorld mods): no copy step. But the uploader would ship `docs/`, `.claude/`, tests and so on unless they are excluded by hand each time.
2. **A dedicated `mod/` folder that is exactly the shipped content; the build writes DLLs into `mod/1.7/Assemblies`; development symlinks `mod/` into `RimWorld/Mods/`** (chosen): what you test is what you ship, and nothing extra leaks.
3. **Assemble a `dist/` folder by script for every run**: clean, but adds a copy step to every iteration.

## Decision
- Layout as in architecture.md §8: `mod/`, `src/QuickMenu.Core`, `src/QuickMenu`, `tests/QuickMenu.Core.Tests`, `tests/QuickMenu.Core.Benchmarks`, `tests/Shared/SyntheticCatalog.cs`, `scripts/`, `Directory.Build.props`, `global.json`, `QuickMenu.slnx`.
- `tests/QuickMenu.Core.Benchmarks`: a BenchmarkDotNet console app (net10.0, `MemoryDiagnoser`) that references Core only. Its output (`BenchmarkDotNet.Artifacts/`) is gitignored.
- `tests/Shared/SyntheticCatalog.cs`: one seeded generator of plain Core `SearchEntry` data (performance.md §2), linked into both test projects with `<Compile Include="..\Shared\SyntheticCatalog.cs" Link="Shared\SyntheticCatalog.cs" />`. It is not a separate assembly, so no extra project needs to be shipped or versioned.
- `mod/LoadFolders.xml`: `<v1.7><li>/</li><li>1.7</li></v1.7>`. `mod/1.7/Assemblies/` is gitignored build output.
- `scripts/link-mod.sh` symlinks `mod/` → `$RIMWORLD_DIR/Mods/QuickMenu`. `scripts/package.sh` zips `mod/` into `dist/` (gitignored).
- CI (GitHub Actions, ubuntu): .NET 10 setup → restore → format check → build all projects, including the benchmarks (compiled, not run) → **step 1**: functional tests with the coverage gate, excluding trait `Category=Perf` → **step 2**: Perf-trait tests (time and zero-allocation gates) in Release **without coverage instrumentation**, because instrumentation distorts both → zip artifact. Benchmarks run manually or from a manually triggered workflow. No game files are needed (ADR 0001).

## Consequences
- The Workshop upload and the GitHub zip contain only mod content.
- Developers must run `link-mod.sh` once per machine.
- Adding a future game version means adding a `1.8/` assembly folder and a LoadFolders entry.
