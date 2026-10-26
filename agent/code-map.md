# Current code map

Basis: supplied snapshot, reviewed 2026-09-23. `SOURCE_ROOT` means `Assets/_Yumiwa` in a full project or `_Yumiwa` in the archive. Paths below are relative to it. Recheck current files before using this map for a change.

## Assembly boundaries

| Assembly | Actual references in the snapshot | Engine references |
|---|---|---|
| `Yumiwa.Domain` | None | Disabled |
| `Yumiwa.Application` | Domain | Disabled |
| `Yumiwa.Infrastructure` | Domain, Application, Unity.InputSystem | Enabled |
| `Yumiwa.Presentation` | Domain, Application, Infrastructure, Unity.InputSystem, VContainer | Enabled |
| `Yumiwa.Presentation.Editor` | Domain, Infrastructure, Presentation, VContainer, Unity.Cinemachine, Unity.InputSystem | Editor only |
| `Yumiwa.Application.Tests` | Domain, Application, Infrastructure, UnityEngine.TestRunner, UnityEditor.TestRunner | Editor only |

Both Domain **and Application** are engine-free in their actual assembly definitions. Do not follow the older architecture document's optional Unity primitive allowance into Application. Presentation retains an Infrastructure assembly reference for `GameLifetimeScope`; ADR-0018 explicitly allows this composition-root exception. The assembly reference does not enforce the narrower per-file rule, so review imports and fully qualified type usage outside that root.

## System owners and flow

| Concern | Existing implementation and integration |
|---|---|
| Composition | `Presentation/GameLifetimeScope.cs` registers configs, typed event bus, input, movement, health, inspection, physical inventory, recovery, evidence log, checkpoints, objectives, missions, puzzles and traps; explicit `presenterRoots` feeds auto-injection |
| Config delivery | Infrastructure ScriptableObjects → `Infrastructure/DirectConfigProvider.cs` → `Domain/IConfigProvider.cs` and pure data structs. The current provider is direct; no Addressables loader is supplied |
| Movement | `Infrastructure/NewInputAdapter.cs` → `Domain/IPlayerInput.cs` → `Presentation/PlayerMovementPresenter*.cs` → `Application/MovementStateMachine.cs` / `Domain/PlayerMovementState.cs`; `CharacterController` applies motion |
| Movement feel | Presenter `.Camera.cs`, `.Motor.cs`, `.Sprint.cs`, `.Input.cs`; `Domain/AirborneMomentum.cs`, `SlopeSnapModel.cs`, `MovementStateTable.cs`; movement/stamina config assets |
| World interaction | `Presentation/PlayerInteractor.cs` uses `Physics.OverlapSphereNonAlloc` to choose a nearby available `IInspectable` on input. This is not the raycast implementation suggested by some design text |
| Inspection | `WorldInspectableItem` → `ItemInspectionController` plus `.Flow.cs` → `Application/InspectionSessionService.cs` → inventory or evidence log. Presenter, rotator, UI and `InspectionHotspotPresenter` provide the view |
| Inspection input | `Presentation/InspectionInputHandler.cs` owns map `Inspection`; the controller disables `Player` during inspection and restores it on exit. Verify cancellation, failure, deactivation and re-entry |
| Hotspots | `Domain/InspectionHotspot.cs`, `InspectionRotationModel.cs`, `InspectionProgress.cs`; session validates angle/distance, stores discovery by item, exports/imports keys |
| Inventory | Runtime registration uses `Application/PhysicalInventoryService.cs`, six slots: left/right hand + four pockets. `SimpleInventoryService` also exists, including in inspection tests; it is not the runtime physical inventory |
| Recovery | `PhysicalInventoryService.DropWithRecovery` → `Domain/IRecoveryPoint.cs` → `Application/AutoReturnService.cs` → registered `WorldInspectableItem.Restore` callbacks. Trace the object's disable lifecycle, not only service methods |
| Evidence | `Application/EvidenceLogService.cs` stores by ID. Critical evidence cannot be removed. Inspection routes critical/optional evidence to the log rather than physical slots |
| Inventory presentation | `InventoryPresenter`, `InventoryHudView`, `EquipmentPivotPresenter` plus `.Visuals.cs` map slots/hand state to UI and held models |
| Mission/objective | `Infrastructure/MissionSequenceAsset.cs` → `Application/MissionFlowService.cs`; `ObjectiveService.cs` owns objectives. `MissionHudPresenter.Start` auto-starts the prototype mission; `CheckpointTrigger` can advance/set mission index |
| Puzzle | `PuzzleDefinitionAsset` → `Application/PuzzleService.cs` → `PuzzleView` plus `.Logic.cs`/`.Trigger.cs` and `PuzzlePanelFactory`. The service checks evidence/item prerequisites, handles retry and publishes solve/fail events |
| Trap | `TrapDefinitionAsset` → `Application/TrapService.cs` → `Presentation/TrapZone.cs`. Zone starts warning cues and a real-time coroutine; leaving/disarming cancels pending damage |
| Health/flashlight | `HealthService` and `FlashlightService` behind Domain interfaces; `HealthHudPresenter`, `FlashlightView`, movement wounded feedback |
| Save | `Domain/CheckpointData.cs` / `ISaveRepository.cs` → `Application/CheckpointService.cs` → `Infrastructure/JsonFileSaveRepository.cs`; separate chapter and house files, current writer version `3` |
| Reload | `Presentation/DeathReloadPresenter.cs` loads snapshot, moves the player and resets health. Inspect the actual restored fields before claiming full world-state restoration |
| Events | `Domain/IEventBus.cs` + `Application/EventBus.cs`: `Subscribe`, `Unsubscribe`, `Publish` with struct events. It does **not** return `IDisposable` subscriptions as the older architecture example suggests |
| Tests | `Application/Tests/`: 19 `*Tests.cs` files plus `Fakes.cs`, within the editor-only test assembly. No PlayMode test assembly is supplied |

