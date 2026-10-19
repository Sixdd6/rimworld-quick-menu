# Sourced by the other scripts. Puts a user-local .NET SDK (~/.dotnet) on PATH when present.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
if [ -x "$HOME/.dotnet/dotnet" ]; then
  case ":$PATH:" in *":$HOME/.dotnet:"*) ;; *) export PATH="$HOME/.dotnet:$PATH" ;; esac
  export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
fi
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export REPO_ROOT
