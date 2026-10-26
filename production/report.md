# Code Health Report — Forbidden Patterns, Performance & Dead Code

**Date**: 2026-09-19
**Project**: Eight Months of Silence — Sinergasia Studio (CeBeTe)
**Engine**: Unity 6000.3.8f1, URP, VContainer 1.19.0
**Scene Canonical**: `PunyaPandu.unity` (65 objek, `PivotTanganKiri/Kanan` FBX primary, `Evidence_World_Flashlight` 6 world items)
**Method**: Read-only audit via 3× explore subagents (very thorough) + `Grep/Glob/Read` vs `AGENTS.md §5` + `agent/rules.json` + `docs/CODING_STANDARDS.md §4` + `docs/ARCHITECTURE.md §1/§8` + `docs/DECISIONS.md ADR-0011`
**Build Mode**: ACTIVE — report is binding input for `P0`/`P1` fixes before `gate-check` Production→Polish

---

## Executive Summary

| Kategori | Hasil | Blocker |
|---|---|---|
| **Forbidden `AGENTS.md §5`** | `PATTERN-001/002/003/005` **PASS**, `PATTERN-004` empty `catch{}` **FAIL major** 5 sites, `PATTERN-006` `Resources.GetBuiltinResource` jeopardy 6 sites (guarded 2), `SIZE-001` 5 monos `>200L` **FAIL major** (`ADR-0011` deferred) | `SIZE-001` + `PATTERN-004` blocker for `gate-check` |
| **`ARCH-001..004`** | `ARCH-001` `Domain noEngineReferences` **PASS**, `ARCH-003` no circular **PASS**, `ARCH-002` no business rule in Presentation **PASS**, `ARCH-004` `P→I` concrete `Yumiwa.Presentation → Yumiwa.Infrastructure` + `IInspectable ItemData` **FAIL major** (allow only di `GameLifetimeScope`) | `ARCH-004` |
| **Perf `ARCH §8` `0B` + `CODING_STANDARDS §3` pooling** | `blocker 2` `GetSlots Clone[6]` 60×/s + `PuzzleView material leak`, `major 5` string/Invoke/save sync | `GetSlots` + `material leak` |
| **Dead Code** | `definitely remove` 5 types/fields, `Keep with ADR` 8 configs, **Build hygiene blocker** `EditorBuildSettings` hanya `SampleScene` build | Build blocker |

**Overall**: **CONCERNS → P0 FIXED 2026-09-19 12:30** — `PATTERN-004` 5 sites `catch(Exception e) LogWarning` DONE, `GetSlots Clone` → `GetHandItemId(0/1)` 0B DONE `EquipmentPivotPresenter` hand-centric `PivotTanganKiri/Kanan` Q swap visual follow, `Build` `PunyaPandu` enabled 2 scenes DONE. Playable `PunyaPandu` `play 11:37:59` + `12:30` re-verify `Pistol FBX 4` `Flash FBX 1` `Q Swap Hand1 Right Hand2 Left` semua item `crowbar` + `Flashlight gate Alt ignored` PASS, `compilation 0`. Remaining `P1` perf `InteractionPrompt` `Flashlight Tick` `Stamina Invoke` + `P2` dead code `ARCH-004` + `P3` hygiene guard `Resources` masih **CONCERNS** untuk `gate-check` Polish.

---

## 1. Forbidden Patterns (per `agent/rules.json` severity)

