# Source review and workflow rationale

Reviewed 2026-09-23 from `ChatGPT.zip` and the later supplied `AGENTS(1).md`. This is a targeted source review of assemblies, composition, gameplay flows and representative tests, with a full file inventory. It is not an exhaustive game audit or a Unity test run. Source paths use the `SOURCE_ROOT` convention from `agent/code-map.md`.

## Findings that affect future tasks

| ID / priority | Evidence in supplied code | Consequence and next verification |
|---|---|---|
| F01 / P0 | `Presentation/PlayerMovementPresenter.cs` declares `machine` as `IMovementStateMachine`. `.Motor.cs:57` calls `machine.RequestEnter(MovementState.Jump, hasObstacle)`. `Domain/IMovementStateMachine.cs:9` exposes only the one-argument overload. The two-argument member exists only on `Application/MovementStateMachine.cs`; no matching extension was found in the supplied C# | Static interface/call mismatch: likely compilation blocker in the supplied source. Compile in the full Unity project first; repair the intended interface contract and relevant fakes under an authorized bugfix. No compiler output was produced here |
| F02 / P1 | `PlayerMovementPresenter.Motor.cs`, `UpdateJump`, sets `verticalVelocity` before `RequestEnter` succeeds | Rejection may leave a physical jump impulse even though state/stamina entry was denied. Reproduce through the real presenter with insufficient stamina and invalid transition. A state-machine-only test cannot detect the earlier field write |
| F03 / P1 | `WorldInspectableItem.Consume` deactivates the object by default; `OnDisable` unregisters its Restore callback. `AutoReturnService.EnqueueRecovery` can immediately restore only registered callbacks | Objective-item recovery path is suspect after actual consumption. Trace Take → deactivation → drop → recovery/reload; do not treat ADR-0012's historic “resolved” text as live proof |
| F04 / P1 | `JsonFileSaveRepository.WriteAtomicAsync` is fire-and-forget `async void`, uses the same `.tmp` path per store, deletes the prior destination before `File.Move`, and has no visible save/clear serialization. Existing checkpoint tests construct the service without a repository | Concurrent operations and interruption can lose or reorder data; delete-then-move does not provide atomic replacement of an existing file. ADR-0016 accepts this particular async-void lifter, not every persistence hazard. Verify rapid saves, immediate load, clear during save and failed replacement before proposing a scoped repair |
| F05 / P1 | `CheckpointData` includes pose, health/stamina, scene, milestone and recovery/hotspot keys; it does not include inventory, evidence-log contents, puzzle states or mission index. `DeathReloadPresenter` restores position/optional rotation then resets health | Full progression restoration is not established by this snapshot. Agree on the required snapshot boundary before extending the save schema; use the explicit schema/ADR gate. Do not claim death/restart reconstructs every system |
| F06 / P2 | `PlayerInteractor` and `PlayerMovementPresenter` subscribe in `Start` and unsubscribe in `OnDisable`, without a corresponding re-subscription path shown | Disable/re-enable can lose interaction/movement event handling. Exercise the actual lifecycle after pooling or scene transitions. This is a source-derived risk, not a recorded runtime failure |
| F07 / P2 | `PhysicalInventoryService.TransferToPocket` assigns `slots[pocketIdx] = slots[sourceIdx]`; `InventorySlot` contains its own `Type` | The returned slot metadata can retain a hand type at a pocket index. Verify `GetSlots()` and HUD/consumer expectations after Hand → Pocket |
| F08 / documentation | Older rules point to `Assets/_Project/Source`, `EOS.*` and nonexistent `rules/` files. ADR-0018's final entry permits the Infrastructure reference at `GameLifetimeScope` | Update workflow scopes and source links; preserve the precise exception. Do not trigger a namespace migration or remove the composition reference as an incidental “fix” |

Priorities above are review recommendations, not new implementation authorization. No listed gameplay defect was patched by this workflow task.

## Design conflicts deliberately left unresolved

`docs/GDD.md` and the supplied `AGENTS.md` use **James Forryas**; numbered narrative documents use **James Farryas**. The condensed GDD gives a 2+ hour length while `docs/12-references-and-open-tbd.md` leaves total duration open. Record both sources and ask only when a requested change depends on resolving them. This package does not choose new canon or final tuning.

## Missing inputs and unavailable capabilities

