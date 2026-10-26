# ADR-0015: Build Canonical — PunyaPandu as buildIndex 0

## Status
Accepted

## Date
2026-09-19

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.8f1 (6000.3 LTS) + URP 17.3.0 |
| **Domain** | Core / Build |
| **Knowledge Risk** | LOW — EditorBuildSettings API stable since 5.x, no post-cutoff change |
| **References Consulted** | `ProjectSettings/EditorBuildSettings.asset`, `UnityEditor.EditorBuildSettings.scenes` docs, `docs/ARCHITECTURE.md:2` (VContainer LifetimeScope per scene), `docs/10-production-scope.md: MVP` |
| **Post-Cutoff APIs Used** | None — `EditorBuildSettingsScene(string,bool)` + `EditorBuildSettings.scenes` setter |
| **Verification Required** | Build `StandaloneWindows64` produces `PunyaPandu` first window; `SampleScene` still loads via index 1 if needed for tests |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (MissionSequence data) — no, independent; ADR-0009 (presenterRoots explicit) |
| **Enables** | Gate `Production→Polish` — SHIP not blank per `production/report.md:19` P0 |
| **Blocks** | Build verification / QA smoke test — cannot start until canonical fixed |
| **Ordering Note** | Must be Accepted before `gate-check` Polish; no code dependency on other ADRs |

## Context

### Problem Statement
`EditorBuildSettings.asset` had `m_Scenes: [SampleScene, PunyaPandu]` with `SampleScene` at `buildIndex 0`. Player launching built `SHIP` lands in empty `SampleScene` (blank, 12KB from Jan 2025), not the playable `PunyaPandu.unity` (65 objek, 218KB, 6 world items). Violates `docs/10` MVP (1 crime scene completable) and `production/report.md` Build Hygiene blocker. `production/project-stage-report.md:30 #1` flagged this as P0.

### Constraints
- Technical: `EditorBuildSettings.scenes` order is source of truth for `SceneManager.LoadScene(0)` and build output; no additive loading for MVP.
- Timeline: 0.5h fix, no scene content change, only ordering.
- Compatibility: Keep `SampleScene` in build for legacy tests but not as entry point; do not delete asset yet to avoid guid churn.
- No circular asmdef, no code change — only project settings asset.

### Requirements
- `PunyaPandu` must be `buildIndex 0` `enabled:1`.
- `SampleScene` stays `buildIndex 1` `enabled:1` for now (future removal via separate ADR).
- Must be verifiable via `EditorBuildSettings.scenes` read + file `ProjectSettings/EditorBuildSettings.asset`.

## Decision
Reorder `EditorBuildSettings.scenes` to `[PunyaPandu, SampleScene]` both `enabled:1`, with `PunyaPandu` guid `2c8ee7187fd8af94eb3e22ab862e01cc` first. Implemented via `UnityEditor.EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/PunyaPandu.unity", true), new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true) }` + `AssetDatabase.SaveAssets()`. Verified via `unity_unity_execute_code port:7890` readback `After 2 scenes: [0] PunyaPandu buildIndex=0, [1] SampleScene buildIndex=1`.

### Architecture Diagram
```
Build (StandaloneWindows64)
  [0] PunyaPandu.unity (canonical, 65 objek, LifetimeScope presenterRoots 19, Mission M01_Alley)
  [1] SampleScene.unity (legacy, 12KB, kept for tests)
  LoadScene(0) → PunyaPandu → GameScope → MissionFlowService M01..M05
```

### Key Interfaces
- No new code interface. Project setting interface: `UnityEditor.EditorBuildSettings.scenes : EditorBuildSettingsScene[]` where `EditorBuildSettingsScene(string path, bool enabled)` + `AssetDatabase.SaveAssets()`.

## Alternatives Considered

### Alternative 1: Remove SampleScene from build entirely
- **Description**: `m_Scenes: [PunyaPandu only]`.
- **Pros**: Single-scene build, no confusion, smallest build.
- **Cons**: Breaks any CI referencing `buildIndex 1` for `YumiwaMovementTest` legacy; removes fallback for tests that load SampleScene.
- **Rejection Reason**: Keep SampleScene for now; removal can be ADR-0015 superseded later when tests migrated.

### Alternative 2: Keep SampleScene first but add bootstrap loader scene
- **Description**: Create `Bootstrap.unity` at index 0 that `LoadSceneAsync(PunyaPandu)`.
- **Pros**: Explicit bootstrap per `docs/ARCHITECTURE.md:2` persistent scope.
- **Cons**: Adds extra scene, extra load time, extra LifetimeScope complexity for MVP; not needed for 2-scene slice.
- **Rejection Reason**: Over-engineering for MVP; simple reorder suffices per `KISS`.

## Consequences

### Positive
- SHIP launches directly into playable `PunyaPandu` — `production/report.md` Build Hygiene blocker resolved, `gate-check Production→Polish` can PASS.
- Minimal change (only ordering, no content), zero risk to gameplay code.

### Negative
- Two scenes in build still ships `SampleScene` bytes (12KB negligible but clutter).
- Developers must remember `buildIndex 0` is canonical; future scene add needs ordering discipline.

### Risks
- **Wrong order regression**: Someone re-adds `SampleScene` first via UI. Mitigation: CI lint `EditorBuildSettings` first entry must be `PunyaPandu` (add to `agent/rules.json` future `BUILD-001`).
- **Guid mismatch**: If `PunyaPandu.unity` guid changes (move/rename) build breaks. Mitigation: never rename without updating `EditorBuildSettings` via MCP.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `10-production-scope.md` MVP | 1 level crime scene completable | SHIP now boots that level, not blank |
| `ARCHITECTURE.md:2` Composition root | LifetimeScope per scene | `PunyaPandu` root is the single entry, no cross-scene find |

## Performance Implications
- **CPU**: None.
- **Memory**: +12KB `SampleScene` still in build (negligible).
- **Load Time**: -1 scene load vs bootstrap loader (faster).
- **Network**: N/A.

## Migration Plan
Already executed `2026-09-19` via `unity_unity_execute_code port:7890`. No code migration. If later removing `SampleScene`, set `m_Scenes: [PunyaPandu only]` via same API and bump this ADR to `Superseded by ADR-001X`.

## Validation Criteria
- `ProjectSettings/EditorBuildSettings.asset: m_Scenes[0].path == Assets/Scenes/PunyaPandu.unity` and `enabled:1`.
- `unity_unity_execute_code` readback `buildIndex 0 PunyaPandu`.
- Built player (if executed) shows `Mission: M01_Alley` on launch, not blank.
- `unity_unity_get_compilation_errors count 0`, `sceneDirty false` after save.

## Related Decisions
- ADR-0003 (MissionSequence data) — PunyaPandu is the scene that hosts `MissionSequence.asset` M01..M05.
- ADR-0009 (presenterRoots explicit) — canonical scene has 19 roots.
- `production/report.md:19` Fix Status 2026-09-19 P0 Build.
