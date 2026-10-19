---
name: publish
description: Prepare and publish a release (version, changelog, artifacts, release CI, tag, registry/host publish, GitHub release) via the release-engineer agent with a mandatory explicit user confirmation before anything leaves the machine.
argument-hint: "[version | patch | minor | major]"
disable-model-invocation: true
---

# Publish

Version request: `$ARGUMENTS` (empty = derived from commits)

## 1. Readiness
Confirm from STATUS.md and plan.md that the Verify phase passed: tests are green, there are no open blocker/major review findings and no critical/high security findings, every performance budget passes (if `performance.md` exists), and the sre readiness review passed (if `operations.md` exists). If it did not pass, list what is missing and offer to run `/test-suite full` and `/review-changes release --fix` first.

## 2. Prepare (Mode A)
Launch `release-engineer` in **Mode A (prepare)** with the version request, the paths to spec §12, architecture §10, environment.md, and the release checklist template.

## 3. Confirm (the mandatory gate, in every autonomy mode)
Show the user exactly:
- what will be published (artifact names and version), and to where (registry/host/URL), with public/private visibility
- the tag and branch it comes from, and the CI status on that commit
- the credentials needed, and whether they are available (names only)
- any new ongoing costs (hosting, services) from `operations.md`
- anything irreversible (for example, most registries do not allow re-publishing the same version)

Ask with AskUserQuestion: **Publish now** / **Change something** / **Not yet**. Anything other than an explicit "Publish now" means stop.

## 4. Publish (Mode B)
1. Create the marker that lets the guard hook allow publish commands: `touch .claude/.publish-approved`.
2. Launch `release-engineer` in **Mode B (publish)**, with the user's confirmation quoted verbatim (target, version, visibility, and whether pushing the tag is approved). If the project has an `sre` and the release is deployed to hosted infrastructure, launch `sre` in **deploy** mode after the artifacts are published, with the same confirmation quoted. For app-store releases, the `mobile-developer` prepares the store build and metadata in Mode A, and submission counts as publishing.
3. **Always** remove the marker afterwards, even on failure: `rm -f .claude/.publish-approved`.

## 5. Report
The published URLs, the verification result, and the checklist state. Update STATUS.md (Phase 7 done, log entry with version and date). If anything failed partway, show the exact state and the rollback steps from the agent's report.
