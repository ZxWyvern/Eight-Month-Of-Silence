# ADR-0028: Tkp1 as the Canonical Chapter 1 Crime Scene

## Status

Proposed — implementation authorized 2026-11-02. This ADR becomes Accepted only after the Play/build verification listed below passes.

## Date

2026-11-02

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.8f1 (6000.3 LTS) + URP 17.3.0 |
| **Domain** | Core / Scene / UI |
| **Knowledge Risk** | LOW — the decision uses `SceneManager`, `Physics` overlap queries, `CharacterController`, VContainer `LifetimeScope`, and existing project services. `docs/engine-reference/` is absent in this repository, so this ADR relies on the repository's verified Unity 6 APIs and the existing Play-verified scene patterns. |
| **References Consulted** | `docs/ARCHITECTURE.md`, `docs/05-chapter-structure.md`, `docs/06-level-design.md`, `docs/07-ui-ux.md`, `docs/10-production-scope.md`, `docs/architecture/adr-0027-main-menu-boot.md`, `docs/DECISIONS.md` (ADR-0009, ADR-0018, ADR-0023, ADR-0024, ADR-0027) |
| **Post-Cutoff APIs Used** | None beyond the existing Unity 6 / VContainer APIs already used by the project. |
| **Verification Required** | `MainMenu` boots `Tkp1`; `Tkp1` can be completed end-to-end; completion returns to `MainMenu`; checkpoint/death/pause routes work in `Tkp1`; no console errors or missing references. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0009 (explicit `presenterRoots`, no scene search), ADR-0018 (`GameLifetimeScope` composition-root exception), ADR-0027 (`MainMenu` is build index 0 and the production boot scene) |
| **Enables** | A production-playable Chapter 1 slice and a truthful `MainMenu` → `Tkp1` boot path |
| **Blocks** | Chapter 1 acceptance and final build smoke testing |
| **Ordering Note** | Do not change `MainMenuPresenter.firstSceneName` or the build order until `Tkp1` passes Play and build verification. `PunyaPandu` remains the prototype fallback during implementation. |

## Context

### Problem Statement

`MainMenu` is now the production boot scene, but it still loads `PunyaPandu`, the prototype sandbox. `Tkp1` is the intended Chapter 1 crime scene, but it currently contains only environmental meshes, a directional light, and no player, camera, Canvas, EventSystem, composition root, or gameplay services. A player therefore cannot start or complete Chapter 1 from the production menu.

### Constraints

- `MainMenu` must remain the build index 0 production entry point per ADR-0027.
- `PunyaPandu` must remain available as the regression/prototype scene until `Tkp1` is verified; it is not deleted.
- Unity scene mutations must use the Unity MCP bridge, not YAML edits.
- Existing Domain/Application services and save schema v4 must be reused; no save-schema migration is planned.
- `Act1ObjectiveChain` currently advances the mission past `M01_Alley`; the chapter boundary must stop at `M01` and the existing test expectation must be corrected.
- Chapter 1 completion must not be confused with the final Adrian encounter or the house checkpoint system.
- No new singleton, `Find*`, or scene-wide discovery may be introduced.

### Requirements

- `Tkp1` must be playable from `MainMenu` with the existing player, movement, stamina, interaction, inspection, Evidence Log, inventory, reconstruction, dialogue, checkpoint, pause, settings, and fail-retry systems.
- The crime scene must present the three B1 evidence items and one reconstruction case in a readable spatial sequence.
- Completion must show a clear chapter-end state and return to `MainMenu`.
- Death and pause reload must remain safe in `Tkp1` and must not teleport the player into a snapshot from another scene.
- The scene must use the existing B1 ScriptableObjects and MainMenu build flow; placeholder art is acceptable for the MVP.

## Decision

Adopt `Assets/Scenes/Tkp1.unity` as the canonical Chapter 1 crime scene **after** it passes the validation criteria below. `MainMenuPresenter.firstSceneName` and the build order are updated only at the end of that verification. Until then, `MainMenu` continues to load `PunyaPandu`.

`Tkp1` is assembled by moving the existing PunyaPandu gameplay rig into the scene rather than reimplementing mechanics:

- `GameScope`/`GameLifetimeScope` with the existing service graph and explicit `presenterRoots`.
- `YumiwaPlayer`, `CameraTarget`, `Main Camera`, `InspectionRig`, `InspectionUI`, `EventSystem`.
- `StaminaHud`, `InventoryHUD`, `EvidenceLog`, `MissionHUD`, `PauseMenu`, `SubtitleView`, `TitleCardView`, `ReconstructionBoard`, and the B1 interaction objects.
- A new Presentation-only `ChapterEntryPresenter` starts `M01_Alley` explicitly instead of relying on `MissionHudPresenter.Start`.
- A new Presentation-only `ChapterCompletionPresenter` listens for `Act1ChapterCompletedEvent`, shows the chapter-end title card, and returns to `MainMenu`.