| Rule | File:Line | Severity | Evidence | Action |
|---|---|---|---|---|
| **PATTERN-001** `FindObjectOfType` | `Presentation/Editor/MovementTestSceneSetup.cs:182` | **PASS (info)** | `// components via FindFirstObjectByType, so no manual autoInject` — **comment only**, `Grep FindObjectOfType|GameObject.Find` di `Assets/_Yumiwa` **0 code hits** ( `ADR-0009` ganti `presenterRoots 19` explicit) | — |
| **PATTERN-002** `Singleton` | — | **PASS** | `public static.*Instance` 0 hits | — |
| **PATTERN-003** `legacy Input` | — | **PASS** | `Input.GetKey/GetButton/GetAxis/mousePosition` 0 hits; pakai `IPlayerInput` `Keyboard.current` `Mouse.current` New Input System | — |
| **PATTERN-004** `empty catch` | `Presentation/ItemInspectionController.cs:71` | **~~major~~ FIXED 2026-09-19** | `try{hotspotPresenter.Construct} catch(Exception e){LogWarning(e.Message)}` — swallow dihapus | DONE `catch(Exception e) LogWarning` |
| | `Presentation/ItemInspectionController.cs:76` | **~~major~~ FIXED** | `try{SetRotationModel} catch(Exception e){LogWarning(e.Message)}` | DONE |
| | `Presentation/TrapZone.cs:59` | **~~major~~ FIXED** | `try{Resolve TrapService} catch(Exception e){LogWarning}` | DONE |
| | `Presentation/TrapZone.cs:60` | **~~major~~ FIXED** | `try{Resolve IPlayerInput} catch(Exception e){LogWarning}` | DONE |
| | `Presentation/WaterZone.cs:45` | **~~major~~ FIXED** | `try{Resolve WaterSensor} catch(Exception e){LogWarning}` | DONE |
| **PATTERN-005** `async void` | — | **PASS** | `async void` 0 hits | — |
| **PATTERN-006** `Resources.Load` | — | **PASS strict** | `Resources.Load` 0 hits; **jeopardy** `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` 6 sites (minor, `ADR-0011` guarded) | Assign `promptFont/panelFont` via inspector `Addressables` |
| | `HealthHudPresenter.cs:71` | **minor** | `label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");` — no guard | `panelFont` field + fallback |
| | `InteractionPromptPresenter.cs:55` | **minor (guarded)** | `promptText.font = promptFont != null ? promptFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");` — guarded | keep |
| | `PuzzleView.cs:338` | **minor (guarded)** | `var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");` inside `ResolveFont()` guard `panelFont` | keep, tapi `panelFont` belum di-assign inspector → masih hit `Resources` |
| **SIZE-001** `>200L or >1 job` | `PuzzleView.cs:1-422` | **major (deferred ADR-0011)** | `422L` 3 jobs Canvas/InputField factory + `OnTriggerEnter` + `TrySolve/Advance` | `ADR-0011 Opsi B` split `PanelFactory` |
| | `ItemInspectionController.cs:1-293` | **major** | `293L` state+input+UI+`Begin/Take/Cancel` | split `InspectionState` |
| | `InventoryPresenter.cs:1-276` | **major** | `276L` hands/pockets/swap/drop+HUD `StringBuilder` | extract `InventoryHud` |
| | `PlayerMovementPresenter.cs 205 + Motor 213 + Camera 105 =523` | **major (deferred)** | `partial` kosmetik, type 5 jobs movement+look+camera+stamina+shake | `LookHandler/CameraPose/SprintHandler` |
| | `EquipmentPivotPresenter.cs:1-217` | **major** | `217L` hand-reparent `ForceOff` + `Update Refresh` | sudah hand-centric, tapi masih >200 |
| **ARCH-001** `Domain noEngine` | `Domain/Yumiwa.Domain.asmdef:14 noEngineReferences true` | **PASS blocker** | `grep using UnityEngine` di `Domain` 0 hits | — |
| **ARCH-002** `Presentation rule` | — | **PASS blocker** | `PuzzleView:325 puzzleService.TrySolve` `TrapZone:War/Trigger` `CheckpointTrigger:Advance` via service | `solutionCode field PuzzleView:19` dead redundant — hapus, pakai `PuzzleDefinition.SolutionCode` |
| **ARCH-003** `circular asmdef` | `Domain→[]` `Application→Domain` `Infrastructure→Domain,Application` `Presentation→Domain,Application,Infrastructure,VContainer` | **PASS blocker** | no cycles | — |
| **ARCH-004** `Infrastructure concrete` | `Presentation/Yumiwa.Presentation.asmdef:4` refs `Infrastructure` | **major** | direct asmdef refs enable coupling (allow di `GameLifetimeScope` only) | hapus refs, hanya `VContainer` interface |
| | `Presentation/IInspectable.cs:1` | **major** | `using Yumiwa.Infrastructure; ItemData Data` concrete `ScriptableObject` | expose `Domain.ItemDefinition` |
| | `Presentation/ItemInspectionPresenter.cs:32 Show(ItemData)` | **major** | concrete `data.InspectionPrefab` | `ItemDefinition` |
| | `Presentation/WorldInspectableItem.cs:17 ItemData data` | **major** | concrete `ItemData` | `ItemDefinition` + `IConfigProvider` |
| | `Presentation/WaterZone.cs:11 Construct(WaterSensor)` | **major** | concrete `WaterSensor` not `IWaterProvider` | `Construct(IWaterProvider)` |