- The archive has six nested ZIPs. `AGENTS.md` was supplied separately later; its original bytes are included unchanged.
- `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`, `Packages/manifest.json`, `opencode.json`, the referenced input-actions asset, canonical `PunyaPandu` scene and full source DOCX are absent from the export. Their state in the real repository is unknown.
- Ten installed skill files were enumerated. `customize-opencode` and `find-docs` were named in the contract but not found among those installed files. Generic skill/template paths differ from this project's actual paths.
- `.agents/mcp_config.json` declares `unity-mcp`; local server source exists. No live Unity or context7 MCP tools, OpenCode executable, Unity editor or C# compiler were exposed in the authoring session.
- The local `help` and `project-stage-detect` instructions were read for compatibility analysis. They were **not invoked through an OpenCode skill runtime**. No director or specialist agent was spawned; the review was performed by one agent.
- Official OpenCode documentation was consulted using the available web lookup to check artifact syntax. This was not a context7 invocation. The package is therefore not represented as a fully executed project-toolchain run under `AGENTS.md §11`.

## Follow-up resolutions (2026-09-28 session)

The original table above is preserved as the 2026-09-23 observation baseline. Status changes below are dated and evidence-linked; runtime-pending items are marked as such, not claimed as verified.

| ID | Status 2026-09-28 | Evidence |
|---|---|---|
| F01 | **Resolved (static)** | `Domain/IMovementStateMachine.cs:9-10` now exposes both `RequestEnter` overloads; `Application/MovementStateMachine.cs:32-35` implements both; `PlayerMovementPresenter.Motor.cs:55` two-arg call compiles (`dotnet build` 0 warnings/errors). Contract repair per ADR-0020 M2/M3. |
| F02 | **Resolved (static)** | `PlayerMovementPresenter.Motor.cs:45-62` — `verticalVelocity` is now assigned only after `machine.RequestEnter(Jump, hasObstacle)` returns true; Domain owns the LimitedJump gate. Requires Play Mode jump-rejection sizing to close fully. |
| F03 | **Fixed, runtime-pending** | `WorldInspectableItem` gains `OnDestroy` unregister (dead callbacks cannot linger) + `Consume` guard (objective + destroyOnTake → deactivate with warning, never Destroy). `AutoReturnService.EnqueueRecovery` only clears pending / publishes Recovered after ≥1 successful restore, per its own comment. 4 Application + 2 Presentation regression tests added; EditMode + Play Mode Take→drop→recovery trace pending. |
| F04 | **Fixed, runtime-pending** | Delete-then-move replaced with atomic `File.Replace`-if-exists / `Move`-if-new (`MoveReplacing`); dead sync `WriteAtomic` removed; new `DrainAsync()` hook observes fire-and-forget completion without changing `ISaveRepository` (async-void lifter kept per ADR-0016). Schema untouched — Version stays "4", no migration. 5 real temp-dir tests added (`SaveRepositoryTests`: round-trips, 20× rapid-saves-last-wins + no orphan tmp, clear semantics, store independence); EditMode run pending. |
| F05 | **Resolved (reported, prior session)** | `ADR-0022` extended save to v4 full snapshot (inventory/evidence/puzzle/mission). Live death/reload round-trip re-verification still pending. |
| F06 | **Fixed, runtime-pending** | `PlayerInteractor.cs` (~145L), `PlayerMovementPresenter.cs` (~235L: core + partials), plus same-class sweep fix `StaminaHudPresenter.cs` (125L) now use guarded `SubscribeOnce()` from `Start` + `OnEnable`, cleared in `OnDisable`. Compiles clean; disable/re-enable Play Mode check pending. |
| F07 | **Fixed, runtime-pending** | `PhysicalInventoryService.TransferToPocket` now constructs `new InventorySlot((InventorySlotType)pocketIdx, …)`; all slot writes in the file use destination-index types (rg-verified). Two regression tests added (`TransferToPocket_SetsPocketSlotType`, `TransferToPocket_RoundTrip_PreservesSlotTypes`); EditMode run pending. `InventoryGridMoveTests.MoveSlot_EmptySlot_NoOp` assertion also corrected (pocket-first placement) + occupied same-slot hardening. |
| F08 | **Partially addressed** | v2 bundle (`workflow.json`, `rules.json` scope_resolution, `code-map.md`) already uses `SOURCE_ROOT = Assets/_Yumiwa`; `EOS.*` vs `Yumiwa.*` drift remains under ADR-0010 (Proposed). |

## Changes made by this package

New portable/OpenCode agent and task command; v2 workflow and scoped rule inventory; code map, verification matrix, task/resume/narrative templates; read-only validator and reproducible source anchors. The original workflow/rules/template are retained for review. The uploaded AGENTS contract, game source, docs/ADRs, design canon, Unity assets and installed skills are unchanged.

The validator demonstrates bundle consistency and the narrow static properties documented in its output. It cannot certify gameplay, full architectural conformance, tool invocation, test coverage or permissions. Future implementation acceptance still requires the project's actual skills/MCP gates and live verification.
