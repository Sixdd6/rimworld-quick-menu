---
name: env-setup
description: Set up and verify the project's development environment (toolchain, scaffold, quality tooling, MCP servers, dev container, cloud setup script, CI) through the environment-engineer agent, asking the user before adding servers or services that need credentials or money.
argument-hint: "[what to add or change, e.g. 'add postgres mcp']"
---

# Environment setup

Request: `$ARGUMENTS` (empty = full setup for the approved architecture)

1. Check the prerequisites: `docs/project/architecture.md` is approved (for a full setup). If it is not, say so and stop, unless the request is a narrow change.
2. Set Phase 3 to `in progress` in STATUS.md.
3. Launch the `environment-engineer` agent with: the paths to architecture.md, decisions/, spec.md, `.claude/templates/environment.md`, `.claude/templates/mcp-catalog.md`, the specific request (if any), and the current platform (local vs Claude Code cloud session; check the `CLAUDE_CODE_REMOTE` env var).
4. Review its report:
   - **Proposed MCP servers / services that need credentials or payment**: ask the user with AskUserQuestion (add now / add later / skip), and state which secrets each one needs and where to get them. Then have the agent add the approved ones.
   - **Secrets**: list the variable names the user must provide. Tell them to put local values in `.env` (gitignored), and to set them as CI/repository secrets, or as environment variables in the Claude Code cloud environment settings. Never ask the user to paste secret values into the chat.
   - **Blockers**: report them with the exact error and the proposed remedy.
5. **Verify it yourself**: run the install → build → lint → test commands from `environment.md` and confirm they pass. If they do not, send the agent back with the output.
6. Remind the user that new MCP servers load when a Claude Code session starts, and that project servers from `.mcp.json` ask for one-time approval. Suggest a session restart if servers were added.
7. Update STATUS.md (Phase 3 done, log entry) and commit `chore: set up development environment`.
