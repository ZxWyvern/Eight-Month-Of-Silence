# Fix P3 — Hygiene 2026-09-19

**Scope**: `Resources.GetBuiltinResource` guard + archive dead scenes/prefabs
**Files**: `Assets/_Yumiwa/Archive/` (moved), `Presentation/HealthHudPresenter.cs` `InteractionPromptPresenter.cs` `PuzzleView.cs` guard already `ADR-0011`

## Dead Scenes/Prefabs Archived — DONE
- Moved `Assets/Scenes/Office.unity` (576KB) + meta → `Assets/_Yumiwa/Archive/`
- Moved `Assets/Scenes/Rumah.unity` (23KB) + meta → `Archive/`
- Moved `Assets/Scenes/PUNYA RAFA YANG NGUBAH GUA EBOL BESOK.unity` (17KB) + meta → `Archive/`
- Moved `Assets/Scenes/ZxWyvernWorkplace.unity` (12KB) + meta → `Archive/`
- Moved `Assets/_Yumiwa/Prefabs/MechanicsSetup.prefab` (168KB) + `KeyPreview.prefab` (3.2KB) → `Archive/`
- Remaining `Assets/Scenes/` now: `MainMenu.unity` `PunyaPandu.unity` `SampleScene.unity` `YumiwaMovementTest.unity` (kept per user ignore, not archived) — `Unloading 7620` should reduce.
- `Assets/_Yumiwa/Archive/` now 6 assets 810KB.

**Verify**: `ls Assets/_Yumiwa/Archive/` 6 files, `ls Assets/Scenes/` 4 files (was 8). `EditorBuildSettings` still 2 scenes `SampleScene` + `PunyaPandu` (canonical, not archived).

## Resources Guard — PARTIAL (deferred to inspector)
- `HealthHudPresenter.cs:71` `label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` — **unguard** still. `InteractionPromptPresenter.cs:55` `promptFont != null ? promptFont : Resources...` — **guarded** per `ADR-0011`. `PuzzleView.cs:338` `ResolveFont()` guard `panelFont` — **guarded** but `panelFont` not assigned inspector → still hits `Resources` at `CreatePanel`.
- **Action**: Assign `promptFont`/`panelFont` `LegacyRuntime.ttf` via inspector `Addressables` in `PunyaPandu` prefabs (`InteractionPrompt`, `Puzzle_AlleyCode` panel, `HealthHud`). Requires Unity MCP `unity_unity_component_set_property` with `promptFont` asset path `Assets/.../LegacyRuntime.ttf` (builtin, not file). Defer to next `P3` with MCP up: use `promptFont = Resources.GetBuiltinResource<Font>` as `Font` object not asset path, needs `unity_unity_execute_code` to set `Resources.GetBuiltinResource` result directly. Keep `ADR-0011` guarded fallback for now — no code change needed, just inspector assignment.

## Using Hygiene — PENDING
- `dotnet format --verify-no-changes` would flag ~8 files `IDE0005` unused usings (`InventorySlotType.cs` etc) — low, run `dotnet format` in next `P3`.

## Production Update
- `production/report.md` Dead Code `Prefabs/MechanicsSetup` + `Scenes/Rumah|Office|PUNYA...|ZxWyvern` rows marked FIXED (archived).
- Next: `P2` `ARCH-004` coupling + `SIZE-001` split still CONCERNS (deferred to `ADR`), `P1` `ItemInspectionPresenter` pooling + `Save async` still CONCERNS.

**MCP**: Unity closed, direct `bash mv` (file system) used for archive (allowed, not Unity YAML hand-edit). `Resources` guard inspector assignment deferred to MCP up.
