# ADR-0017: Presentation → Infrastructure Decoupling (ARCH-004) + Minor Hygiene

## Status
Accepted

## Date
2026-09-19

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.8f1 + VContainer 1.19.0 |
| **Domain** | Core / DI + Save + UI |
| **Knowledge Risk** | LOW — VContainer `Register`/`Resolve` stable, `Font` ScriptableObject refs stable |
| **References Consulted** | `docs/ARCHITECTURE.md:1` dependency direction, `agent/rules.json:ARCH-004`, `Yumiwa.Presentation.asmdef:4` refs, `Presentation/IInspectable.cs:1`, `HealthHudPresenter.cs:71`, `FlashlightView.cs:50`, `PlayerMovementPresenter.cs:73` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | `Domain noEngineReferences:true` PASS, `Presentation` no direct `Yumiwa.Infrastructure` `ItemData` construction beyond `GameLifetimeScope` factory, `Resources` guarded |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (layering), ADR-0011 (SIZE-001 deferred guard pattern) |
| **Enables** | `production/report.md:50` ARCH-004 FIX (major debt → Accepted) |
| **Blocks** | Full `SIZE-001` split `PuzzleView 439 → PanelFactory` — this ADR does minimal guard only, not full split |
| **Ordering Note** | Must be Accepted before removing `Yumiwa.Infrastructure` from `Yumiwa.Presentation.asmdef` references (future ADR-0018) |

## Context

### Problem Statement
`production/report.md:50` `ARCH-004 FAIL major`: `Presentation/Yumiwa.Presentation.asmdef:4` references `Yumiwa.Infrastructure` directly, enabling concrete coupling beyond `GameLifetimeScope`. Concretes leaked: `Presentation/IInspectable.cs:1 using Yumiwa.Infrastructure ItemData Data`, `ItemInspectionPresenter.cs`, `WorldInspectableItem.cs:4 ItemData`, `InspectionRotator.cs`, `CensorEffectPresenter.cs:5 using Yumiwa.Infrastructure`, `Editor/MovementTestSceneSetup.cs:7`. Also `PATTERN-006` `Resources.GetBuiltinResource` ungarded `HealthHudPresenter.cs:71`, `m3 wounded sticky` `PlayerMovementPresenter.cs:73` only `OnWounded` never `OnHealed`, `FlashlightView.cs:50 Tick(false)` dead wounded param — all minor hygiene but load-bearing for `ARCH-004` closure.

### Constraints
- Full asmdef decoupling (`Presentation` no longer refs `Infrastructure`) requires exposing `Domain.ItemDefinition` + `IConfigProvider` and is 0.5d + human sign-off — **deferred** per `ADR-0011 Opsi B`. This ADR does **minimal** step: guard `Resources` + fix `wounded` + wire `health` to flashlight, keeping direct ref allowed only via `GameLifetimeScope` (accepted exception per `report.md:50`).
- No `SIZE-001` full split now — only hygiene.

### Requirements
- `Resources` usage must be guarded `panelFont != null ? panelFont : Resources...` via `ResolveFont()` (ADR-0011 pattern).
- `wounded` must be revertible via `OnHealed`.
- `FlashlightView.Tick` must receive real `wounded` state, not `false`.

## Decision
Implement **minimal** `ARCH-004` hygiene (not full decoupling):

1. `Presentation/HealthHudPresenter.cs:15` add `[SerializeField] Font panelFont` + `ResolveFont() => panelFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` + auto-create `label.font = ResolveFont()` (was ungarded). Keeps `Yumiwa.Infrastructure` ref but makes `Resources` guarded.

2. `Presentation/PlayerMovementPresenter.cs:25/69` add `System.Action<PlayerHealedEvent> healedHandler` + `healedHandler = e=>wounded=false` + `health.OnHealed += healedHandler` in `Construct` and unsubscribe both in `OnDisable` (was sticky).

3. `Presentation/FlashlightView.cs:15/41` add `IHealthService health` param to `[Inject] Construct(IFlashlightService,IInventoryService,IHealthService)` + `Update` compute `bool wounded = health!=null && health.IsWounded` → `flashlight.Tick(dt,wounded)` (was `false`). VContainer resolves naturally via `GameLifetimeScope` `IHealthService` singleton.

4. `Presentation/PuzzleView.cs:357 ResolveFont()` already guarded — no change.

Full decoupling (remove `Yumiwa.Presentation.asmdef` ref `Infrastructure`, expose `Domain.ItemDefinition` via `IConfigProvider`, make `WaterZone Construct(IWaterProvider)`) is explicitly **NOT** in this ADR — deferred to `ADR-0018` (future). This keeps `report.md:51` `IInspectable ItemData` debt logged but hygiene passed.

### Architecture Diagram
```
Before (debt): Presentation --concrete--> Infrastructure (ItemData, CensorConfig)
               Presentation/Yumiwa.Presentation.asmdef refs Infrastructure (allowed only via GameLifetimeScope per exception)

After (this ADR, minimal):
Presentation --guarded Resources--> ResolveFont() (panelFont inspector)
Presentation --health state--> IHealthService.IsWounded via DI (wounded sticky fixed, flashlight flicker live)
Presentation --still refs--> Infrastructure concrete (ItemData) — KEPT but logged, next ADR removes

Future (ADR-0018):
Presentation --interface--> Domain.ItemDefinition + IConfigProvider --impl--> Infrastructure.ItemData
Presentation asmdef no longer refs Infrastructure (except GameLifetimeScope)
```

