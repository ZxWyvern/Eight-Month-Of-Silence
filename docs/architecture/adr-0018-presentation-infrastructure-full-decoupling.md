# ADR-0018: Presentation → Infrastructure Full Decoupling (Remove asmdef Reference)

## Status
Accepted

## Implemented
2026-09-19 — code + scene migrated, `play` verify PASS, `ARCH-004` now PASS for gameplay code (only `GameLifetimeScope` + Editor tooling still refs Infrastructure as allowed exception). `Yumiwa.Presentation` no longer needs direct `ItemData` for 5 gameplay files; `WorldInspectableItem` now `itemId` + `IConfigProvider` + legacy `data` Object fallback migrated to `itemId` strings and removed from scene YAML.

## Date
2026-09-19

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.8f1 (6000.3 LTS) + VContainer 1.19.0 + URP 17.3.0 |
| **Domain** | Core / DI + Configuration |
| **Knowledge Risk** | LOW — VContainer `Register`/`Resolve`, asmdef `references` stable since 2018; `ScriptableObject` → `Domain` struct mapping stable |
| **References Consulted** | `docs/ARCHITECTURE.md:1` dependency direction `Presentation → Application → Domain` + `Infrastructure implements`, `agent/rules.json:ARCH-004`, `Yumiwa.Presentation.asmdef:4` refs `Infrastructure`, `Yumiwa.Domain.asmdef:13` `noEngineReferences:true`, `Presentation/IInspectable.cs:1` `ItemData`, `Infrastructure/ItemData.cs:21`, `Domain/ItemDefinition.cs:10` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | `Yumiwa.Domain` still 0 `using UnityEngine`; `Yumiwa.Presentation.asmdef` `references` no longer contains `Yumiwa.Infrastructure` (only `Domain,Application,VContainer,InputSystem`); `presenterRoots:19` still injects OK; `play PunyaPandu` 6 world items still `available` |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (layering) `Accepted`, ADR-0011 (deferred SIZE-001) `Accepted`, ADR-0017 (minimal hygiene) `Accepted` |
| **Enables** | ADR-0011 Opsi B `SIZE-001` split `PuzzleView`/`InventoryPresenter` (clean after decoupling), future `FinalEncounterService` composition without concrete leak |
| **Blocks** | `SIZE-001` full split `PuzzleView 439 → PanelFactory` — should not start until this is Accepted (otherwise PanelFactory will re-introduce `ItemData` leak) |
| **Ordering Note** | Must be Accepted before `gate-check Production→Polish` can mark `ARCH-004` as PASS; implementation is breaking (compile) so do in dedicated branch |

## Context

### Problem Statement
`production/report.md:50` + `production/gdd-code-audit-2026-09-17.md` `M10` still `ARCH-004 FAIL major`: `Presentation/Yumiwa.Presentation.asmdef:4` lists `Yumiwa.Infrastructure` as direct reference. This allows 5 concrete leaks beyond `GameLifetimeScope` factory:
- `Presentation/IInspectable.cs:1` `using Yumiwa.Infrastructure; ItemData Data {get;}`
- `Presentation/ItemInspectionPresenter.cs:3` `Show(ItemData)`
- `Presentation/WorldInspectableItem.cs:4` `ItemData data`
- `Presentation/ItemInspectionRotator.cs:4` `InspectionConfig` via `Infrastructure`
- `Presentation/CensorEffectPresenter.cs:5` `CensorEffectConfig`
`ADR-0017` did minimal hygiene (guarded `Resources`, fixed `wounded`) but explicitly **kept** the direct ref as `Accepted` exception. For long-term team maintainability ("paling enak untuk kedepan" — 4 orang mixed seniority), this exception must be closed: `Domain` must not be bypassed, `Infrastructure` must be behind interfaces defined in `Domain/Application` and resolved only at `GameLifetimeScope`.

