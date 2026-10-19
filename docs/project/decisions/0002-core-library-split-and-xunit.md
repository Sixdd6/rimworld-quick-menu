# 0002. Split pure logic into QuickMenu.Core and test it with xUnit v3

- **Status**: accepted (2026-10-19)
- **Date**: 2026-10-19

## Context
NFR-8 requires layout maths (ring/grid placement, edge dodging, sizing) and pin identity/serialisation to be unit-tested outside the game. Game types (`Verse.*`, `UnityEngine.*` from UnityStubs) cannot be loaded in a plain test host without the game's custom runtime and data. Much of the mod's risk is in maths and state handling: edge cases near screen edges, Fixed vs Auto pins, toggle/hold transitions.

## Options considered
1. **One assembly, test through the game only** (self-test debug action and manual checks): the simplest build, but it fails NFR-8 and gives slow feedback.
2. **Separate `QuickMenu.Core` assembly with no game references, plus a thin adapter assembly**: Core is fully testable with any test framework and keeps the game-facing code small. It costs a second DLL and a few small value types (`Vec2`, `Rect2`) converted at the boundary.
3. **One assembly, with Core code compiled into both the mod and the test project through linked source files**: one shipped DLL, but the build is less obvious and accidental game references can slip into "pure" files unnoticed.

Test framework:
1. **xUnit.net v3** (`xunit.v3` 4.0.x on Microsoft Testing Platform): current, widely used, runs as a normal executable, and has coverage through `Microsoft.Testing.Extensions.CodeCoverage`.
2. **NUnit 4**: also mature. Its richer constraint model is not needed here.
3. **MSTest**: fine, but less common in the RimWorld modding community.

## Decision
- Two projects: `src/QuickMenu.Core` (net10.0, `Nullable=enable`, `TreatWarningsAsErrors=true`, **no** game or Unity references, enforced by the project having none) and `src/QuickMenu` (adapter, references Core and the game). Both DLLs ship in `mod/1.7/Assemblies/`. The assembly names are prefixed to avoid collisions with other mods.
- Tests: `tests/QuickMenu.Core.Tests` with `xunit.v3` 4.0.x (pin the exact version in the lockfile; 4.0.2 is the latest stable at the time of writing). Line coverage of Core must be at least 90 %, as a CI gate.
- Core exposes plain data (`SlotPlacement[]`, `PinResult`, `MenuCommand`, `SearchEntry`). It never calls back into the game. `SearchEntry` holds only strings, ints and bools; the adapter maps `SearchEntry.Id` to its own designator table.
- Performance gates are xUnit tests with trait `Category=Perf`. They run as a separate CI step from the coverage run. Micro-benchmarks live in `tests/QuickMenu.Core.Benchmarks` (BenchmarkDotNet), see ADR 0008.

## Consequences
- Fast, deterministic tests for most behaviour, written test-first.
- The adapter must stay thin. Logic found in the adapter during review is moved to Core.
- The `Vec2`/`Rect2` conversion at the boundary is a small amount of glue code.
- Adapter code (Harmony patches, IMGUI) is verified by the in-game self-test and the manual checklist, not by unit tests.
