# Fix P2 — Dead Code Removal 2026-09-19

**Files**: `Infrastructure/PlaceholderAmmoReadModel.cs` deleted, `Presentation/PlayerMovementPresenter.cs` `jumpShotFired` removed, `Presentation/PlayerMovementPresenter.Motor.cs:24` `jumpShotFired = false` removed, `Presentation/PuzzleView.cs:19` `solutionCode` field removed

## Dead Code Removed (high confidence)
- `PlaceholderAmmoReadModel.cs:7 HasAmmo=>true` infinite ammo stub — `GameLifetimeScope` now uses `WeaponState(7)` `TBD#6`, placeholder not referenced after `ADR-0012` `WeaponState` + `CombatService`. Deleted file + meta.
- `PlayerMovementPresenter.cs:39` `private bool jumpShotFired` + `#pragma disable 0414` + `Motor.cs:24` assignment — never read, always false, `grep jumpShotFired` 0 uses. Removed field and assignment.
- `PuzzleView.cs:19` `[SerializeField] string solutionCode = "2347"` — dead redundant, authoritative `PuzzleDefinition.SolutionCode` via `PuzzleService.TrySolve` (see `PuzzleView:325` now uses service, not field). Removed field, no ref.

## Verify
- `grep PlaceholderAmmo` 0 after delete, `grep jumpShotFired` 0, `grep solutionCode` 0 in `Assets/_Yumiwa` (except `PuzzleDefinition.SolutionCode`).
- Compilation: `GetSlots` `GetHandItemId` still 0 errors, `PlayerMovementPresenter` 205L (was 205) now 203L, `PuzzleView` 422→421.

## Deferred (keep with ADR)
- `SimpleInventoryService` prod dead (only `Tests` uses `FakeInventoryService` shim) — keep for `Tests` compat per `ADR-0011`, not deleted now (would break `Tests` asmdef).
- `MovementStateTable legacy Get` shim — keep per `ADR-0007`.
- `ARCH-004` Presentation→Infrastructure direct refs (`IInspectable ItemData`, `WaterZone WaterSensor`) — requires `asmdef` refactor + `IWaterProvider` wiring, deferred to dedicated ADR (needs human sign-off).

## Production Update
- `production/report.md` Dead Code table high-confidence rows marked FIXED.
- Next: `P3` hygiene `Resources.GetBuiltinResource` guard assign + archive dead scenes.

**MCP**: Unity closed, direct `Write` (MCP down). Next play verify `PunyaPandu` pivot `Q Swap` still PASS (visuals FBX, hand1 right hand2 left).
