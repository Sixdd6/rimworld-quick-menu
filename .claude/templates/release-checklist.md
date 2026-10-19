# Release Checklist: v<version>

## Pre-release
- [ ] All milestone tasks in scope are `[x]` in plan.md
- [ ] Full test suite green locally and in CI on the release commit
- [ ] Every Must requirement has a passing test (traceability table complete)
- [ ] `code-reviewer`: no blocking findings open
- [ ] `security-auditor`: no high/critical findings open; dependency audit clean
- [ ] README, usage docs and CHANGELOG updated
- [ ] Version bumped consistently in all manifests
- [ ] License file present and correct
- [ ] Every asset is in assets.md with a compatible license. Attribution/credits are shown where required. No placeholder assets remain (or the user accepted them)
- [ ] Every performance budget passes (if performance.md exists)
- [ ] sre readiness review passed: rollback tested, alerts and runbooks in place, backup restore tested (if operations.md exists)
- [ ] Data migrations tested against a copy of the previous version. AI eval scores meet thresholds (if data-ml.md exists)
- [ ] Store build, signing, privacy/data-safety forms and screenshots ready (mobile)
- [ ] Visual review and audio measurement reports have no open blocker findings. The user has checked the items flagged for subjective review
- [ ] Build artifact produced and smoke-tested (install/run from the artifact, not the source tree)

## Release (requires explicit user confirmation)
- [ ] User confirmed target(s), version, and visibility
- [ ] Credentials available as environment variables / CI secrets
- [ ] Tag `v<version>` created and pushed
- [ ] Artifact published to: <registry / host>
- [ ] GitHub release created with notes

## Post-release
- [ ] Install/visit the published artifact and verify it works
- [ ] STATUS.md log updated
- [ ] Follow-up issues recorded
