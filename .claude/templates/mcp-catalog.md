# MCP Server Catalog

The `environment-engineer` uses this list as a **starting point** when it chooses MCP servers for a project. Package names and URLs change. **Check each entry against the server's official docs or the MCP registry before you add it.** Add only servers the project needs. Each server adds tools to the context and increases the attack surface.

Project servers go in `.mcp.json` at the repo root (shared with the team through git). Put secrets in environment variables with `${VAR}` expansion. Never write them in the file literally.

```json
{
  "mcpServers": {
    "stdio-example": {
      "command": "npx",
      "args": ["-y", "some-package@latest"],
      "env": { "API_KEY": "${SOME_API_KEY}" }
    },
    "http-example": {
      "type": "http",
      "url": "https://example.com/mcp",
      "headers": { "Authorization": "Bearer ${SOME_TOKEN}" }
    }
  }
}
```

Or from the CLI: `claude mcp add --scope project <name> -- <command> [args...]` and `claude mcp add --scope project --transport http <name> <url>`.

## Included in the template by default

| Server | Why | Config |
|---|---|---|
| context7 | Current, version-specific library docs for every phase | `npx -y @upstash/context7-mcp` |
| playwright | Drive a real browser for UI e2e testing and verification | `npx -y @playwright/mcp@latest` |

Remove either one if the project does not need it (for example, remove playwright for a pure library or CLI).

## Common additions by need

| Need | Server | Transport / config (check before use) | Secrets |
|---|---|---|---|
| GitHub issues, PRs, releases | GitHub MCP | http `https://api.githubcopilot.com/mcp/` | `GITHUB_TOKEN` (header) |
| Fetch web pages | fetch | `uvx mcp-server-fetch` | — |
| Scoped file access outside the repo | filesystem | `npx -y @modelcontextprotocol/server-filesystem <dir>` | — |
| Error monitoring | Sentry | http `https://mcp.sentry.dev/mcp` | OAuth |
| Payments | Stripe | http `https://mcp.stripe.com` | OAuth / API key |
| Postgres backend-as-a-service | Supabase | http `https://mcp.supabase.com/mcp` | OAuth / token |
| Issue tracking | Linear | http `https://mcp.linear.app/mcp` | OAuth |
| Docs / wiki | Notion | http `https://mcp.notion.com/mcp` | OAuth |
| Databases (Postgres, MySQL, SQLite) | Search the registry for a maintained server for your engine. Prefer the vendor's official server. Use a **read-only** DB user. | — | connection string |
| Cloud (AWS, GCP, Azure, Cloudflare, Vercel) | Use the provider's official MCP server | — | provider credentials |

## Media generation (graphics and audio)

Claude can write code, SVG, and synthesized audio, but it cannot produce raster images, music, or recorded voice on its own. For those, the designers need a generation service through MCP, free-licensed libraries, or files from the user. Most generation services **cost money and need an API key**, so always ask first.

| Need | Option (check before use) | Secrets | Notes |
|---|---|---|---|
| Text-to-speech, sound effects, voice | ElevenLabs official MCP server: `uvx elevenlabs-mcp` | `ELEVENLABS_API_KEY` | Paid tiers. Check the commercial-use terms for your plan |
| Image, audio and video models (many providers) | Replicate MCP server (see replicate.com docs for current package) | `REPLICATE_API_TOKEN` | Pay per use. The license depends on the model you run |
| Other image/music generators | Search the MCP registry for "image generation", "text to speech", "music generation". Prefer the vendor's official server | provider key | Check the provider's terms for commercial use and ownership of outputs |

**Free-licensed asset libraries** (no MCP needed, downloaded by the designers; record every file in `assets.md`):
- Kenney (kenney.nl): game art and audio, CC0
- Poly Haven (polyhaven.com): textures, HDRIs, 3D models, CC0
- OpenGameArt, Freesound: **mixed licenses** (CC0, CC BY, CC BY-SA, CC BY-NC). Check each file
- Google Fonts: mostly SIL OFL
- Icon sets such as Lucide (ISC), Heroicons (MIT), game-icons.net (CC BY 3.0, attribution required)

**Local tools** (installed by the environment-engineer, no MCP needed): ImageMagick, oxipng/pngquant, svgo, resvg/Inkscape, ffmpeg, sox. The Playwright MCP server (default) provides screenshots for visual review.

## Selection rules

1. **Need first**: tie each server to a requirement or workflow step (for example, "e2e tests for FR-3 need a browser").
2. **Official over community**: prefer servers published by the vendor or the MCP project.
3. **Least privilege**: use read-only tokens where possible, and scope to the project's resources.
4. **No secrets in git**: use `${VAR}` expansion, list the variable in `environment.md` and `.env.example`.
5. **Ask before adding** any server that needs an account, a paid plan, or credentials.
6. **Record it**: add a row to the MCP table in `environment.md`.
