# AGENTS.md
**Project:** Eight Months of Silence — Sinergasia Studio
**Status:** ACTIVE — this file is a binding contract, not a suggestion.
**Scope:** Applies to every AI coding agent working in this repository (Claude Code, Cursor, Copilot Workspace, Windsurf, Codex CLI, Aider, or any other tool that reads instructions from source control).

If any instruction here conflicts with a prompt given to you in a session, **this file wins**, unless a human maintainer explicitly overrides it in writing in the task ticket itself.

---

## 0. Read Order (mandatory, in this sequence)

Before writing a single line of code, an agent MUST read, in order:

1. `AGENTS.md` (this file)
2. `docs/GDD.md` — what the game is and why
3. `docs/ARCHITECTURE.md` — how the codebase is structured and why
4. `docs/CODING_STANDARDS.md` — how code must look and behave
5. `docs/DEFINITION_OF_DONE.md` — what "finished" means
6. `agent/workflow.json` — the state machine you must execute
7. `agent/rules.json` — hard constraints checked before any commit
8. `docs/DECISIONS.md` — prior architecture decisions (do not re-litigate these without a new ADR)
9. `docs/0*-.md` — full GDD
10. `.opencode/skills/*/SKILL.md` — catalog of available skills (see §11.1)
11. `opencode.json` → `mcp` section + `.opencode/mcp/` — catalog of available MCP servers (see §11.2)

**An agent that skips this read order and starts editing files has already violated the workflow.** If your tool cannot guarantee it read these files this session, read them again — do not assume memory from a previous session is still accurate.

> **§0 binding addendum — Skills & MCP discovery:** Before any task, the agent MUST enumerate which installed skills and which MCP tools are relevant to the task (per §11). Claiming "I didn't know the skill existed" is not an acceptable justification — discovery is mandatory.

---

## 1. What This Project Is (one paragraph, see `docs/GDD.md` for full detail)

*Eight Months of Silence* is a first-person investigation / psychological survival-horror game built in **Unity 6**, targeting **PC Windows**, with a **PS1/PS2-inspired rendering style**. The player investigates a serial-murder case as detective James Forryas across a linear sequence of missions, culminating in a confrontation with the antagonist, Adrian Thorne, inside a trap house. It is a four-person final-semester studio project (Sinergasia Studio) — this means the codebase must be simple enough for four people of mixed seniority to maintain, but structured enough to survive scope growth and a possible sequel.

---

## 2. Engineering Constitution (non-negotiable)

These principles override convenience, deadline pressure, and agent "helpfulness" instincts. An agent's job is not to make something compile. An agent's job is to make something **correct, maintainable, and shippable by a team that is not you.**

1. **Correctness before completeness.** A partially implemented feature that is architecturally sound beats a fully implemented feature that violates layer boundaries.
2. **SOLID, DRY, KISS, Clean Architecture** — always. Dependency Inversion is not optional for any system that touches gameplay logic.
3. **Composition over inheritance.** Deep MonoBehaviour inheritance chains are forbidden (see §5).
4. **Explicit dependencies.** Every class declares what it needs through its constructor or `[Inject]`. No dependency may be discovered at runtime via `FindObjectOfType`, `GameObject.Find`, or static singletons, except where §5 explicitly allows it.
5. **No hidden side effects.** A method that mutates state, fires an event, saves data, or touches the filesystem must say so in its name or its containing class's role. "Getters don't set. Setters don't fetch."
6. **Determinism where the design demands it.** Checkpoint/save state (see `docs/GDD.md` §52) must be reproducible from serialized data alone — never from scene object references.
7. **Every risk gets named.** If you write code that has a GC allocation in a per-frame path, a race condition in async loading, or a save-corruption edge case, you must say so out loud in your response and in the PR description — silence on a known risk is itself a defect.
8. **Never guess in the dark.** If the task is ambiguous, or the GDD and the codebase disagree, stop and ask (see §8). Do not silently pick an interpretation and proceed.
9. **Preserve behavior unless a change was asked for.** Refactors must not change observable gameplay behavior unless the task explicitly requests it.
10. **Tutorial-grade code is a defect**, not a starting point. No "TODO: handle this later" in submitted work unless it is tracked in `docs/DECISIONS.md` as an explicit, human-approved deferral.
11. **Skills & MCP first — no manual bypass.** If an installed skill or MCP server covers the task, the agent MUST invoke it via the `skill` / MCP tool. Re-implementing by hand when a skill/MCP exists is a violation of §11 and fails `ARCHITECTURE_CHECK`.

---

## 3. Technology Baseline

Assume, unless a task or ADR says otherwise:

