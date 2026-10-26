# Eight Months of Silence — Agent Workflow v2

A project-specific workflow for the Unity `Yumiwa` code supplied in `ChatGPT.zip`, governed by the separately supplied `AGENTS(1).md`. This bundle creates the `eos-workflow` OpenCode primary agent and `/eos-work` command. It also supplies a portable agent profile for other coding assistants.

## Install in the existing project

For the live project's story/mechanics backlog, start with [Story mechanics workflow](agent/story-mechanics-workflow.md). It maps existing systems and missing mechanics to Chapters 1 through 5, with production ordered as progression/input fixes, MVP Chapter 1, vertical slice, then full-story expansion. It extends the same nine-phase agent; it does not run gameplay changes automatically. See the [authoring report](agent/story-workflow-report.md) for current evidence and tool limitations.

```text
/eos-work Audit gate MVP from agent/story-mechanics-workflow.md read-only and identify the next eligible ticket.
```

The installation and archive compatibility notes below describe the original 23 September 2026 export, not the current live Unity project's completeness.

1. Review the bundle in a branch or working copy. This is a workflow overlay, not a Unity project.
2. Keep the project's `AGENTS.md`. The copy included here is byte-for-byte identical to the uploaded `AGENTS(1).md`; merge manually if your local version differs.
3. Merge `agents/`, `agent/`, and `.opencode/` into the repository root. Replace only `agent/workflow.json`, `agent/rules.json`, and `agent/task-template.md`; the other supplied paths are additions relative to the supplied snapshot. Review any collision in a newer local project. Do not replace those directories wholesale. Originals of the three replaced files are in `agent/legacy-v1/` for comparison and rollback; do not execute them as active policy. This README is the bundle's guide; keep your repository's own README.
4. Keep the project's existing OpenCode permissions, model, MCP configuration, skills, and plugins. This bundle neither installs servers nor changes those settings.
5. Run `python agent/tools/validate_workflow.py --root .`. Then open OpenCode in the project and run the command below.

```text
/eos-work Investigate the movement RequestEnter interface mismatch; preserve gameplay behavior.
```

Other task examples:

```text
/eos-work Review objective-item recovery after Take, Drop and checkpoint reload. Read-only first.
/eos-work Design one investigation beat using existing inspection, puzzle and mission systems. Do not change canon.
/eos-work Refactor the inspection lifecycle while preserving input and cursor restoration.
```

For another coding assistant: “Read AGENTS.md, then agents/workflow-agent.md, and execute agent/workflow.json for this task: …”. The profile does not assume that OpenCode-only tools exist in another host. Required unavailable tools remain explicit blockers for the operations that need them.

## What is included

| File | Purpose |
|---|---|
| `AGENTS.md` | Original project contract, unchanged |
| `agents/workflow-agent.md` | Portable workflow agent responsibilities and execution instructions |
| `.opencode/agents/eos-workflow.md` | OpenCode primary-agent entry point; inherits model and permissions |
| `.opencode/agents/eos-story.md`, `eos-mechanics.md`, `eos-qa.md` | Story, mechanics and QA subagent profiles; inherit model and permissions |
| `agent/subagent-workflow.md` | Delegation brief, file ownership, exclusive Unity session and usage examples |
| `.opencode/commands/eos-work.md` | Task command, forwarding `$ARGUMENTS` to the agent |
| `agent/workflow.json` | All nine required phases, transitions, gates and task routes |
| `agent/story-mechanics-workflow.md` | Existing mechanics, canonical chapter beats, ordered implementation tickets and acceptance gates |
| `agent/story-workflow-report.md` | Story workflow authoring evidence and limitations |
| `agent/rules.json` | Existing rule IDs with real code scopes, ADR exceptions and evidence requirements |
| `agent/code-map.md` | Actual assembly graph, system owners and entry points |
| `agent/verification-matrix.md` | Tests and player interactions required for each affected system |
| `agent/review-findings.md` | Source findings, missing inputs and unverified claims |
| `agent/task-template.md` | Task, acceptance criteria, tool availability and phase evidence |
| `agent/session-template.md` | Resume context compatible with the existing session-state location |
| `agent/narrative-beat-template.md` | Trigger-to-consequence authoring without assuming a cutscene framework exists |
| `agent/tools/validate_workflow.py` | Read-only workflow structure, source-anchor and assembly-boundary checks |
| `agent/source-baseline.json` | Hashes and source anchors from the supplied snapshot |
| `agent/validation-report.json` | Actual static-validation result for this bundle and snapshot |
| `agent/bundle-qa.json` | Validation-script checks and unexecuted runtime checks |
| `agent/legacy-v1/` | Original workflow/rules/task template; comparison only |

## Compatibility and verification

The workflow retains `INTAKE → CLARIFY → PLAN → ARCHITECTURE_CHECK → IMPLEMENT → SELF_REVIEW → TEST → DOCUMENT → HANDOFF`, mandatory skills/MCP discovery, anti-soft-lock rules, design TBD ownership and Unity MCP Play+Execute verification. No game source, scene, prefab, asset, save format, design canon or existing skill was changed.

The archive is a partial project snapshot: 158 C# files, six assembly definitions and 19 `*Tests.cs` files. It lacks `ProjectSettings`, `Packages/manifest.json`, the referenced input-actions asset, and the canonical `PunyaPandu` scene. Unity, OpenCode and the Unity/context7 MCP tools were unavailable in the authoring environment. Static checks are evidence about this workflow bundle, not a successful Unity compilation, playtest, OpenCode load or game build.

The source review found a likely compilation blocker and several lifecycle/persistence risks; see `agent/review-findings.md`. Do not carry forward historical “PASS” statements as fresh results.

OpenCode adapter syntax was checked against the official [agent documentation](https://opencode.ai/docs/agents/) and [command documentation](https://opencode.ai/docs/commands/) on 2026-09-23. The adapter only uses `description`, `mode`, `agent`, and `$ARGUMENTS`. It does not require a particular model provider.
