# Narrative / horror beat: <stable beat ID>

Status: <PROPOSAL / authorized implementation / verified implementation>
Source: <numbered GDD file and section; related accepted decision>
Location/chapter: <existing canon; do not create a new location silently>

## Player experience

Player action: <what the player actively does>
Information before the beat: <what they know and what remains uncertain>
Observation gained: <specific clue, contradiction or implication>
Consequence: <objective, puzzle, route or understanding that changes>
Horror purpose: <anticipation, uncertainty, loss of safety, consequence; avoid arbitrary startle>
Pillar check: grounded movement / meaningful inspection / narrative through action.

## Execution contract

| Field | Specification |
|---|---|
| Preconditions | Required evidence, mission/objective, item, puzzle or scene state |
| Trigger | Existing event/interaction/volume and actual publisher; label any new event as proposed |
| Eligibility owner | Domain rule / Application service, not a hidden condition inside a visual effect |
| State ownership | Who owns idle/eligible/running/completed/cancelled and prevents duplicate starts |
| Sequence | Exact ordered cues and player actions; timers are provisional config, not final tuning |
| Player control | Movement/look/inspection/pause allowed; prior input and cursor state to restore |
| Camera/animation | Existing integration or explicitly proposed component; blend/exit/interrupt rules |
| Audio | Cue purpose, spatial origin, mixer destination, readable warning, cooldown and fade/stop behavior |
| Text/visual fallback | Subtitle/observation/readable environmental clue when audio is missed or disabled |
| Completion | Observable state transition and exactly-once progression effect |
| Re-entry | One-shot / repeatable / retry policy; idempotency key if required |
| Cancellation | Death, leave area, skip, scene unload, disable, lost reference; cleanup for each |
| Persistence | What survives Cancel/death/restart; identify any proposed save-schema change |
| Missing asset behavior | Preserve controls/progression; visible authoring error and tracked placeholder |

## Implementation map

Existing source anchors: <paths/symbols verified this task>
New infrastructure required: <none or scoped proposal; do not assume a narrative runner/Timeline/audio backend exists>
ScriptableObjects/assets and wiring: <stable IDs, data, scene references and DI registration>
Asset provenance: <supplied/authored/reference; placeholder clearly marked>
Unknown canon or balancing: <TBD number and bounded options>

## Acceptance

- [ ] The player can discover the intended information through action.
- [ ] Trigger fires only when eligible and follows the specified repeat policy.
- [ ] Repeated triggers and interruption cannot duplicate progression or strand controls.
- [ ] Audio/visual/text cues remain understandable under relevant accessibility settings.
- [ ] Retry/reload follows the stated persistence contract without losing essential evidence.
- [ ] Required logic tests, live scene execution and console/state/screenshot evidence are attached.

Do not implement the Adrian finale, a new canonical event, final timings or new save fields without the authorization required by `AGENTS.md` and `docs/12-references-and-open-tbd.md`.