| Concern | Standard |
|---|---|
| Engine | Unity 6 (Unity 6000.x LTS) |
| Input | New Input System only — legacy `Input.GetKey*` is forbidden |
| Dependency Injection | VContainer (constructor injection; `LifetimeScope` per composition root) |
| Async | `Awaitable` / `UniTask` — never blocking `Task.Wait()`/`Thread.Sleep` in gameplay code |
| Asset loading | Addressables — no `Resources.Load` for new content |
| Data / Balancing | `ScriptableObject`-based configuration assets, not hardcoded constants or magic numbers in MonoBehaviours |
| Module boundaries | Assembly Definitions (`.asmdef`) per layer, per feature — see `docs/ARCHITECTURE.md` |
| Testing | Unity Test Framework (NUnit) — EditMode for pure logic, PlayMode for integration |
| Version control | Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `perf:`) |
| Skills runtime | OpenCode `skill` tool — every task must check `.opencode/skills/` catalog (§11.1) |
| MCP runtime | `opencode.json:mcp` — `context7` (remote) + `unity` (local anklebreaker-unity-mcp) (§11.2) |

---

## 4. Layered Architecture (summary — full detail in `docs/ARCHITECTURE.md`)

```
Presentation   → MonoBehaviours, UI, VFX, animation glue, input adapters
Application    → Use-cases / systems that orchestrate domain logic (MissionFlowService, EvidenceCollectionService…)
Domain         → Pure C# (no UnityEngine references): rules for evidence, inventory, mission state, puzzle logic
Infrastructure → Save/load, Addressables loading, audio backends, file I/O, platform services
```

**Dependency direction is one-way inward:** `Presentation → Application → Domain`. `Infrastructure` implements interfaces defined in `Domain`/`Application` and is injected in, never referenced directly by Domain. **Domain must never `using UnityEngine`.** This is the single most important rule in this repository and is enforced by `agent/rules.json`.

---

## 5. Forbidden Patterns

An agent must never introduce the following. If existing code already does this, flag it in `docs/DECISIONS.md` as tech debt rather than silently replicating it elsewhere.

- `Singleton<T>` / static "Manager" god-objects holding mutable game state
- `FindObjectOfType`, `FindObjectsOfType`, `GameObject.Find` in gameplay or Domain/Application code
- MonoBehaviours over ~200 lines or doing more than one job (movement AND inventory AND save, etc.)
- Magic numbers/strings for gameplay tuning — use `ScriptableObject` configs
- Catching exceptions without handling or re-throwing them (`catch {}`)
- `async void` outside of top-level Unity event handlers (`Start`, `OnClick`, etc.)
- Direct field access across systems instead of interfaces (`otherSystem.someField = x`)
- Circular references between Assembly Definitions
- Committing generated files (`Library/`, `Temp/`, `obj/`, `.csproj`, baked lighting data) — see `.gitignore`
- **Bypassing an available skill or MCP server** to do the same work manually (see §11) — e.g., hand-writing an ADR without invoking `architecture-decision`, or calling Unity HTTP bridge directly instead of `unity_*` MCP tools

---

## 6. Required Workflow

You do not decide your own process. You execute `agent/workflow.json`, a strict state machine with named phases, entry/exit criteria, and required artifacts per phase. Do not skip phases. Do not merge phases to save time. Each phase's exit criteria must be satisfied and stated explicitly before moving to the next.

At minimum, every task passes through: **INTAKE → CLARIFY → PLAN → ARCHITECTURE_CHECK → IMPLEMENT → SELF_REVIEW → TEST → DOCUMENT → HANDOFF**. See `agent/workflow.json` for the authoritative definition, gate conditions, and required outputs of each phase.

### 6.1 Skills & MCP gate inside the workflow (binding)

Every phase MUST check §11 before acting:

| Workflow phase | Mandatory check |
|---|---|
| **INTAKE** | Run `help` or `project-stage-detect` to locate the task in GDD/architecture; use `unity_unity_project_info` / `unity_unity_scene_info` via Unity MCP if the task touches scenes/prefabs. |
| **CLARIFY** | If GDD/architecture/code disagree, invoke `help` to surface next-step, and `context7` MCP to verify API/engine assumptions before asking a human. |
| **PLAN** | If the task creates or changes architecture, invoke `create-architecture` or `architecture-decision` — do not plan ad-hoc. |
| **ARCHITECTURE_CHECK** | Validate plan against `agent/rules.json` AND against the skill/MCP that will implement it — a plan that ignores an applicable skill fails this gate. |
| **IMPLEMENT** | All Unity Editor mutations (GameObject, scene, prefab, asset, material, component) MUST go through `unity_*` MCP tools (`unity_unity_gameobject_*`, `unity_unity_scene_*`, `unity_unity_asset_*`, `unity_unity_script_*`, etc.). Never use direct HTTP bridge calls, never hand-edit `.unity`/`.prefab` YAML. For library/API usage, fetch current docs via `context7` MCP (`ctx7` CLI) before coding. |
| **SELF_REVIEW** | Re-run the §11 skill/MCP checklist: "Did I bypass any skill/MCP that should have been used?" If yes, return to PLAN/IMPLEMENT. |
| **TEST** | **PLAY+EXECUTE binding:** For any code/mechanics change, enter `unity_unity_play_mode:play`, trigger the affected mechanic (e.g., `Inspection Hotspot examine`, `Inventory Take`, `PuzzleView` panel), capture `unity_unity_console_log` + `unity_unity_get_compilation_errors` (+ `unity_unity_screenshot_game` or `unity_unity_execute_code` for text-mechanics state). Only PASS after console shows zero `MissingComponentException`/`ArgumentException`. Then `unity_unity_play_mode:stop`. Never claim tests pass without MCP execution. |
| **DOCUMENT** | If an ADR or architecture change occurred, it MUST have been produced via `architecture-decision` / `create-architecture` skill output, not a free-form markdown write. |
| **HANDOFF** | Report must list every skill and MCP tool invoked (or "none applicable — justification: ...") per §9 **plus MCP play-verify evidence (duration, console snapshot, screenshot if any)**. |

Skipping a required skill/MCP invocation **or skipping MCP PLAY VERIFY** is a **workflow violation** equivalent to skipping a phase.

---

## 7. Definition of Done

A task is not done because it compiles. See `docs/DEFINITION_OF_DONE.md` for the full checklist. No task is handed back to a human as complete unless every applicable box is checked and the agent has stated so explicitly.

Additional DoD items from §11 (always applicable):

- [ ] Applicable skill(s) from §11.1 were invoked, or explicitly justified as not applicable.
- [ ] Applicable MCP server(s) from §11.2 were used for every Unity operation and every external-library doc lookup, or explicitly justified as not applicable.
- [ ] **MCP PLAY VERIFY (binding, 2026-09-17):** If any `.cs`, `.prefab`, `.unity`, `.asset`, or `InputActionAsset` changed, the agent MUST enter Play Mode via `unity_unity_play_mode:play`, execute the affected mechanic/UI (e.g., trigger PuzzleView, confirm Inventory/Inspection/Hotspot), and verify via `unity_unity_console_log` + `unity_unity_get_compilation_errors` + `unity_unity_screenshot_*` or `unity_unity_execute_code` that no `MissingComponentException`/`ArgumentException`/`NullReferenceException` remains. Claiming "fixed" without MCP play+execute is a DoD failure. Stop Play via `unity_unity_play_mode:stop` after verify.
- [ ] Handoff report lists skills/MCP used (see §9) **including the MCP play-verify evidence (play duration, console entries checked, screenshot path if taken)**.

---

## 8. When an Agent Must Stop and Ask a Human

Stop immediately and ask, rather than proceeding on an assumption, when:

- The GDD, the architecture doc, and the existing code disagree with each other.
- A task requires crossing a layer boundary defined in §4 in a new way.
- A task implies changing save-data schema (risk of corrupting existing save files, GDD §52).
- A task touches the combat/Adrian encounter state machine (highest narrative and technical risk system in the game per GDD §54).
- Performance budgets are unclear and the change is in a per-frame hot path.
- You are about to delete or substantially rewrite code you did not write this session without an explicit instruction to refactor it.
- A required skill or MCP server is unavailable/broken and the task cannot be completed idiomatically without it — report which skill/MCP failed and propose a fallback, do not silently hand-roll.

State your assumption explicitly if you must proceed without an answer (per Bug Investigation / Refactoring conventions in `docs/CODING_STANDARDS.md`), and flag it prominently so a human reviewer cannot miss it.

---

## 9. Reporting Format

Every agent response that produces or changes code must include, in this order:

1. **What changed and why** (one paragraph, plain language).
2. **Architecture impact** — which layer(s) touched, any new dependencies introduced.
3. **Skills & MCP used** — list every skill invoked via `skill` tool (`/architecture-decision`, `/create-architecture`, `/gate-check`, etc.) and every MCP server/tool used (`context7` → `ctx7 docs`, `unity` → `unity_unity_*`), with one-line purpose per invocation. If no skill/MCP was applicable, state `None — justification: [why no skill/MCP covers this task]`. Omitting this section is a DoD failure.
4. **Risks** — performance, concurrency, save-compat, coupling — explicitly, even if "none identified."
5. **Definition of Done checklist** — ticked against `docs/DEFINITION_OF_DONE.md` plus §7 addendum above.
6. **Follow-ups** — anything intentionally deferred, and why, logged as a candidate ADR.

