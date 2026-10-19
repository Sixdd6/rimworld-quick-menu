---
name: project-status
description: Summarize current project state - phase, gates, milestone progress, test health, open questions, blockers, and the recommended next command.
---

# Project status

1. If `docs/project/STATUS.md` does not exist, say that no project has started and suggest `/kickoff "<idea>"`.
2. Read STATUS.md and plan.md, then run `git log --oneline -10` and `git status --short`.
3. Report briefly:
   - Project name, autonomy mode, current phase
   - A phase table (state and approval)
   - Current milestone: tasks done/total, in progress, blocked
   - The last test result recorded (do not run the suite unless asked)
   - Open questions and blockers
   - **Recommended next step**: one concrete command (for example `/build-next milestone`)