## Existing source anchors for authoring a story beat

`MissionChangedEvent`, `PuzzleSolvedEvent`, `PuzzleFailedEvent`, `TrapWarningEvent`, `TrapTriggeredEvent`, `InspectionStartedEvent`, `InspectionEndedEvent` and `ItemPickedUpEvent` exist. Follow their actual payloads and publishers. Hotspot discovery currently calls `IEvidenceLog.AddEvidence`; do not assume an `EvidenceCollected` or `HotspotDiscovered` event already exists.

No implementation of `FinalEncounterService`, `AdrianEncounterService`, `HorrorAudioCueService`, `IAudioBackend`, Timeline orchestration or a general narrative event runner was found in the supplied C# files. Some names occur only in comments or design documents. Adding one is a scoped design/engineering task, not a wiring step for an existing system.

## Input actions referenced by code

| Map | Referenced actions |
|---|---|
| Player | `Move`, `Look`, `Sprint`, `Jump`, `Attack`, `Crouch`, `Inventory`, `SwapHands`, `Drop`, `Use`, `Censor`; `PlayerInteractor` also reads `Interact` |
| Inspection | `InspectRotate`, `InspectZoom`, `InspectTake`, `InspectCancel`, `InspectResetRotation`, `InspectExamine` |
| UI | Map enabled by `NewInputAdapter`; exact bindings require the missing input-actions asset |

Never infer key bindings or action types solely from these names. The actual `InputActionAsset` and package version must be inspected through the required project tooling.

## Snapshot boundaries and older documentation

- The export places `_Yumiwa` at its root; this is not a reason to relocate a real Unity project's assets.
- `Assets/_Project/Source`, `EOS.*`, `EvidenceCollectionService` and several other names in the older architecture text are design examples or drift, not the present file tree. ADR-0010 remains **Proposed**, so retaining the current names is preservation of the snapshot, not invented approval of that ADR.
- ADR-0015 records `PunyaPandu` as a canonical build scene. Neither that scene nor build settings is included here. Scenes under `Archive/` are not automatically the intended runtime scene.
- Required numbered GDD files exist; `rules/agent-rules.md`, `rules/tbd-escalation-rules.md` and `GAME_DESIGN_DOCUMENT.docx` do not exist in this export. Their absence is reported, not filled with invented content.
- Ten skills are installed under `.opencode/skills/`. Their generic stage heuristics and some plugin paths assume `src/`, `.agents/skills/` or `docs/framework/`. Do not infer “fresh project” from those absent paths when the actual `Yumiwa` code is present; report the tool's path mismatch.
