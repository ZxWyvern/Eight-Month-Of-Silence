# Verification matrix

Apply only affected rows, plus the universal gates. All nine workflow phases still run. `NOT_APPLICABLE` needs a scope reason; `BLOCKED` and `NOT_RUN` never count as `PASS`.

## Universal gates

| Gate | Required evidence |
|---|---|
| Contract and context | Current `AGENTS.md` and required docs read; selected route; actual source files and related tests read; tool/skill availability recorded |
| Static workflow | `python agent/tools/validate_workflow.py --root .`; this validates workflow structure/source anchors/assembly boundaries only, not Unity compilation or every rule |
| Diff and compatibility | Full relevant diff, no unrelated changes, `.meta`/serialized references preserved, partial classes reviewed together, baseline debt distinguished from new defects |
| C# runtime change | Current Unity compilation result, no new warnings, affected existing EditMode tests and meaningful regression tests; fresh MCP Play+Execute evidence under `AGENTS.md §7` |
| Scene/prefab/asset/input change | Correct Unity instance/project selected; editor change through MCP; save/reopen evidence; missing-reference check; player-facing flow exercised |
| External API change | Actual dependency version and successful context7 retrieval, as required by the project; no signatures guessed from memory |
| Docs/workflow-only change | JSON/frontmatter/reference checks; workflow semantics review; Unity gates `NOT_APPLICABLE` only when no runtime or Unity asset changes occur |

## Feature checks

| Area | Existing test entry points | Required additional checks when affected |
|---|---|---|
| Movement | `MovementStateMachineTests`, `PlayerMovementStateTests`, `MovementStateTableTests`, `AirborneMomentumTests`, `SlopeSnapModelTests`, `StaminaServiceTests`, `StaminaPoolTests`, `ConfigDefaultsTests` | Compile presenter calls against interface, not just concrete class. In Play Mode check walk/sprint/crouch/limited jump, rejection with insufficient stamina, slope/landing, inspection locking, disable/re-enable and camera settings. State rejection must not already move the player |
| Inspection | `InspectionSessionServiceTests`, `InspectionRotationModelTests` | Take exactly once, full inventory rejection keeps item/session valid, critical/optional evidence bypasses slots, correct hotspot angle/distance, discovery survives Cancel/reopen. Re-enter after deactivation; restore input, cursor and preview on every exit. Include the physical inventory, not only the simple test implementation |
| Inventory/recovery | `PhysicalInventoryServiceTests`, `SimpleInventoryServiceTests`, `EvidenceLogServiceTests` | Six-slot cap; swap/replace/transfer both directions; reported slot type matches destination; use only hand items; objective item Take → DropWithRecovery → visible world recovery → retake; recovery still works after disable and checkpoint/load |
| Puzzle | `PuzzleServiceTests` | Missing prerequisites reject; incorrect answer retries without loss; correct solve emits once; UI closes and controls restore; solved state and progression remain coherent after reload |
| Mission/objective | `ObjectiveServiceTests`; no dedicated `MissionFlowServiceTests` supplied | Empty sequence, start/advance/end boundaries, stable mission IDs, repeated trigger, unmet objectives and checkpoint interaction. HUD is a view; trace actual progression ownership |
| Traps | No dedicated `TrapServiceTests` supplied | Warning causes no damage; cue is audible/readable; exit/disarm before expiry prevents damage; repeat/re-entry/death/disable cancels stale routines; verify chosen pause/time-scale behavior. Calling `Trigger` directly does not prove warning cancellation |
| Save/checkpoint | `CheckpointServiceTests` (in-memory only in supplied code) | Real temporary-directory repository tests; await/observe completion; rapid consecutive saves, save-then-load, save-then-clear, malformed/older versions, interrupted replacement and independent chapter/house stores. Compare full requested restore state, not just position. Migration requires explicit scope/ADR |
| Health/flashlight/censor | `HealthServiceTests`, `CensorServiceTests`; no dedicated flashlight fixture supplied | Wounded/healed transitions, flashlight held/pocketed/on/off/depletion, UI subscriptions after reload; named settings actually change their presentation |
| Narrative/audio/level beat | Relevant underlying service tests; no general beat-runner tests supplied | Trigger/preconditions, player action and information gained, once/repeat semantics, interrupt/cancel, retry/reload, input/camera/audio cleanup, observable consequence and visual/text fallback. Mark unimplemented infrastructure explicitly |
| Performance | No profiler capture supplied | Reproduce the relevant hot path in the actual project/hardware, record frame-time/GC scope and method. “0 B” requires measurement; initial event-list creation, UI polling and preview allocation must not be hidden by steady-state claims |

## MCP Play+Execute evidence record

Record project path, Unity version if available, selected instance/port, scene, change identifier or diff, start/end time, duration, player actions, expected/observed states, compilation result, console entries checked and artifact paths. Check all fresh exceptions/errors, including MissingComponentException, ArgumentException and NullReferenceException. Explain relevant pre-existing failures rather than presenting them as fixed.

Use the actual exposed MCP tools: discover/connect/select → compile → enter Play Mode → perform the mechanic → read state and presentation → collect logs plus screenshot/state dump → stop Play Mode. Record whether execution used player input, a public API or instrumentation. Public API calls can verify service state but do not alone prove input, collision or UI wiring.

If the editor is absent, Unity compilation and gameplay are `BLOCKED` for implementation acceptance. Reading historical logs, parsing scenes or counting tests cannot substitute. A read-only source review may still be handed off as a source review with that limitation.
