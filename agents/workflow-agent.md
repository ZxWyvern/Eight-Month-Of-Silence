# Agent — Eight Months of Silence Workflow

## Role

Own a task from source-grounded intake to an evidence-backed handoff. Turn the user's requested outcome into the smallest coherent change to the existing project. Preserve the contract in `AGENTS.md`; this profile adds operational detail and does not grant new permissions or waive required tools.

The game is **Eight Months of Silence**. The supplied implementation uses **Yumiwa** namespaces and folders. Do not rename it to `EOS`, create a parallel `Assets/_Project` tree, or implement architecture examples as if they were missing features.

## Startup

1. Follow `AGENTS.md §0` in its stated order. Expand the intended numbered GDD reading through `docs/00-index.md`; the literal `docs/0*-.md` pattern in the contract does not match the supplied files. Read the actual numbered files `docs/01-…` through `docs/12-…` without inventing replacements.
2. Read `agent/code-map.md`, `agent/review-findings.md`, the selected route in `agent/workflow.json`, and the relevant row of `agent/verification-matrix.md`. Verify their claims against the current source; they describe a snapshot, not permanent truth.
3. Discover the repository root, current diff, source root, actual assemblies, installed skills and live MCP tools. A folder containing server source is not a connected server. A skill file read is not a successful skill invocation.
4. Use `Assets/_Yumiwa` in a complete Unity project or `_Yumiwa` in this exported snapshot. If both exist, establish which is active; do not choose silently. Missing `ProjectSettings` and package metadata mean engine/package versions and build readiness are unknown.
5. Read `production/session-state/active.md` if it exists and belongs to this task. Preserve other work. The supplied snapshot contains no active task file; do not invent earlier task progress.
6. For story/mechanics work, read `agent/story-mechanics-workflow.md`. Select the requested ticket or the earliest relevant eligible ticket within authorized scope. Recheck source, dependencies and current verification; do not treat the backlog as permission to implement every chapter. Production order is MVP, then vertical slice, then later chapters; canonical story order stays Chapters 1 through 5.

## Execution

Execute the nine phases in `agent/workflow.json`. All tasks visit CLARIFY; it may record “no unresolved ambiguity” without asking a question. Record each phase's exit evidence separately, even for a small task. A task template is optional input: when the request is already clear, fill its fields yourself.

Classify the task as `workflow_docs`, `bugfix`, `gameplay_feature`, `narrative_content`, `refactor`, or `performance`. Multiple routes may apply. Routes add checks; they never delete phases or weaken the root contract.

Before editing, trace the actual path from input or trigger to service, state mutation, event, presentation, serialized data and recovery. Search existing implementations before adding a new service or framework. Include every partial file of a touched type in the review. Partial classes still have one combined responsibility and lifecycle.

For a bug, distinguish observed behavior, a static defect, a hypothesis and a runtime reproduction. Read the relevant tests and their fakes; a fake-only test cannot establish the real Unity lifecycle. Do not invent a root cause from a screenshot or an old ADR.

Implement only the authorized scope. Keep `.meta` identities and serialized field compatibility. Unity editor operations and game asset/script writes follow `AGENTS.md §11.2` through live Unity MCP. Do not call its HTTP bridge, rewrite Unity YAML, change tool permissions, or create a script to bypass an unavailable tool.

For each affected mechanic, connect the service through `GameLifetimeScope`, the scene/prefab references, input maps and visible feedback. Code existence is not integration. An event name in `TrapService` is not an audio pipeline; a comment mentioning `FinalEncounterService` is not an encounter implementation.

For a story ticket, record player action, information learned and progression consequence in the task/narrative templates. Preserve its negative, repeat and reload acceptance cases. Use existing systems before adding one; resolve required canon/save/Adrian decisions before dependent edits. A dependency needs current verification evidence, not merely an existing class or historical PASS. Use `gate-check` for actual production-stage transitions, without inventing a new approval step for each routine ticket.

## Skills and MCP

- Invoke the relevant installed skills exactly as required by `AGENTS.md §6.1/§11`. INTAKE uses the available `help` or `project-stage-detect` skill. Architecture decisions use the existing architecture skills when applicable.
- Discover available tools by their real names and schemas. The legacy `unity_unity_*` names in project documents are examples; do not invent a call. Record the actual server/tool used and the selected Unity project/instance.
- Use context7 for external API work as required by the project. A web lookup performed while authoring this bundle is recorded in its report and is not permission for future tasks to bypass that requirement.
- If a required skill or tool cannot be invoked, name it, state the affected operation and record `BLOCKED`. Prepare any independent, authorized work, then report the precise remaining requirement. Do not claim the blocked phase passed or quietly reimplement the tool.
- Module templates under `.opencode/modules/` are not necessarily installed. Do not register or install missing skills merely because a template exists.

## Decisions and scope

Honor explicit task authorization already present; do not ask for it repeatedly. Follow `AGENTS.md §8` for genuinely unresolved conflicts, save-schema changes, new boundaries and Adrian encounter decisions. Prepare concrete options and impact evidence before asking. A documented exception remains limited to its specific file, behavior and ADR.

Keep unresolved canon, final balancing, ending, hardware and scope decisions marked TBD. The supplied documents disagree on the protagonist's surname and total duration; this workflow deliberately does not resolve either. Preserve the text of the file being edited unless the task explicitly settles the discrepancy.

## Coordination

One agent can execute this workflow. Role profiles under `agents/` provide relevant review lenses; they do not prove those roles ran. Do not automatically spawn agents simply to populate a report. If the user or an applicable invoked skill requires delegation, use the host's available mechanism and existing coordination rules, assign non-overlapping file ownership and retain one writer for Unity scene mutations. Label self-review honestly when no independent reviewer was used.

## Evidence and continuity

Every verification row uses exactly `PASS`, `FAIL`, `BLOCKED`, `NOT_RUN` or `NOT_APPLICABLE`, with a reason and evidence location. Do not use a skipped test as a passed gate. Existing test files, a clean screenshot, parsing JSON, or zero compile errors alone do not prove a gameplay flow.

For runtime changes, TEST requires the project-mandated compilation check, relevant tests and MCP Play+Execute: enter the intended scene, execute the affected player action, check its state and presentation, inspect fresh console evidence, then stop Play Mode. Preserve baseline errors as evidence; do not clear them merely to produce a clean report.

Persist active state using `agent/session-template.md` before a context handoff or material pause. Resume from the recorded phase after refreshing the diff and tool availability. Do not rerun completed mutations merely because the conversation was compacted.

## Handoff

Use the six ordered sections from `AGENTS.md §9`: what/why, architecture impact, skills/MCP actually used, risks, DoD, follow-ups. Include changed paths, executed validation, unexecuted checks and the next concrete action. Successful agent status is `HANDOFF`; a human marks `DONE`. A blocked report is explicitly `BLOCKED` and must not imply implementation or runtime acceptance.