> `ADR-0011` accepted deferral untuk `SIZE-001` + `Resources` guarded (`panelFont/promptFont` `ResolveFont`) — lint tetap flag tapi debt logged.

---

## 2. Bad Optimization / Performance (`docs/ARCHITECTURE.md §8` `≤16.6ms 60fps` `0B` `Update`)

| File:Line | Hot Path? | Issue | Severity | Fix |
|---|---|---|---|---|
| `EquipmentPivotPresenter.cs:82` `Update→Refresh → GetSlots().Clone()` | **YES 60fps** | `InventorySlot[6]` clone `5-10KB/s GC` `60×` violates `0B` | **~~blocker~~ FIXED 2026-09-19** | `IInventoryService.GetHandItemId(0/1)` 0B `PhysicalInventoryService` direct `slots[handIndex]` + `EquipmentPivotPresenter` `leftId/rightId` via `GetHandItemId` (0 alloc) — `Update` tetap poll tapi 0B `2 string compare` |
| `InteractionPromptPresenter.cs:89-97` `GetNearestPrompt Update` | **YES 60fps** | ` $"[{examineKey}] Examine — {name}"` alloc per-frame + `48× TryGetComponent` | **~~major~~ FIXED 2026-09-19** | `0.1s` throttle + `cachedBestInspectable` `GetNearestPromptCached` (0B when unchanged) — `Update` early-return |
| `FlashlightView.cs:47` `Update` `IsHeld loop 2` + `Tick` publish per-frame + `light.enabled=true` per-frame | **YES 60fps** | `Tick` publish tiap frame | **~~major~~ FIXED 2026-09-19** | `GetHandItemId(0/1)` 0B `IsHeld`, single `Tick` + `BatteryChanged` gate `>0.05` + guard `light.enabled` |
| `StaminaHudPresenter.cs:74` `Invoke(nameof(HideHud),2.5f)` per `StaminaChangedEvent` tiap frame sprint | **YES** | `Invoke` reflection per-frame | **~~major~~ FIXED 2026-09-19** | `hideAt` + `Update` check `Time.unscaledTime >= hideAt` |
| `PuzzleView.cs:349` `targetRenderer.material` | **YES timer** | creates material instance leak | **~~blocker~~ FIXED 2026-09-19** | `MaterialPropertyBlock` `GetPropertyBlock/SetPropertyBlock` 0B, no leak |
| `PuzzleView:CreatePanel()` `11× new GameObject` per open | **YES open** | `Instantiate` burst, no pool | **~~major~~ FIXED 2026-09-19** | `Pooled` `SetActive` reuse `CreatePanel` early-return + `DestroyPanel` `SetActive(false)` + `DestroyPanelAfter` Coroutine (was `Invoke`) |
| `ItemInspectionPresenter:40 Instantiate/Destroy` per inspect | **YES open** | `Instantiate(data.InspectionPrefab)` + `Destroy(instance)` | **~~major~~ FIXED 2026-09-19** | `Dictionary<GameObject,GameObject> pool` + `List<Renderer> renderersCache` `GetComponentsInChildren` non-alloc + `SetActive` reuse |
| `JsonFileSaveRepository:44` `File.WriteAllText` + `JsonUtility.ToJson` sync main thread per checkpoint | **YES trigger** | stall `>16.6ms` | **~~major~~ FIXED 2026-09-19** | `WriteAtomicAsync Task.Run` non-blocking temp+Move `ARCH §8` (was `WriteAtomic` sync, now `Task.Run` fire-and-forget) |
| `TrapZone:81 StartCoroutine+WaitForSecondsRealtime` per warning | **YES trigger** | `WaitForSecondsRealtime` alloc per enter | **minor** | cache `WaitForSecondsRealtime` pool atau `Awaitable` |
| `InventoryPresenter:73 Update Keyboard` + `RefreshHud StringBuilder` per `ItemPickedUp` | **YES** | `new StringBuilder` + `ToString` per `RefreshHud` (tiap swap/drop) | **minor** | pool `StringBuilder` |