No response that changes code may end with only a diff and no risk statement.

---

## 10. Ownership

This file, `docs/ARCHITECTURE.md`, and `agent/workflow.json` are owned by the technical lead. Agents may propose changes to them (as a diff plus rationale) but may not silently edit them while executing an unrelated task.

---

## 11. Skills & MCP — Mandatory Tooling (binding)

> **Rule:** Every agent MUST discover, select, and invoke the relevant installed **skills** and **MCP servers** before doing equivalent work manually. This section is not advisory — it is enforced at `ARCHITECTURE_CHECK` and `SELF_REVIEW`, and audited in the Handoff report (§9.3).

### 11.1 Installed Skills — catalog and when to use them

Source of truth: `.opencode/skills/*/SKILL.md` (authoritative) and `opencode.json` instructions. The table below is a binding summary — if the filesystem and this table disagree, the filesystem wins but the agent must flag the drift.

| Skill (invoke via `skill` tool) | Purpose | MUST be used when… |
|---|---|---|
| `start` | Onboarding — asks where you are, routes to correct workflow | Task starts from zero / user says "where do I start" / fresh clone with no `production/stage.txt` |
| `concept-brainstorm` | Guided game concept ideation → structured concept doc | Creating or revising `design/gdd/game-concept.md`, defining pillars, core loop |
| `map-systems` | Decompose concept into systems, map dependencies, create `systems-index.md` | Any task that adds/splits/merges a GDD system; before writing new GDDs |
| `create-architecture` | Produce `docs/architecture/architecture.md` master blueprint | Before any code when architecture is missing/outdated; when `gate-check` requires architecture traceability |
| `architecture-decision` | Author a single ADR in `docs/architecture/adr-*.md` | Any decision that touches layer boundaries, engine APIs, save schema, performance budgets, or patterns in `agent/rules.json` |
| `setup-engine` | Pin engine/version in AGENTS.md, populate `docs/engine-reference/` | Engine or version changes; `VERSION.md` missing; post-cutoff API risk unknown |
| `project-stage-detect` | Full project audit — stage, gaps, next steps | User asks "where are we", "what stage", "full audit"; at INTAKE for unfamiliar tasks |
| `gate-check` | Formal PASS/CONCERNS/FAIL gate verdict for phase transitions | Before advancing phases (`workflow-selection`, `systems-design → technical-setup`, `technical-setup → pre-production`, etc.); required by `agent/workflow.json` gates |
| `help` | Lightweight "what do I do next" based on current artifacts | User says stuck/lost/next step; quick orientation without full audit |
| `init-template` | First-time repo setup — transform template into clean project | Fresh project bootstrap only |
| `find-docs` / `ctx7` CLI via `context7` MCP | Fetch current library/framework docs (see §11.2) | Any task touching an external library, SDK, framework, or CLI tool — even well-known ones (React, VContainer, UniTask, Addressables, Input System) |
| `customize-opencode` | Edit `opencode.json`, `.opencode/`, agents, skills, MCP, permissions | Only when the task is explicitly to configure OpenCode itself |

**Invocation rules:**

1. **Check first, code second.** At INTAKE/PLAN, the agent MUST `Read` or `Glob` the skill catalog and decide which skill(s) apply. If one applies, the agent MUST call `skill` with that `name` — not replicate its steps manually.
2. **No partial bypass.** Do not "borrow the template" from a skill's `SKILL.md` and write the artifact by hand. Invoke the skill so its workflow, gates, and director reviews run.
3. **Composition:** A single task may require multiple skills in sequence (e.g., `project-stage-detect` → `create-architecture` → `architecture-decision`). Chain them; do not collapse.
4. **Justification if skipped:** If the agent believes no skill applies, it must state in PLAN and HANDOFF: `No skill applicable — reason: [one sentence] + catalog checked at [path]`.

### 11.2 Installed MCP Servers — catalog and when to use them

Source of truth: `opencode.json` → `mcp` and `.opencode/mcp/`. These are not optional integrations — they are the **only** supported way to perform their respective operations.

