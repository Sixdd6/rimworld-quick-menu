#!/usr/bin/env bash
# Idempotent, non-interactive environment setup. Safe to run twice.
# Used by the dev container (postCreateCommand), Claude Code cloud sessions (SessionStart hook) and humans.
#
# Installs the .NET 10 SDK if missing, then restores NuGet packages (no game files needed).
# SDK install order: (1) already on PATH / ~/.dotnet, (2) Microsoft's official dotnet-install.sh,
# downloaded to a temp file and then run (not piped), installing into ~/.dotnet without sudo.
# Distro-package alternative (needs sudo): Fedora `sudo dnf install dotnet-sdk-10.0`, Ubuntu `sudo apt-get install dotnet-sdk-10.0`.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=scripts/_env.sh
. "$here/_env.sh"
cd "$REPO_ROOT"

need_major=10
have_sdk() {
  command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -Eq "^${need_major}\."
}

if have_sdk; then
  echo "setup: .NET SDK present: $(dotnet --version)"
else
  echo "setup: installing .NET ${need_major} SDK into \$HOME/.dotnet using Microsoft's official dotnet-install.sh"
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' EXIT
  curl -fsSL --retry 3 -o "$tmp/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
  bash "$tmp/dotnet-install.sh" --channel "${need_major}.0" --install-dir "$HOME/.dotnet" --no-path
  # shellcheck source=scripts/_env.sh
  . "$here/_env.sh"
fi

# Make the SDK visible to later steps in Claude Code cloud sessions.
if [ -n "${CLAUDE_ENV_FILE:-}" ] && [ -x "$HOME/.dotnet/dotnet" ]; then
  grep -qs 'DOTNET_ROOT' "$CLAUDE_ENV_FILE" || {
    echo "export DOTNET_ROOT=\"$HOME/.dotnet\"" >> "$CLAUDE_ENV_FILE"
    echo "export PATH=\"$HOME/.dotnet:\$PATH\"" >> "$CLAUDE_ENV_FILE"
  }
fi

echo "setup: dotnet restore"
if [ -n "${CI:-}" ]; then
  dotnet restore QuickMenu.slnx --locked-mode
else
  dotnet restore QuickMenu.slnx
fi
echo "setup: done"