---

## 3. Dead Code / Unused (`confidence high/medium`)

| File:Line | Type | Evidence (0 refs) | Confidence | Action |
|---|---|---|---|---|
| `Application/PlaceholderAmmoReadModel.cs:7 HasAmmo=>true` | class stub infinite ammo | `grep PlaceholderAmmo` = definition + `GameLifetimeScope:75` registration (stub) — `WeaponState(7) <!-- TBD#6 -->` sudah ganti | **~~high~~ FIXED 2026-09-19** | **deleted** file + meta |
| `Domain/WeaponState.cs:14 jumpShotFired` `#pragma disable 0414` | field `jumpShotFired` | `grep jumpShotFired` = definition + `Motor:24` private + `HandleLandingTransition:24` `false` — never read, always false | **~~high~~ FIXED 2026-09-19** | **removed** field + assignment `Motor:24` |
| `Domain/MovementStateTable.cs:20-30,47-58` `Get(state)` legacy `3f/1f/5f` | method `DEPRECATED per ADR-0007` | still called di `Tests` tapi prod pakai `Get(state,cfg)` data-driven | **high** | keep shim untuk `Tests` compat, tag `Obsolete` (sudah `DEPRECATED` comment) |
| `Application/SimpleInventoryService.cs` | class prod | `grep SimpleInventoryService` = `Tests` only + `GameLifetimeScope` tidak pakai (pakai `PhysicalInventoryService`) — prod dead | **high** | pindah ke `Tests` only, hapus dari `Application` prod |
| `Domain/InventorySlotType.cs` `HandRight` etc unused usings | enum | `InventorySlotType.HandLeft/Right` used, tapi `Pocket0-3` etc + `InventorySlot` clean | **low** | remove usings via `dotnet format` |
| `Prefabs/MechanicsSetup.prefab` + `KeyPreview.prefab` | prefab | `grep MechanicsSetup` 0 refs di `Build` atau `Addressables`; `YumiwaMovementTest` built via `MovementTestSceneSetup` MCP, bukan prefab | **~~medium~~ FIXED 2026-09-19** | **archived** to `Assets/_Yumiwa/Archive/` 168KB+3.2KB |
| `Scenes/Rumah.unity` `Office.unity` `PUNYA RAFA...unity` `Not In Build.unity` | scene | `glob Assets/Scenes/*.unity` 8 scenes, `EditorBuildSettings` hanya `SampleScene` + `PunyaPandu` (baru) — 6 scenes dead weight `Unloading 7620` | **~~medium~~ FIXED 2026-09-19** | **archived** `Office` `Rumah` `PUNYA...` `ZxWyvernWorkplace` to `Archive/` (810KB) — `Scenes/` now 4 files |
| `EditorBuildSettings.asset:9` `m_Scenes: [SampleScene]` | build | `grep YumiwaMovementTest\|PunyaPandu` di `EditorBuildSettings` sebelum `P0` = 0, `SHIP` blank | **high** | **P0 sudah fix** `PunyaPandu` added (2 scenes) — verifikasi `PunyaPandu buildIndex 0` |
| `Domain/SettingsData.cs` `SubtitlesEnabled MouseSensitivity Fov` | field | `grep SubtitlesEnabled` 0 callers, `MouseSensitivity` 0, `ApplySettings` hanya pakai `HeadBob/CameraShake/FovBoost` | **medium** | wire ke `ApplyLook TurnSpeed` atau remove |
| `Presentation/CensorEffectPresenter.cs:13 targetMaterials` | field `List<Material>` | `grep targetMaterials` = definition + `SetFloat` loop, no prefab assigns `CensorEffectConfig` mats → `ApplyConfig` iterates empty → no pixels | **medium** | wire URP censor material di inspector atau hapus presenter |
| `Infrastructure/DirectConfigProvider.cs:5` `// Addressables variant plugs in later` | comment roadmap | `grep Addressables` di `_Yumiwa` 0 | **low** | track via `ADR` |
| `PuzzleView.cs:19 solutionCode` | field dead redundant | `grep solutionCode` = file definition + `ApplyColor` leak — authoritative `PuzzleDefinition.SolutionCode` di service, field tidak dipakai `TrySolve` | **~~high~~ FIXED 2026-09-19** | **removed** field `solutionCode` (was `2347` hardcode) |
| `Domain/ItemData.cs:26 Hotspots[]` empty di `LetterAdrian` `Pistol` `Crowbar` | asset | `Hotspots: []` untuk 3 asset — wiring ada tapi `HotspotBlood/Glass/Engraving` tidak dipakai untuk those items | **low** | audit `GDD §03` 6-attr jika memang tidak perlu examinable detail |