### Constraints
- Technical: `Domain noEngineReferences:true` must stay; `Infrastructure.ItemData : ScriptableObject` cannot be moved to `Domain` (has `UnityEngine`).
- Timeline: 0.5d dedicated refactor branch — touches 8 files + 1 asmdef + `GameLifetimeScope` — must be atomic to avoid half-leak compile error.
- Compatibility: Existing `ItemData` assets (`Brass Key`, `Crowbar`, `Pistol`, `Letter`, `Watch`, `Flashlight`) must not be re-authored; conversion via `ToDefinition()` stays.
- No behavior change: `WorldInspectableItem.Consume()` → pooled `SetActive` + `IRecoveryPoint` must still work.

### Requirements
- `Yumiwa.Presentation.asmdef` `references` must be `[Yumiwa.Domain, Yumiwa.Application, Unity.InputSystem, VContainer]` — **no** `Yumiwa.Infrastructure`.
- `IInspectable` must expose `Domain.ItemDefinition` + `Domain.Evidence` + `InspectionHotspot[]` or `IConfigProvider` lookup, not `Infrastructure.ItemData`.
- `WaterZone` must be `Construct(IWaterProvider)` not `WaterSensor` concrete.
- `GameLifetimeScope` remains the **only** place that `using Yumiwa.Infrastructure` and `new ItemData.ToDefinition()` mapping.

## Decision
Perform **full** `ARCH-004` decoupling in one atomic change (future branch `refactor/adr-0018-decoupling`):

1. **Domain** (no UnityEngine already):
   - Keep `Domain/ItemDefinition.cs:10` `readonly struct ItemDefinition` + `ItemKind` + `Evidence` + `InspectionHotspot` as pure data transfer objects.
   - Add `Domain/IItemRegistry` (or reuse `IConfigProvider`) with `ItemDefinition GetItem(string itemId)` for Presentation lookup by id.

2. **Application**:
   - Extend `IConfigProvider` (`Domain/IConfigProvider.cs:7`) with `ItemDefinition GetItemDefinition(string itemId)` and `PuzzleDefinition GetPuzzle(string id)` backed by `Infrastructure/DirectConfigProvider`.
   - `PhysicalInventoryService` + `InspectionSessionService` already work on `ItemDefinition` — no change.

3. **Infrastructure** (stays concrete):
   - `Infrastructure/ItemData.cs:21` stays `ScriptableObject` with `public ItemKind Kind = PhysicalTool` + `public InspectionHotspotData[] Hotspots` + `public ItemDefinition ToDefinition()` + `public Evidence ToEvidence()` + `public InspectionHotspot[] ToHotspots()` — **only** called from `GameLifetimeScope` or `DirectConfigProvider`.

4. **Presentation** (breaking):
   - `Presentation/IInspectable.cs:1` change `using Yumiwa.Infrastructure; ItemData Data` → `using Yumiwa.Domain; ItemDefinition Definition {get;} Evidence LinkedEvidence {get;} InspectionHotspot[] Hotspots {get;} bool IsAvailable {get;} void Consume();`
   - `Presentation/WorldInspectableItem.cs:4` change `SerializeField ItemData data` → `SerializeField string itemId` + `[Inject] Construct(IConfigProvider,IRecoveryPoint,IEventBus)` → `Definition = config.GetItemDefinition(itemId)` on `Awake/Construct`; keep `Consume()` pooled `SetActive(false)`.
   - `Presentation/ItemInspectionPresenter.cs:3` `Show(ItemData)` → `Show(ItemDefinition, Evidence, InspectionHotspot[])` or `Show(IInspectable)`.
   - `Presentation/CensorEffectPresenter.cs:5` `using Yumiwa.Infrastructure CensorEffectConfig` → `Domain.CensorEffectData` via `IConfigProvider.GetCensor()`.
   - `Presentation/Yumiwa.Presentation.asmdef:4` remove `"Yumiwa.Infrastructure"` from `references`.
   - `Presentation/GameLifetimeScope.cs:8` is **exception** — it keeps `using Yumiwa.Infrastructure` to map `ItemData asset → ItemDefinition` at startup and register `IConfigProvider` singleton via `DirectConfigProvider(MovementConfig,StaminaConfig)`.

Migration is compile-breaking by design — no `ItemData` literal in `Presentation` will compile after step 4, forcing completion.

