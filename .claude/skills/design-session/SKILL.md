---
name: design-session
description: Run a structured, multi-round design interview with the user to turn an idea into an approved, testable product specification (docs/project/spec.md). Uses the requirements-analyst agent to find gaps between rounds.
argument-hint: "[focus area or 'revise']"
---

# Design session

Goal: an approved `docs/project/spec.md` in which every Must requirement has testable acceptance criteria and no open question would change the architecture. You conduct the interview. Only the main conversation can talk to the user.

Focus / mode: `$ARGUMENTS`

## Setup
1. Read `docs/project/idea.md`, `docs/project/STATUS.md`, and `docs/project/spec.md` if it exists. If the argument is `revise`, or a spec already exists, work only on the sections that need to change.
2. If there is no spec yet, copy `.claude/templates/spec.md` to `docs/project/spec.md`. Pre-fill what the idea already answers, and mark inferred content with `(assumed)`.
3. Set Phase 1 to `in progress` in STATUS.md.

## Interview rounds
Use **AskUserQuestion** for every round: at most 4 questions, each on one topic, with 2–4 concrete options, the recommended one first and labelled "(Recommended)". The user can always pick "Other". Before each round, say in one sentence what it covers. After each round, write the answers into `spec.md` **right away**. Do not keep them only in the conversation.

Go through these topics in order. Skip any question the idea or earlier answers already settle.

If the user is unsure of the scope, ask probing questions. Draw out ideas the user has not yet put into words. Offer concrete options to react to, such as a small, a medium and a large version of the project.

1. **Vision and users**: the core problem, the primary user/persona, what success looks like, the single most important use case.
2. **Scope**: the MVP feature list (propose one, and let the user add or cut), explicit non-goals, deadline or time box.
3. **Experience**: the interface type (web app, mobile, desktop, CLI, API, library, bot, and so on), key screens/commands/endpoints, look and feel and existing references, accessibility level.
4. **Look, sound and assets**: visual style and references (ask for links or images), theme(s), which graphics are needed (icons, illustrations, sprites, animation, 3D, video), whether there is any audio (sound effects, music, voice/TTS), and where the assets come from: the user supplies them, free-licensed libraries, AI generation (which service, and who pays), made in code, or placeholders for now. Also: is commercial use planned (this decides which licenses are acceptable)? Skip the details if the answer is "standard UI, no media". Record the answers in spec §8.
5. **Data**: what is stored, where, for how long, sensitivity (personal data? payments?), import/export, multi-user or single-user, sharing.
6. **Integrations**: external APIs/services, auth provider, payments, email, AI/LLM usage, and for each: does the user already have an account or keys?
7. **Quality attributes**: expected scale, performance expectations, availability, offline support, security/compliance needs, supported platforms/browsers, i18n.
8. **Technical preferences and constraints**: preferred languages/frameworks/engines (or "you choose"), hosting preference and budget, existing systems, license.
9. **Release and distribution**: where it ships (npm, PyPI, crates.io, Docker registry, web host such as Vercel/Fly/Cloudflare/GitHub Pages, app store, itch.io/Steam, GitHub Releases binaries), who operates it, public or private, versioning.
10. **Acceptance**: how the user will judge that v1 is done, and the demo scenario they want to see.

### Deep-dives (only when they apply)
After the core topics, add a round for each area the product includes:
- **Game**: genre and references, a brief description of the gameplay (what the player does in a typical session), core loop in one sentence, controls and input devices, session length, progression, number of players, the multiplayer type if any (local or online, co-op or competitive, real-time or turn-based), difficulty and accessibility options, monetization (or none). Record in spec §4, §5 and §7. The game-designer expands it later.
- **Mobile**: iOS, Android or both; native or cross-platform preference; minimum OS versions; phone/tablet; offline use; push notifications; device features (camera, location, biometrics); app-store distribution and accounts (Apple Developer, Google Play).
- **Data / AI**: data volumes and growth, search or analytics needs, any ML or LLM feature (what it decides or generates, acceptable error rate, human review), which AI provider and who pays, acceptable cost per month, and privacy constraints on training or sending data to providers.
- **Hosted service**: expected users and traffic, availability expectation (for example "hobby, downtime OK" vs "business-critical"), regions, monthly hosting budget, who is on call (usually nobody: design for that), custom domain.
- **Performance**: concrete targets the user cares about (page load, response time, frame rate, startup, size), and on what devices or networks.

For open-ended topics (vision, feature list, visual/audio references), a short free-text question in chat is fine instead of AskUserQuestion.

## Gap analysis loop
After topics 1–6, and again after topics 7–10 and the deep-dives:
1. Launch the `requirements-analyst` agent with the paths to `idea.md` and `spec.md`, and a summary of any interview notes not yet in the spec.
2. Ask the user its highest-impact "Next questions" (at most 2 rounds of 4). Offer its "Assumptions" list for bulk confirmation in one question: accept all / review individually / I will specify.
3. Update the spec. Repeat until the analyst returns `READY`, or the user says to stop (then record the remaining gaps as assumptions in §15).

## Finish
1. Write the final spec: every FR has an ID, a MoSCoW priority, and Given/When/Then criteria. NFRs have a verification method. Resolved questions are removed from §15. No template placeholders remain.
2. Present a **compact summary**: a one-paragraph pitch, the Must/Should/Could counts with titles, the key NFRs, the release target, and the assumptions made. Link to `docs/project/spec.md` for the full text.
3. **Gate**: ask for approval (approve / request changes). On approval, set the spec Status to `approved (date)`, update STATUS.md (Phase 1 done, approved ☑, log entry), and commit `docs: approve product spec`.