| MCP server | Type | Tools / CLI | MUST be used for… | FORBIDDEN alternative |
|---|---|---|---|---|
| `context7` | remote `https://mcp.context7.com/mcp` | `ctx7` CLI: `npx ctx7@latest library <name>` + `npx ctx7@latest docs <libraryId> "<query>"` ; MCP tools: `context7_resolve-library-id`, `context7_query-docs` | Every external library/framework/SDK/API doc lookup (VContainer, UniTask, Addressables, New Input System, DOTween, etc.) — even if the agent "knows" the API. Training data is not authoritative. | Guessing API signatures from memory; web search without ctx7; copying outdated StackOverflow snippets |
| `unity` | local `npx -y anklebreaker-unity-mcp@latest` (bridge port `7890`, hub at `C:\Program Files\Unity Hub\Unity Hub.exe`) | All `unity_*` tools: `unity_unity_editor_ping`, `unity_unity_scene_*`, `unity_unity_gameobject_*`, `unity_unity_asset_*`, `unity_unity_script_*`, `unity_unity_component_*`, `unity_unity_search_*`, `unity_unity_play_mode`, `unity_unity_console_log`, `unity_unity_get_compilation_errors`, `unity_unity_build`, `unity_unity_packages_*`, `unity_unity_screenshot_*`, `unity_unity_execute_code`, `unity_list_instances`, `unity_select_instance`, `unity_advanced_tool`, etc. | **All** Unity Editor mutations and inspections: creating/deleting GameObjects, editing scenes/prefabs/materials/components, reading/writing scripts, searching by component/tag/layer, building, play-mode control, console/compilation checks, screenshots. | Direct HTTP calls to `http://127.0.0.1:7890/api/...`; hand-editing `.unity`/`.prefab`/`.mat` YAML; `File.WriteAllText` to `Assets/` without going through MCP; `GameObject.Find` / `FindObjectOfType` for discovery (use `unity_unity_search_*` instead) |

**MCP invocation rules:**

1. **Unity → MCP only.** Every Unity operation MUST go through a `unity_*` MCP tool. The HTTP bridge is an internal transport — calling it directly bypasses the multi-agent queue, agent tracking, and safety checks and is forbidden.
2. **Multi-instance safety.** On first Unity call, instances are auto-discovered. If multiple editors are running, call `unity_list_instances` then `unity_select_instance` and include `port` on every subsequent call.
3. **Docs → context7 first.** Before writing code that calls any external library, resolve the library via `context7_resolve-library-id` (or `npx ctx7@latest library`) and fetch docs via `context7_query-docs`. Do this even for "well-known" APIs — Unity 6 / VContainer / UniTask APIs change.
4. **No silent fallback.** If an MCP server is down or a skill fails with a quota/error, the agent must report the failure, suggest the fix (`npx ctx7@latest login` / set `CONTEXT7_API_KEY` / restart Unity bridge), and not silently fall back to training-data guesses.
5. **Evidence in Handoff.** List each MCP tool invoked and what it did (e.g., `unity_unity_gameobject_create: created EvidencePickup prefab`). If no MCP was needed, justify why (e.g., "Domain-only change — no Unity Editor mutation, no external docs needed").

### 11.3 Enforcement & auditing

- **ARCHITECTURE_CHECK gate** fails if PLAN does not name the required skill(s)/MCP server(s) or if it proposes a manual path where a skill/MCP exists.
- **SELF_REVIEW** must explicitly grep the diff and the session's tool-call log for skill/MCP bypass (e.g., direct `Resources.Load` when Addressables + `context7` docs were required, or raw file writes to `Assets/` instead of `unity_unity_asset_*`).
- **TEST gate — MCP PLAY VERIFY:** `agent/rules.json:VERIFY-001` (blocker) fails if a mechanics/UI code change has no `unity_unity_play_mode:play` + `unity_unity_console_log`/`unity_unity_get_compilation_errors` evidence in the session log. Screenshot or `unity_unity_execute_code` state dump required for text-only mechanics.
- **CI future:** `agent/rules.json` will gain `SKILL-001` / `MCP-001` / `VERIFY-001` lint rules mirroring this section; until then, enforcement is via agent self-check and human review of §9.3.
- **Violation handling:** If an agent discovers it already bypassed a skill/MCP mid-task, it must stop, invoke the correct skill/MCP to redo or validate the work, and note the correction in HANDOFF. Shipping a bypass without disclosure is a defect.

---

## 12. Change Log for This Document

| Date | Author | Change |
|---|---|---|
| 2026-09-17 | Sinergasia Studio (agent) | Add §11 Skills & MCP mandatory tooling (binding): enumerate 10 installed skills + 2 MCP servers, bind each workflow phase to required skill/MCP checks, forbid manual bypass, require §9.3 reporting. Amend §0 read order, §2 constitution, §3 baseline, §5 forbidden patterns, §6 workflow, §7 DoD, §8 escalation, §9 reporting. |

