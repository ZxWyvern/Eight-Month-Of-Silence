# Task: <short outcome>

## Request and scope

User request: <preserve the actual requested outcome>
Route(s): <workflow_docs / bugfix / gameplay_feature / narrative_content / refactor / performance>
Acceptance criteria: <observable behavior or deliverable; include relevant failure/recovery behavior>
Authorized scope and existing approvals: <record them; do not ask for the same approval again>
Out of scope: <specific adjacent changes>

## Story and mechanics traceability (when applicable)

Workflow ticket: <ID from agent/story-mechanics-workflow.md, or not applicable with reason>
Production scope: <MVP / vertical slice / full-story expansion>
Canonical chapter/location/beat: <source; distinguish slice sample from release story order>
Dependencies and evidence: <ticket/gate, current check result, evidence path; code existence is not PASS>
Player action → information learned → consequence: <observable sequence>
Existing mechanic vs missing integration: <verified owners; new work explicitly proposed>
Controls and recovery: <success, rejection, Cancel, repeat, death/reload and scene unload>
Human-owned decisions: <applicable TBD/AGENTS §8 trigger, existing authorization or blocker>

## Grounding

Project/source root: <actual paths>
GDD and ADR references: <exact files/sections, accepted vs proposed>
Code path: <input/trigger → owner/service → state/event → presentation → persistence/recovery>
Related tests: <actual fixture names and what they cover>
Existing implementation to reuse: <paths/symbols>
Unresolved design/source conflicts: <none or evidence from both sides>
Assumptions/TBDs: <explicit scope; do not finalize canon or tuning silently>

## Skills and tool availability

| Capability | Relevant? | Present/configured? | Callable now? | Invoked tool/skill and result | Affected operation if unavailable |
|---|---|---|---|---|---|
| Installed task skill(s) | | | | | |
| context7 | | | | | |
| Unity MCP / selected project and instance | | | | | |

Reading a skill file is not invocation. Configured server source is not a live connection. Tool availability does not alter authorization.

## Plan

| Changed file/type | Layer or workflow area | Change and reason | Acceptance criterion | Validation |
|---|---|---|---|---|
| | | | | |

Serialization/DI/input/assets affected: <specific contracts or not applicable>
Risks: <performance, concurrency, save compatibility, lifecycle, coupling>

## Phase ledger

Each phase receives its own row before proceeding. Keep a blocked task at the affected phase; a partial delivery does not pass it.

| Phase | State: PASS / FAIL / BLOCKED / NOT_RUN | Exit evidence / reason |
|---|---|---|
| INTAKE | NOT_RUN | |
| CLARIFY | NOT_RUN | |
| PLAN | NOT_RUN | |
| ARCHITECTURE_CHECK | NOT_RUN | |
| IMPLEMENT | NOT_RUN | |
| SELF_REVIEW | NOT_RUN | |
| TEST | NOT_RUN | |
| DOCUMENT | NOT_RUN | |
| HANDOFF | NOT_RUN | |

## Verification evidence

| Check | Expected | Observed | PASS / FAIL / BLOCKED / NOT_RUN / NOT_APPLICABLE | Command/tool, timestamp and evidence path |
|---|---|---|---|---|
| | | | | |

For MCP Play+Execute include project, instance, scene, duration, actions, compilation result, fresh console results, screenshot/state dump and confirmation Play Mode stopped. If not applicable, explain why the task changes no Unity runtime code/assets.

## Handoff — AGENTS.md §9 order

1. **What changed and why:** <outcome, files, acceptance criteria>
2. **Architecture impact:** <layers and dependencies; or workflow-only>
3. **Skills & MCP used:** <actual invocations and purpose; unavailable/not-applicable explicitly>
4. **Risks:** <remaining risks and limits of evidence>
5. **Definition of Done:** <applicable checked items, named unchecked items, evidence>
6. **Follow-ups:** <bounded next action, owner decision if required, relevant ADR status>

Task state: <HANDOFF or BLOCKED at phase>. Human acceptance: <pending; never fabricate DONE>.