### Architecture Diagram
```
Before (exception):
Presentation --concrete--> Infrastructure.ItemData (via asmdef ref)
  IInspectable.Data : ItemData
  WorldInspectableItem.data : ItemData
  Presentation.asmdef refs [Domain, Application, Infrastructure]

After (this ADR):
Presentation --interface--> Domain.ItemDefinition / IConfigProvider.GetItemDefinition(id)
                              ^                         |
                              |                         | implements
Domain <-- Infrastructure.ItemData.ToDefinition() --- Infrastructure.DirectConfigProvider
                              |
                      GameLifetimeScope (only place with using Infrastructure, does mapping)
  IInspectable.Definition : ItemDefinition (pure)
  WorldInspectableItem.itemId : string + IConfigProvider lookup
  Presentation.asmdef refs [Domain, Application] only
```

### Key Interfaces
```csharp
// Domain (new on IConfigProvider)
public interface IConfigProvider {
  MovementConfigData GetMovement();
  StaminaConfigData GetStamina();
  ItemDefinition GetItemDefinition(string itemId); // NEW
  PuzzleDefinition GetPuzzle(string puzzleId);     // NEW via PuzzleDefinitionAsset
}

// Presentation (breaking)
public interface IInspectable {
  ItemDefinition Definition { get; }
  Evidence LinkedEvidence { get; }
  InspectionHotspot[] Hotspots { get; }
  bool IsAvailable { get; }
  void Consume();
}

// Infrastructure (unchanged but now only called from GameLifetimeScope/DirectConfigProvider)
public sealed class ItemData : ScriptableObject {
  public ItemDefinition ToDefinition();
  public Evidence ToEvidence();
  public InspectionHotspot[] ToHotspots();
}
```

## Alternatives Considered

### Alternative 1: Keep exception forever (status quo ADR-0017)
- **Description**: Leave `Presentation.asmdef` refs `Infrastructure`, keep `IInspectable ItemData` concrete, document as `Accepted exception: allowed only via GameLifetimeScope`.
- **Pros**: Zero churn, `play` already PASS, no compile break.
- **Cons**: Every new feature will re-copy `using Yumiwa.Infrastructure` into `Presentation` (already 5 files) — debt spreads, violates `Clean Architecture` one-way inward, makes `Domain noEngineReferences` pointless.
- **Rejection Reason**: Not "paling enak" for 4-person team — debt compounds, future `FinalEncounterService` will also leak concrete.

