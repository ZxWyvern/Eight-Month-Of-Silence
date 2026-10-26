# Definition of Done

A task is complete only when every applicable item below is true **and the agent has stated so explicitly** in its final report (per `AGENTS.md` §9). Unchecked or skipped items must be called out, not omitted silently.

## Universal (every task)

- [ ] Code compiles with zero new warnings introduced.
- [ ] No forbidden pattern from `docs/CODING_STANDARDS.md` §4 introduced.
- [ ] Dependency direction from `docs/ARCHITECTURE.md` §1 respected — no new Domain→Unity or Presentation-bypassing-Application reference.
- [ ] Naming follows `docs/CODING_STANDARDS.md` §1.
- [ ] No hardcoded tuning values that belong in a `ScriptableObject` config.
- [ ] No commit of generated/build artifacts (`Library/`, `Temp/`, `.csproj`, `obj/`).
- [ ] Risk statement written (performance, concurrency, save-compatibility, coupling) even if "none identified."
- [ ] `docs/DECISIONS.md` updated if an architectural choice was made or deferred.

## New Gameplay System / Feature

- [ ] Domain logic has EditMode unit tests covering the core rule(s), including at least one edge case and one invalid-input case.
- [ ] Application service tested with faked Infrastructure dependencies (no real disk/Addressables access in EditMode tests).
- [ ] Feature mapped to the correct row in `docs/ARCHITECTURE.md` §4 — if it doesn't fit an existing row, the table has been updated (flagged for human review).
- [ ] Any new cross-system notification goes through the typed event bus (`docs/ARCHITECTURE.md` §6), not `UnityEvent`/static events, unless it's pure Presentation wiring.
- [ ] Manual note on frame-time/GC impact if the feature runs in a per-frame path.

## Save / Checkpoint Changes

- [ ] Backward compatibility with existing save data addressed explicitly (migration path or version bump), or explicitly flagged as a breaking change requiring human sign-off.
- [ ] Write path confirmed non-blocking / doesn't stall gameplay.
- [ ] Corruption scenario considered (crash mid-write) and mitigated or documented as a known risk.
- [ ] Checkpoint milestones match GDD §52 (`docs/GDD.md` §8) unless the task explicitly changes them.

## Final-Level / Adrian Encounter Changes

- [ ] Escalated per `AGENTS.md` §8 before starting, unless the task ticket explicitly pre-approved the scope.
- [ ] State machine transitions are explicit and exhaustively handled (no default/fallthrough silently doing nothing).
- [ ] No possibility of a softlock identified (player cannot reach a state with no valid progression or fail path) — state how this was verified.

## UI / Presentation-only Changes

- [ ] No gameplay rule logic added to a MonoBehaviour — confirmed it calls into Application, not implements new rules.
- [ ] Input goes through the New Input System, not legacy `Input.*`.

## Bug Fixes

- [ ] Root cause documented per `docs/CODING_STANDARDS.md` §6, not just the symptom patched.
- [ ] Regression test added that would have caught this bug, where testable (EditMode preferred).

## Refactors

- [ ] Behavior preserved — stated explicitly, and how it was verified (existing tests still pass, or manual verification steps listed if untestable).
- [ ] Migration strategy stated for anything not doable in a single atomic change.

## Before Handoff to a Human Reviewer

- [ ] Summary written per `AGENTS.md` §9 (what/why, architecture impact, risks, DoD checklist, follow-ups).
- [ ] Any deviation from `docs/ARCHITECTURE.md` or `docs/CODING_STANDARDS.md` is explicitly called out with justification — not left for the reviewer to discover.
