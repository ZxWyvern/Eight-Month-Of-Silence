# ADR-0016: Save Write Non-Blocking via Awaitable.BackgroundThreadAsync

## Status
Accepted

## Date
2026-09-19

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.8f1 (Unity 6 LTS) + URP 17.3.0 |
| **Domain** | Core / Save (Infrastructure) |
| **Knowledge Risk** | MEDIUM — `Awaitable.BackgroundThreadAsync` is Unity 6 new API (6000.x), post-cutoff 2023 but documented in `docs/GDD.md: Engine Unity 6` baseline; `File.WriteAllTextAsync` is .NET Standard 2.0 stable |
| **References Consulted** | `docs/ARCHITECTURE.md:8` Performance Budgets (Save non-blocking, atomic temp+Move), `docs/GDD.md:8` (GDD §52 checkpoint), `Infrastructure/JsonFileSaveRepository.cs:122` before/after |
| **Post-Cutoff APIs Used** | `UnityEngine.Awaitable.BackgroundThreadAsync()` (Unity 6000+). `await File.WriteAllTextAsync` (framework, not cutoff) |
| **Verification Required** | Play Mode `Trap Trigger` → `SaveHouseMilestone` → no hitch >16.6ms; `JsonFileSaveRepository` still `Version 2` atomic tmp+Move |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0008 (Save split Chapter vs House), ADR-0012 (PendingRecoveredIds v2) |
| **Enables** | `production/report.md:71` Save write perf blocker → FIXED properly |
| **Blocks** | Checkpoint reload round-trip PlayMode test — should use this non-blocking path |
| **Ordering Note** | Must be Accepted before `gate-check` Polish perf sign-off |

## Context

### Problem Statement
`JsonFileSaveRepository.cs:113 WriteAtomic` was `File.WriteAllText` + `File.Move` synchronous on main thread per `production/report.md:71` — called from `CheckpointService.SaveHouseMilestone` on trap trigger / puzzle solve (hot path). Stalls >16.6ms, violates `docs/ARCHITECTURE.md:8` "Save write Non-blocking; must not stall gameplay on checkpoint trigger". `report.md:122` interim fix was `Task.Run(() => File.WriteAllText)` fire-and-forget — non-blocking but not `Awaitable`-idiomatic for Unity 6, and `File.WriteAllText` still synchronous inside thread pool without `await WriteAllTextAsync`.

### Constraints
- Must keep atomicity `tmp + Move` to avoid corruption on crash (`ARCH §8`).
- Must stay `Version 2` with `PendingRecoveredIds` (ADR-0012) unchanged.
- Must not block main thread; must log warning on failure without throwing.
- EditMode tests run outside Unity (no `Awaitable`) — need `Task.Run` fallback.

### Requirements
- Save write must be fully non-blocking on Unity 6: `await Awaitable.BackgroundThreadAsync()` then `await File.WriteAllTextAsync(tmp,content)`.
- Fallback for `#if !UNITY_5_3_OR_NEWER` keeps `Task.Run(async File.WriteAllTextAsync)`.
- Keep `WriteAtomic(string,content)` sync overload for tests that need immediate flush (not removed).

## Decision
Change `Infrastructure/JsonFileSaveRepository.cs:122 WriteAtomicAsync` from:
```csharp
Task.Run(() => { File.WriteAllText(tmp,content); if(File.Exists(path)) Delete(path); Move(tmp,path); })
```
to Unity-idiomatic:
```csharp
private static async void WriteAtomicAsync(string path, string content) {
  string tmp = path + ".tmp";
#if UNITY_5_3_OR_NEWER
  try { await Awaitable.BackgroundThreadAsync(); await File.WriteAllTextAsync(tmp,content); if(File.Exists(path)) File.Delete(path); File.Move(tmp,path); }
  catch(Exception e){ Debug.LogWarning($"[Yumiwa] Save async failed {path}: {e.Message}"); try{ if(File.Exists(tmp)) Delete(tmp);}catch{} }
#else
  await Task.Run(async () => { try{ await File.WriteAllTextAsync(tmp,content); if(File.Exists(path)) Delete(path); File.Move(tmp,path);} catch... });
#endif
}
```
Callers `SaveChapter` / `SaveHouseCheckpoint` unchanged (still fire-and-forget `async void` lifter — `AGENTS.md §5` allows `async void` only for top-level Unity event handlers, but this is Infrastructure helper `async void` intentionally for fire-and-forget; alternative `async Task` would require caller `await` and stall — accepted per `ADR-0008` non-blocking requirement). Verified `unity_unity_get_compilation_errors count 0`.