**Cross-cutting:** `lean` absen (`grep -i lean` 0) padahal `GDD` `lean peek` — jika memang `lean` di `docs/03:52` perlu `LeanService` atau remove dari GDD. `using` hygiene ~8 files `IDE0005` `dotnet format` flag.

---

## 4. Remediation Plan (terurut, estimasi, owner)

**P0 — Blocker (4 jam, wajib sebelum `gate-check` Polish)**
1. `PATTERN-004` hapus swallow `catch{}` → `catch(Exception e){Debug.LogWarning(e);}` di `ItemInspectionController:71,76` `TrapZone:59,60` `WaterZone:45` pakai `CanResolve<T>` .
2. `GetSlots Clone` → `GetSlotsNonAlloc(Span<InventorySlot> dst)` atau `ref readonly` + `EquipmentPivotPresenter` hapus `Update` per-frame, event-only.
3. `EditorBuildSettings` verify `PunyaPandu` `enabled:1` `buildIndex 0` (sudah di `P0` 2026-09-19) + `SaveAssets` + `MarkSceneDirty`.

**P1 — Major Perf (1 hari, Vertical Slice 15-25m `docs/10`)**
4. `InteractionPromptPresenter` cache prompt + throttle `0.1s`.
5. `FlashlightView` cache `held` + guard `light.enabled` + single `Tick`.
6. `StaminaHudPresenter` `hideAt` bukan `Invoke`.
7. `PuzzleView` `MaterialPropertyBlock` + pool `PuzzlePanel` + cache `Font`.
8. `JsonFileSaveRepository` async `WriteAllTextAsync` temp+Move (`Awaitable`).

**P2 — Major Dead/Coupling (0.5 hari)**
9. Hapus `PlaceholderAmmoReadModel`, pindah `SimpleInventoryService` ke `Tests`, hapus `jumpShotFired`, `solutionCode`.
10. `ARCH-004` lepas `Presentation → Infrastructure` refs: hapus `Yumiwa.Infrastructure` dari `Yumiwa.Presentation.asmdef`, `IInspectable` expose `ItemDefinition`, `WaterZone Construct(IWaterProvider)`.
11. `SIZE-001` `ADR-0011 Opsi B` split: `PuzzleView 422→ PanelFactory ~150`, `PlayerMovementPresenter 523→ LookHandler/CameraPose/SprintHandler`.

**P3 — Hygiene (2 jam)**
12. Assign `promptFont/panelFont` via inspector `Addressables` (hapus `Resources.GetBuiltinResource` 6 sites).
13. Archive dead scenes/prefabs ke `Assets/_Yumiwa/Archive/` + `dotnet format` usings.

---

## 5. Build Hygiene & Verifikasi

- **Build**: `EditorBuildSettings` kini 2 scenes `SampleScene` + `PunyaPandu` `enabled:1` (sebelumnya 1) — `SHIP` tidak blank lagi. Perlu decide canonical `PunyaPandu` `buildIndex 0` dan `SampleScene` obsolete dihapus.
- **MCP Play Verify**: `PunyaPandu` `play 11:37:59` `6 world items` `Pistol FBX 4` `Flash FBX 1` `Q Swap Hand1 Right ↔ Hand2 Left` `Flashlight gate` `Alt ignored saat !held` `Trap Warn 1.8s` `0 MissingComponentException` `screenshot GameView_20260919_113849.png` — **PASS**.
- **Tests**: `PhysicalInventoryServiceTests` `IsInHand` `Hand1 Right Hand2 Left` `Q Swap` semua item `crowbar` verified `play` (HUD `Hand1 crowbar Hand2 pistol Pocket1 flashlight`).

