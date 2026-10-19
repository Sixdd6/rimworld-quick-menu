# Environment: rimworld-quick-menu

## Toolchain
| Tool | Version | Install | Why |
|---|---|---|---|
| .NET SDK | 10.0.402 verified. `global.json`: `10.0.100`, `rollForward: latestFeature` (any 10.0.x) | `scripts/setup.sh` (Microsoft's official `dotnet-install.sh`, downloaded then run, into `~/.dotnet`, no sudo). Or distro package: Fedora `sudo dnf install dotnet-sdk-10.0`; Ubuntu `sudo apt-get install dotnet-sdk-10.0` | ADR 0001: RimWorld 1.7 runs on net10 |
| C# | 14 (`LangVersion=latest`), nullable on | SDK | |
| Krafs.Rimworld.Ref | 1.7.4982-unstable | NuGet (central versions in `Directory.Packages.props`) | Compile against game APIs with no game files (CI) |
| Lib.Harmony | 3.0.0-rc.3, compile-only (`ExcludeAssets=runtime`) | NuGet | Runtime copy comes from `brrainz.harmony` mod |
| xunit.v3 | 4.0.2 (Microsoft Testing Platform) | NuGet | Core tests |
| Microsoft.Testing.Extensions.CodeCoverage | 18.12.0 | NuGet | Coverage (cobertura) |
| BenchmarkDotNet | 0.15.8 | NuGet | Benchmarks (compile-only in CI) |
| zip, unzip, curl, bash | any | OS package | `scripts/package.sh`, `scripts/setup.sh` |

All package versions are pinned centrally (`Directory.Packages.props`) and locked per project (`packages.lock.json`, restored with `--locked-mode` in CI).

## Commands
| Purpose | Command |
|---|---|
| Install deps | `scripts/setup.sh` (SDK if missing + `dotnet restore QuickMenu.slnx`) |
| Build | `dotnet build QuickMenu.slnx -c Release` (DLLs to `mod/1.7/Assemblies/`) |
| Run (dev) | `scripts/link-mod.sh` once, then launch RimWorld with `-mod=brrainz.harmony,sixdd.QuickMenu -quicktest` |
| Unit tests (functional + coverage gate) | `scripts/test.sh` (`--filter-not-trait "Category=Perf"`, Core line coverage >= 90 %, override with `MIN_COVERAGE`) |
| Perf gates | `scripts/perf.sh` (`--filter-trait "Category=Perf"`, Release, no coverage) |
| All tests | `scripts/test.sh && scripts/perf.sh` |
| Benchmarks | `scripts/perf.sh --bench` (writes gitignored `BenchmarkDotNet.Artifacts/`) |
| Lint | `dotnet build` (analyzers `latest-recommended`, warnings are errors, style enforced in build) |
| Format | `dotnet format QuickMenu.slnx` (check: `--verify-no-changes`) |
| Type check | the C# compiler (nullable enabled) via `dotnet build` |
| Package | `scripts/package.sh` -> `dist/QuickMenu-<version>.zip` |
| Local game DLLs instead of Krafs | `dotnet build -p:UseLocalGameRefs=true` (uses `RIMWORLD_DIR`; rewrites lock files, revert with `git checkout -- '**/packages.lock.json'`) |

Notes: `scripts/link-mod.sh` is the ADR 0008 / architecture `link-mod.sh`. The coverage gate is vacuous while Core has no executable lines (script says so); it becomes real with the first Core code. `dotnet test --solution QuickMenu.slnx` also works (MTP runner set in `global.json`), but the scripts use `dotnet run` with absolute coverage paths.

## Setup
- `scripts/setup.sh`: idempotent, non-interactive. Installs the SDK if no 10.x SDK is found, writes PATH to `CLAUDE_ENV_FILE` in cloud sessions, restores packages (locked when `CI` is set). The SessionStart hook runs it in Claude Code cloud sessions.
- `scripts/_env.sh`: sourced helper that puts `~/.dotnet` on PATH.
- Dev container: `.devcontainer/devcontainer.json` (`mcr.microsoft.com/devcontainers/dotnet:1-10.0`, `postCreateCommand: scripts/setup.sh`). Not built or run here (no container tooling checked); the image tag follows the published devcontainer naming and should be confirmed on first use.
- RimWorld itself is not needed to build, test or package. It is needed only to run the mod (`scripts/link-mod.sh`). iOS/mobile/DB/media tooling: not applicable (no specialists, no assets pipeline beyond runtime-generated masks).

## MCP servers (`.mcp.json`)
| Server | Purpose | Transport | Secrets needed |
|---|---|---|---|
| context7 | Current docs for .NET, Harmony, xUnit, BenchmarkDotNet | stdio, `npx -y @upstash/context7-mcp` | none (optional API key for higher limits) |

Decision (2026-10-19, user): keep context7, playwright removed (a game mod has no browser UI). context7 needs Node/npx: run `sudo dnf install nodejs`, then restart the session. No credentialed servers proposed. Optional (needs a GitHub token, so approval needed): GitHub MCP for PR/release work, not required because `gh` CLI covers it.

## Secrets and configuration
No secrets. Names only:
| Variable | Used by | Required | How to obtain |
|---|---|---|---|
| `RIMWORLD_DIR` | `scripts/link-mod.sh`, `-p:UseLocalGameRefs=true` | No (default `~/.steam/steam/steamapps/common/RimWorld`) | Path of the RimWorld install |
| `MIN_COVERAGE` | `scripts/test.sh` | No (default 90) | |

`.env.example` documents these; `.env*` other than `.env.example` is gitignored.

## CI
`.github/workflows/ci.yml`, on push to `main` and every PR, `ubuntu-latest`, `actions/setup-dotnet` from `global.json`, NuGet cache keyed on `packages.lock.json`. Steps: restore `--locked-mode` -> `dotnet format --verify-no-changes` -> Release build (all four projects, benchmarks compiled only) -> `scripts/test.sh` (functional + coverage gate) -> `scripts/perf.sh` (Perf trait, Release, no coverage) -> `scripts/package.sh` -> upload zip artifact (and TestResults). No game files needed. Not run on GitHub yet (no push done); the same commands pass locally. Suggested required check: `build-test`. Note `actions/*@v4` tags are not pinned by SHA.

## Verification
2026-10-19, Fedora 44, .NET SDK 10.0.402 (installed to `~/.dotnet` with the official dotnet-install.sh because passwordless sudo is unavailable):
- `dotnet restore QuickMenu.slnx` and `--locked-mode`: all 4 projects restored (Krafs.Rimworld.Ref 1.7.4982-unstable and Lib.Harmony 3.0.0-rc.3 resolve from nuget.org).
- `dotnet build QuickMenu.slnx -c Release`: Build succeeded, 0 warnings, 0 errors. `mod/1.7/Assemblies/` holds `QuickMenu.dll` and `QuickMenu.Core.dll` (no `0Harmony.dll`).
- `dotnet format QuickMenu.slnx --verify-no-changes`: exit 0.
- `scripts/test.sh`: total 1, failed 0, succeeded 1; coverage report produced (gate vacuous: no executable Core lines yet).
- `scripts/perf.sh`: total 1, failed 0, succeeded 1 (Perf-trait placeholder test).
- `scripts/package.sh`: `dist/QuickMenu-0.1.0.zip` created with About, LoadFolders.xml, Languages, 1.7/Assemblies (no pdb).
- `scripts/setup.sh` run twice: idempotent. `dotnet build -p:UseLocalGameRefs=true` against the installed game DLLs: 0 warnings, 0 errors.
- Not verified: dev container build, GitHub Actions run, `scripts/link-mod.sh` (deliberately not run), in-game loading.
