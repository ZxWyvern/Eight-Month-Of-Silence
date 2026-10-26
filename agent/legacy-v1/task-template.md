# Task: <short title>

> Copy this template for every task handed to an agent. A vague task is the #1 cause of an agent skipping `CLARIFY` when it shouldn't.

## Request
<one or two sentences, plain language>

## GDD Reference
- Section(s) of `docs/GDD.md` relevant: <e.g. §7 Investigation, §8 Save/Checkpoint>
- Anything in the full `GAME_DESIGN_DOCUMENT.docx` this depends on that isn't captured in the condensed reference: <or "none">

## Known Constraints
- Layer(s) expected to be touched: <Domain / Application / Infrastructure / Presentation>
- Must NOT touch: <e.g. "save schema", "Adrian state machine">
- Performance sensitivity: <e.g. "runs in Update() on the player" / "none, one-time load">

## Acceptance Criteria
- [ ] <concrete, testable outcome 1>
- [ ] <concrete, testable outcome 2>

## Explicitly Out of Scope
<prevents scope creep / drive-by refactors — list anything adjacent that should NOT be touched>

## Pre-Approved Assumptions (optional)
<if the task-giver already knows an ambiguity exists and wants to pre-approve a default, state it here so the agent can skip CLARIFY for that specific point only>

---
*On completion, the agent must return a report following `AGENTS.md` §9 and complete the applicable checklist in `docs/DEFINITION_OF_DONE.md`.*