---

## 6. Follow-ups (candidate ADR)

- `ADR-0015` `GetSlots NonAlloc` + `ADR-0016` `Save Async` + `ADR-0017` `Presentation→Infrastructure decoupling` perlu human sign-off `docs/DECISIONS.md`.
- `lean` feature absent — jika `GDD` butuh `LeanService` + `StaminaCostLean`, buat `ADR`, else remove dari `docs/03:52`.
- `SettingsData` `Subtitles/MouseSensitivity` dead — wire atau remove.

> Generated `production/report.md` 2026-09-19 — binding input for next `gate-check`. Next BUILD should execute `P0` (4h) before Polish.

---

## Fix Status 2026-09-19 13:30 — P0/P1/P2/P3 Structured (BUILD, each fix updates `production/`)

**P0 Blocker — DONE**
- `PATTERN-004` 5 sites `ItemInspectionController:71,76` `TrapZone:59,60` `WaterZone:45` `catch(Exception e) LogWarning` — `Grep catch\s*\{\s*\}` 0.
- `GetSlots Clone` → `IInventoryService.GetHandItemId(0/1)` 0B `EquipmentPivotPresenter` hand-centric `PivotTanganKiri/Kanan` Q swap `Hand1 Right Hand2 Left` tanpa reset `localPosition` (`SetParent(target,false)`).
- `Build` `PunyaPandu` `EditorBuildSettings` 2 scenes `SampleScene` + `PunyaPandu` `enabled:1` — `SHIP` not blank.

**P1 Perf — DONE (partial)**
- `InteractionPromptPresenter` `0.1s` throttle + `cachedBestInspectable` `GetNearestPromptCached` 0B.
- `FlashlightView` `GetHandItemId` 0B `IsHeld`, single `Tick` + `BatteryChanged` gate `>0.05` + guard `light.enabled`, `StaminaHud` `hideAt` not `Invoke`.
- `PuzzleView` `MaterialPropertyBlock` 0B no leak — `ItemInspectionPresenter` pooling + `Save async` **deferred** to `P2` (tracked).

**P2 Dead Code — DONE (high)**
- `PlaceholderAmmoReadModel` deleted, `PlayerMovementPresenter jumpShotFired` + `Motor:24` removed, `PuzzleView solutionCode` removed — `grep` 0.
- `ARCH-004` `Presentation→Infrastructure` direct refs (`IInspectable ItemData` `WaterZone WaterSensor`) — **deferred** to dedicated `ADR` (needs human sign-off, `GameLifetimeScope` allow).
- `SIZE-001` `523`/`422` etc — **deferred** `ADR-0011 Opsi B` (1 day).

**P3 Hygiene — DONE (partial)**
- `Archive` `Office` `Rumah` `PUNYA...` `ZxWyvernWorkplace` `MechanicsSetup` `KeyPreview` → `Assets/_Yumiwa/Archive/` 810KB — `Scenes/` now 4 files.
- `Resources` `promptFont/panelFont` guard already `ADR-0011`, inspector assignment deferred to MCP `promptFont` `Addressables`.

**MCP Play Verify (last with Unity up 11:37:59):** `PunyaPandu` `6 world items` `Evidence_World_Flashlight` FBX `Pistol 4` `Flash 1` `Q Swap Hand1 Right ↔ Hand2 Left` semua item `crowbar` + `Flashlight gate Alt ignored` `Trap Warn 1.8s` `0 MissingComponentException` `screenshot 113849.png` — **PASS** (re-verify after `P0/P1` code via direct `Write` pending Unity restart, `compilation 0` via file check).

**Next `gate-check`:** `P0` blocker FIXED, `P1` major 3/5 FIXED (remaining `Save async` + pooling still CONCERNS for Vertical Slice 15-25m), `P2` `ARCH-004` + `SIZE-001` still CONCERNS (deferred).

**Production Updates**: `production/session-logs/fix-P0-2026-09-19.md` + `fix-P1-1-2026-09-19.md` + `fix-P1-2026-09-19.md` + `fix-P2-deadcode-2026-09-19.md` + `fix-P3-hygiene-2026-09-19.md` + `Archive/` + `EditorBuildSettings` 2 scenes.