`Act1ObjectiveChain` stops at the Chapter 1 boundary: it completes the chapter and publishes `Act1ChapterCompletedEvent` without advancing to `M02_Motel`. `Act1ObjectiveChainTests` is updated to assert the chapter boundary rather than `M02_Motel`.

Reliability changes required by the move:

- `ChapterSavePoint` must handle spawn-inside-overlap by using an overlap-safe check/`OnTriggerStay` path, not only `OnTriggerEnter`.
- `DeathReloadPresenter` must only cold-resume a chapter save when `data.ActiveScene` matches the active scene.
- `PauseMenuPresenter` checkpoint loading must fall back to the chapter save and must not apply a snapshot from another scene.
- `FootstepPresenter` must not emit debug surface subtitles by default in the production scene.

### Architecture Diagram

```text
MainMenu (index 0)
  MenuManager
    GameLifetimeScope (mainMenuMode=true)
      ISaveRepository
      ISceneLoader
    MainMenuPresenter
      Mulai -> ClearAll() -> LoadSceneAsync("Tkp1")   [after verification]

Tkp1 (index 2 until promotion, then index 1)
  GameScope (GameLifetimeScope)
    presentation roots:
      YumiwaPlayer / CameraTarget / Main Camera / InspectionRig / InspectionUI
      StaminaHud / InventoryHUD / EvidenceLog / MissionHUD / PauseMenu
      ReconstructionBoard / SubtitleView / TitleCardView / ChapterEntryPresenter
    Application services:
      MissionFlowService / Act1ObjectiveChain / InspectionSessionService
      EvidenceLogService / PhysicalInventoryService / ReconstructionService
      CheckpointService / DialogueQueueService / SceneTransitionService
  ChapterSavePoint (b1_entry)
  B1 evidence: body / scratch / blood
  Reconstruction: case_b1
```

### Key Interfaces

- `ChapterEntryPresenter` — explicit `MissionFlowService.Start()` entry glue; no new Domain contract.
- `ChapterCompletionPresenter` — subscribes to `Act1ChapterCompletedEvent`; uses `ISceneLoader` or the existing scene transition boundary to load `MainMenu`.
- `Act1ObjectiveChain` — maintains the chapter boundary; `Act1ChapterCompletedEvent` remains the completion signal.
- `ChapterSavePoint.OnTriggerEnter/OnTriggerStay` — overlap-safe entry save; still calls only Application services.
- `DeathReloadPresenter.Start` — active-scene guard before applying a chapter snapshot.
- `PauseMenuPresenter.OnLoadCheckpoint` — chapter-save fallback plus active-scene guard.

## Alternatives Considered

### Alternative 1: Keep PunyaPandu as the production Chapter 1

- **Description**: Leave `MainMenu` loading `PunyaPandu` and treat the prototype as the shipped Chapter 1.
- **Pros**: No scene migration; all current mechanics already work there.
- **Cons**: The prototype map is explicitly a sandbox, not the crime-scene layout; it mixes test objects, traps, and prototype geometry; it does not satisfy the TKP Gang narrative space.
- **Rejection Reason**: The production menu would lead to a prototype, not the designed Chapter 1 scene.

### Alternative 2: Build Tkp1 from an empty scene

- **Description**: Recreate the player, camera, UI, composition root, and services inside `Tkp1` from scratch.
- **Pros**: Clean scene with no prototype residue.
- **Cons**: Duplicates wiring already proven in `PunyaPandu`; high risk of missing a serialized reference or a service registration; slow.
- **Rejection Reason**: Moving the verified rig preserves behavior and keeps the work focused on chapter content.

### Alternative 3: Duplicate the entire PunyaPandu scene and replace only the environment

- **Description**: Copy `PunyaPandu` to `Tkp1`, then delete the prototype map and place the crime-scene set.
- **Pros**: Maximum reference preservation.
- **Cons**: Leaves prototype traps, puzzle objects, checkpoints, and sample map residue unless every object is audited; risks carrying test-only content into production.
- **Rejection Reason**: A curated move is safer than a full-scene copy for a production chapter.

## Consequences

### Positive

- The production menu can lead to a purpose-built Chapter 1 scene without reimplementing gameplay systems.
- Existing Application/Domain contracts and save schema remain intact.
- Chapter completion has an explicit, testable boundary at `M01_Alley`.
- Checkpoint and pause reload behavior becomes safer for a second gameplay scene.

### Negative

- `Tkp1` must carry a copied PunyaPandu rig; serialized references need explicit verification.
- `MainMenu` must be switched from `PunyaPandu` to `Tkp1` only after verification, so the boot path temporarily differs from the final build.
- The MVP scene uses placeholder environment/props and may not fully express the final art direction.

### Risks