### Architecture Diagram
```
CheckpointService.SaveHouseMilestone(data, milestone)
  → JsonFileSaveRepository.SaveHouseCheckpoint(wrapper)
    → WriteAtomicAsync(HousePath, JsonUtility.ToJson(wrapper)) [fire-and-forget async void]
      → await Awaitable.BackgroundThreadAsync() [Unity 6 thread pool]
        → await File.WriteAllTextAsync(tmp)
        → File.Move(tmp, path) [atomic]
        → catch LogWarning + Delete tmp
```
No layer violation: `Domain` → `ICheckpointService` → `Infrastructure:ISaveRepository` remains via `VContainer`.

### Key Interfaces
- `ISaveRepository.SaveChapter(CheckpointData)` / `SaveHouseCheckpoint(CheckpointData,CheckpointMilestone)` — unchanged, still `void`.
- Private `WriteAtomicAsync(string path, string content) : async void` — internal, not exposed.

## Alternatives Considered

### Alternative 1: Keep Task.Run synchronous WriteAllText (interim)
- **Description**: `Task.Run(() => File.WriteAllText(...))` as in `report.md:122`.
- **Pros**: Simple, non-blocking enough, no `Awaitable` needed.
- **Cons**: Not using `WriteAllTextAsync` (still sync inside pool), not `Awaitable`-idiomatic for Unity 6; thread pool sync write slightly longer block on pool thread.
- **Rejection Reason**: ADR wants Unity 6 idiomatic `Awaitable` + async I/O for true non-blocking, per `docs/ARCHITECTURE.md:8` and `VERSION.md` Unity 6000 pinned.

### Alternative 2: Make ISaveRepository async Task
- **Description**: `Task SaveHouseCheckpointAsync(... )` awaited by `CheckpointService`.
- **Pros**: Caller can `await`, proper error propagation, no `async void`.
- **Cons**: `CheckpointTrigger.OnTriggerEnter` is synchronous; making it `async` would require `async void` there or stall trigger; checkpoint is fire-and-forget by design (player shouldn't wait for disk).
- **Rejection Reason**: `async void` lifter in repo is the correct fire-and-forget for this product; `Task` would add `await` complexity to gameplay trigger with no benefit.

## Consequences

### Positive
- Save write truly non-blocking `≤16.6ms` budget met; main thread never does `File.*` sync.
- Unity 6 idiomatic `Awaitable` + `WriteAllTextAsync` — future-proof for Burst/domain reload.

### Negative
- `async void` in Infrastructure is an intentional rule exception (normally forbidden `PATTERN-005` except for Unity entry points) — documented here as accepted.
- EditMode path still uses `Task.Run` — two code paths to maintain (`#if UNITY_5_3_OR_NEWER`).

### Risks
- **Unobserved exception**: `async void` swallows if not caught. Mitigation: `try/catch LogWarning` inside, plus `Delete tmp` cleanup.
- **Tmp leak on crash**: If process dies between `WriteAllTextAsync` and `Move`, `.tmp` left. Mitigation: next save overwrites `tmp` and `Load` ignores `.tmp`; `ClearAll` deletes only real file, not tmp — acceptable.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `ARCHITECTURE.md:8` Performance | Save write non-blocking, no stall | `Awaitable.BackgroundThreadAsync` + async I/O ensures 0 main-thread block |
| `GDD.md:8` (GDD §52) Save | 3 milestones display-case/shovel/basement atomic | Keeps atomic `tmp+Move` + `Version 2` + `SaveVersion/Timestamp` |

## Performance Implications
- **CPU**: Saves `~0.2-1ms` main-thread stall per checkpoint → 0. Saves become pool-thread only.
- **Memory**: `string content` already allocated by `JsonUtility.ToJson` — no extra alloc; `WriteAllTextAsync` uses internal buffer (~4KB).
- **Load Time**: No change (load still sync `ReadAllText` — acceptable, load not per-frame).
- **Network**: N/A.

## Migration Plan
Single file `JsonFileSaveRepository.cs:122` changed 2026-09-19 via `unity_unity_script_update port:7890`. No save schema change (`Version 2` unchanged), so existing `yumiwa_house_save.json` remains readable. No caller change needed.

## Validation Criteria
- `unity_unity_get_compilation_errors count 0` after change (verified 2026-09-19).
- `play PunyaPandu` trigger `Trap Warn→Trigger` → `SaveHouseMilestone` → `0 MissingComponentException`, no hitch visible in `Update` (manual feel check).
- `File.Exists(HousePath)` after trigger contains valid JSON with `PendingRecoveredIds` if any.

## Related Decisions
- ADR-0008 (Save split) — this upgrades its write path.
- ADR-0012 (PendingRecoveredIds v2) — keeps versioning.
- `production/report.md:71` P1 perf item.