### Alternative 2: Move ItemData to Domain by removing ScriptableObject (bad)
- **Description**: Create `Domain.ItemData` pure struct and make `Presentation` use it directly, delete `Infrastructure.ItemData`.
- **Pros**: Decoupling without `IConfigProvider`.
- **Cons**: Loses Unity `ScriptableObject` asset workflow (designers can't author `Brass Key` in inspector), violates `DATA-001` data-driven `ScriptableObject` rule.
- **Rejection Reason**: Breaks GDD data-driven requirement `docs/09:9`.

## Consequences

### Positive
- `ARCH-004` lint `PASS` — `Yumiwa.Presentation` no longer references concrete `Infrastructure`, `Infrastructure` truly behind interfaces at composition root.
- Team can add new `WorldInspectableItem` by `itemId` string + `ScriptableObject` asset without touching `Presentation` imports — enak for junior.
- Enables `SIZE-001` split cleanly (new `PanelFactory` won't reintroduce `ItemData`).

### Negative
- Atomic breaking change — 8 files + asmdef must change together, PR cannot be split without compile fail. Needs dedicated `refactor/adr-0018-decoupling` branch.
- `WorldInspectableItem` changes Inspector field from `ItemData` object ref to `string itemId` — existing 6 prefab refs (`Evidence_World_Pistol` etc) must be re-wired via `GameLifetimeScope` lookup or migration script.

### Risks
- **Inspector churn**: `WorldInspectableItem.data` → `itemId` breaks prefab serialization (lost ref). Mitigation: migration editor script `ItemDataMigrator` that on `Awake` reads old `data` if `itemId` empty, writes `itemId = data.name`, then clears `data` — run once via `unity_unity_execute_code`.
- **Asmdef circular**: If `IConfigProvider.GetItemDefinition` is in `Domain` but impl needs `Infrastructure.ItemData`, `Infrastructure` must ref `Domain` (already) — no cycle, but verify `Yumiwa.Infrastructure.asmdef` refs `Domain` not `Presentation`.
- **Test break**: `Application.Tests` that construct `ItemDefinition` directly still OK (pure), but tests that used `DirectConfigProvider` with `ItemData` assets need fake `IConfigProvider` stub — update `Application/Tests/Fakes.cs`.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `ARCHITECTURE.md:1` dependency direction | `Infrastructure` implements `Domain` interfaces, `Presentation` may inject `Infrastructure` only at composition root | Removes direct `Presentation→Infrastructure` concrete ref, keeps only `GameLifetimeScope` as allowed root |
| `ARCHITECTURE.md:7` Configuration via ScriptableObjects | Mission/item/puzzle are data assets, not code | `ItemData.ToDefinition()` mapping stays in `Infrastructure`, `Presentation` only sees `ItemDefinition` |
| `GDD §03 Aturan Evidence` | `Critical` → log not slot, `PhysicalTool` → 6 slots | `InspectionSessionService` already `ItemDefinition`-based, decoupling preserves routing |

## Performance Implications
- **CPU**: `IConfigProvider.GetItemDefinition(id)` is dictionary lookup `O(1)` at `Awake` — once per `WorldInspectableItem`, not per frame.
- **Memory**: `ItemDefinition` struct ~32 bytes vs `ItemData ScriptableObject` ref 8 bytes — pooled array `GetSlots` unchanged.
- **Load Time**: `DirectConfigProvider` preloads `ItemData[]` via `Resources`/`Addressables` once at `GameLifetimeScope` — no Addressables yet (still `DirectConfigProvider` sync), future `AddressableAssetLoader` can replace without `Presentation` change.

## Migration Plan
1. Branch `refactor/adr-0018-decoupling` from `Production`.
2. Edit `Domain/IConfigProvider.cs` add `GetItemDefinition/Puzzle` signatures.
3. Edit `Infrastructure/DirectConfigProvider.cs` implement lookup via `ItemData[]` injected from `GameLifetimeScope` (serialize `List<ItemData> allItems` there).
4. Edit `Presentation/IInspectable.cs`, `WorldInspectableItem.cs`, `ItemInspectionPresenter.cs`, `ItemInspectionRotator.cs`, `CensorEffectPresenter.cs` to use `Domain` types only.
5. Edit `Yumiwa.Presentation.asmdef` remove `Yumiwa.Infrastructure` from `references`.
6. `GameLifetimeScope.cs` keep `using Yumiwa.Infrastructure` + add migration for `itemId` wiring (or manual reassign via `unity_unity_component_set_property` with `itemId` strings `brass_key, crowbar, pistol, letter_adrian, watch_bloody, flashlight`).
7. Verify `unity_unity_get_compilation_errors count 0` + `play PunyaPandu` 6 items `IsAvailable` + `Take` + `DropWithRecovery` + `HasCheckpoint` still PASS.

## Validation Criteria
- `grep "using Yumiwa.Infrastructure" Presentation/*.cs` = 0 outside `GameLifetimeScope.cs` (1 hit allowed).
- `cat Yumiwa.Presentation.asmdef` `references` does not contain `Yumiwa.Infrastructure`.
- `Domain/Yumiwa.Domain.asmdef:13` still `noEngineReferences:true` and `grep "using UnityEngine" Domain/*.cs` = 0.
- `play PunyaPandu` `IsInHand` + `GetHandItemId 0B` + `Evidence` + `Puzzle` + `Trap` + `Checkpoint` same as `2026-09-19` sweep.

## Related Decisions
- ADR-0001 (layering) — this closes its single exception.
- ADR-0011/0017 (deferred guard) — this is the full follow-up.
- ADR-0016 (save) — unrelated, but same hygiene wave.
- `production/report.md:50` `ARCH-004` table — will be `PASS` after this.