- **Missing serialized references after the move**: verify every presenter root, player reference, inspection controller, pause presenters, and B1 `WorldInspectableItem` in Play Mode.
- **Chapter completion soft-lock**: the completion presenter must fire once and always provide a return path; verify by replaying and reloading.
- **Wrong-scene chapter save**: death/pause reload could teleport the player if the active-scene guard is missing; verify by loading a save from another scene and by reloading `Tkp1`.
- **Boot regression**: changing `MainMenuPresenter` or build order before verification could make the build start in an unfinished scene; keep `PunyaPandu` as the boot target until `Tkp1` passes.
- **Duplicate B1 state**: moving evidence and the game scope must not create two `Act1ObjectiveChain` instances or two save points.
- **Performance**: the Tkp1 environment has 84 objects and ~13.8k triangles; the added rig is prototype-scale. Verify frame behavior in Play Mode and avoid per-frame allocations in new code.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `docs/05-chapter-structure.md` | Chapter 1 is the TKP Gang crime scene and precedes the motel | `Tkp1` becomes the canonical Chapter 1 scene; mission advance stops at `M01_Alley`. |
| `docs/06-level-design.md` | Crime-scene layout must support observation and evidence discovery | The scene places body, scratch, blood trail, and reconstruction board in a readable sequence. |
| `docs/03-core-gameplay-systems.md` | Evidence, inspection, reconstruction, inventory, anti-soft-lock | Reuses the existing B1 evidence, hotspot, reconstruction, inventory, and recovery services. |
| `docs/07-ui-ux.md` | Pause Menu includes Main Menu; minimal HUD; settings | Reuses the production menu/pause routes and adds an explicit chapter completion surface. |
| `docs/10-production-scope.md` | MVP: one crime scene, three evidence, one reconstruction, one objective chain | The Chapter 1 slice is the MVP content unit; no Adrian house segment is added. |
| `docs/ARCHITECTURE.md` | One composition root per scene; no scene search | `Tkp1` gets one `GameLifetimeScope` with explicit `presenterRoots`; no new `Find*`. |

## Performance Implications

- **CPU**: The copied rig adds one player, one inspection camera, HUD canvases, and existing services; no new per-frame allocation is planned in the added presenters.
- **Memory**: One additional gameplay scene in the build; `PunyaPandu` remains for regression until promotion.
- **Load Time**: One scene transition from `MainMenu`; same single-scene loading pattern as the current prototype.
- **Network**: N/A.

## Migration Plan

1. Create the ADR and record the boot-promotion gate in `docs/DECISIONS.md`.
2. Move the verified gameplay rig and B1 interaction objects from `PunyaPandu` into `Tkp1` through Unity MCP, preserving GUIDs and serialized references.
3. Add `ChapterEntryPresenter` and `ChapterCompletionPresenter`; fix overlap-safe save and active-scene guards.
4. Build the Chapter 1 crime-scene layout using the existing Tkp1 environment and available assets.
5. Run EditMode tests, Play the full Chapter 1 loop, and capture console/screenshot/state evidence.
6. Only after all checks pass, update `MainMenuPresenter.firstSceneName` to `Tkp1` and set `Tkp1` to build index 1, moving `PunyaPandu` to index 2.
7. Update ADR-0027's related-decision note and mark this ADR `Accepted`.

## Validation Criteria

- `Tkp1` has one `GameLifetimeScope`, one player, one EventSystem, one inspection rig, and wired presentation roots; missing references = 0.
- `M01_Alley` starts through `ChapterEntryPresenter`, not `MissionHudPresenter.Start`.
- Body, scratch, and blood evidence each open inspection, expose at least one valid hotspot, and add the correct evidence to the Evidence Log.
- Reconstruction `case_b1` can be opened, sees the three evidence dependencies, and completes only on `opt_panic`.
- Completing the reconstruction publishes `Act1ChapterCompletedEvent`, shows the chapter-end card, and returns to `MainMenu` without a second completion event.
- Death reload and pause “Muat Checkpoint” either restore a valid `Tkp1` chapter save or leave the player safely in place; they never apply a snapshot from another scene.
- `MainMenu` → `Tkp1` works only after the final promotion check; before promotion, `MainMenu` still loads `PunyaPandu`.
- Compilation 0 errors; Play Mode console 0 errors/exceptions; no new `PATTERN-001` matches.
- EditMode suite passes with the known baseline failure either fixed or explicitly reported as pre-existing.

## Related Decisions

- ADR-0027 — production `MainMenu` boot and menu-scoped new-game reset.
- ADR-0009 — explicit `presenterRoots`; no scene-search injection.
- ADR-0018 — `GameLifetimeScope` composition-root exception.
- ADR-0008 / ADR-0012 / ADR-0022 — chapter save vs house checkpoints, anti-soft-lock recovery, save snapshot v4.
- `docs/05-chapter-structure.md`, `docs/06-level-design.md`, `docs/07-ui-ux.md`, `docs/10-production-scope.md`.