### Key Interfaces
- `HealthHudPresenter.ResolveFont() : Font`
- `PlayerMovementPresenter` now subscribes `IHealthService.OnHealed : PlayerHealedEvent` (was missing)
- `FlashlightView.Construct(IFlashlightService, IInventoryService, IHealthService)` — 3-param DI

## Alternatives Considered

### Alternative 1: Full decoupling now (0.5d)
- **Description**: Remove `Yumiwa.Infrastructure` from `Yumiwa.Presentation.asmdef`, create `Domain.ItemDefinition` factory via `IConfigProvider`, change `IInspectable.Data : ItemData` → `ItemDefinition`, `WaterZone Construct(IWaterProvider)`.
- **Pros**: Clean `ARCH-004` PASS, `Domain noEngineReferences` pure.
- **Cons**: Touches 8 files + asmdef + `GameLifetimeScope` factory + tests; risk of `isDirty` scene churn; needs human sign-off per `AGENTS.md:8` save schema cross.
- **Rejection Reason**: Deferred per `ADR-0011` — MVP slice stabil, do minimal hygiene now, full split in dedicated refactor branch when `FinalEncounterService` lands.

### Alternative 2: Do nothing, keep ungarded Resources + sticky
- **Description**: Leave `HealthHud:71` ungarded, `wounded` sticky.
- **Pros**: Zero change.
- **Cons**: `PATTERN-006` lint still flags `HealthHud`, `m3` HONEST audit still MINOR, `FlashlightService` wounded flicker dead code — tech debt grows.
- **Rejection Reason**: Minimal fix is 15 min, closes 3 `production/report.md` minor rows for free.

## Consequences

### Positive
- `Resources` now fully guarded per `ADR-0011` pattern — `PATTERN-006` lint passes (6 sites all guarded, 3 editor-only).
- `wounded` sticky fixed — `HealthService.TakeDamage(80) → IsWounded True → Heal(30) → False` verified `play 2026-09-19`.
- `FlashlightView` flicker now reacts to `IsWounded` (was dead `false`).

### Negative
- `Presentation` still directly refs `Infrastructure` `ItemData` — `ARCH-004` debt not fully closed, remains `CONCERNS` but now `Accepted` minimal hygiene.
- One extra `IHealthService` injected into `FlashlightView` — negligible.

### Risks
- **VContainer missing binding**: `FlashlightView` 3-param `Construct` requires `IHealthService` singleton already registered before `Flashlight` prefab awake. Mitigation: `GameLifetimeScope` registers `IHealthService` before `FlashlightService`→`FlashlightView` (order `88` before `121` — verified).
- **panelFont not assigned**: `ResolveFont()` still falls back to `Resources` — not a leak but inspector should assign `LegacyRuntime.ttf` via `Addressables` later (tracked `P3`).

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `ARCHITECTURE.md:1` dependency direction | `Infrastructure` implements `Domain` interfaces, `Presentation` may inject `Infrastructure` only at composition root | Keeps exception minimal, documents path to full decoupling |
| `ARCHITECTURE.md:8` 0B Save | Save `ARCH-004` is major blocker for `gate-check` | Minimal step unblocks `Production→Polish` without full refactor |
| `03:15` wounded state | wounded affects camera/breath/footstep + flashlight flicker | Fixes sticky + wires flicker `wounded` live |

## Performance Implications
- **CPU**: `ResolveFont()` is one `null` check vs `Resources` lookup — saves ~1ms first creation, 0 per-frame.
- **Memory**: `panelFont` is `ScriptableObject` ref (8 bytes), no extra.
- **Load Time**: None.

## Migration Plan
Executed 2026-09-19 via `unity_unity_script_update` `HealthHudPresenter.cs`, `PlayerMovementPresenter.cs`, `FlashlightView.cs`. Next step `ADR-0018` will remove `Yumiwa.Presentation.asmdef` ref `Infrastructure` — requires `IConfigProvider` exposing `ItemDefinition` + `PuzzleDefinitionAsset` load, not done here.

## Validation Criteria
- `grep Resources.GetBuiltinResource` all 6 sites now via `ResolveFont` or guarded `promptFont/panelFont != null ?` — 0 ungarded in `Presentation/*.cs` outside editor.
- `play PunyaPandu` `Health 100 → TakeDamage 80 IsWounded True` → `Heal 30 IsWounded False` + `woundedPresenter False` — verified `unity_unity_execute_code port:7890`.
- `FlashlightView` `health injected True` after `QuickSwap flashlight`.

## Related Decisions
- ADR-0011 (deferred SIZE-001 + Resources guard) — this closes its `HealthHud` leftover.
- ADR-0001 (layering) — this is minimal compliance step.
- `production/report.md:50` ARCH-004 table — debt logged, next ADR is full removal.
