# Inventory Grid Rework — Minecraft-like Unified 6-Slot, Hand Highlight, Hybrid Drag/Click

**Date:** 2026-09-24
**Status:** Proposed (pending user review → writing-plans)
**Author:** Sinergasia Studio + AI
**Scope:** `B` — unified grid `3×2` 6 slot, Hand = 2 pertama highlight, drag hybrid, no hotbar, 1 slot per item (Minecraft, bukan RE size).

## Context

### Problem Statement
Cara `Pocket↔Hand` lama (`1-4`, `Shift+1/2`, `Q` swap, `G` drop, `H` pocket) bikin pusing — tidak visual, harus hafal shortcut, tidak ada drag. Player butuh grid yang langsung kelihatan seperti Minecraft tanpa hotbar, Hand tetap 2 slot tapi highlight, pindah 1 click/drag.

### Constraints
- Tetap 6 slot (`2 Hand + 4 Pocket`) per `GDD §03` dan `PhysicalInventoryService` — tidak ubah `CheckpointData Inventory[6]` save `v4`.
- `Critical/Optional` tidak masuk inventory (sudah `EvidenceLog`), `IsObjectiveItem` tidak hilang permanen (`IRecoveryPoint` auto-return).
- `Domain` no `UnityEngine`, `Presentation→Application→Domain` tetap, `0 GC` di hot path.
- Input `New Input System` + mouse + gamepad.

### Requirements
- Grid `3×2` (atau `2×3`) unified 6, Hand = index 0,1 border kuning + icon tangan.
- Hybrid: mouse `BeginDrag→Drop` ghost, gamepad `A select→A place`, `swap` kalau isi, `B` cancel.
- No hotbar, semua 1 slot per item (tidak ada `2×1` RE).
- `Save/Load` `Inventory` tetap via `CheckpointService`.

## Decision — Approach A Grid Reinterpret (UI only)

### Architecture
```
PhysicalInventoryService (6 InventorySlot[6] : 0,1 Hand 2-5 Pocket)
        ↑ GetSlots() / MoveSlot(from,to) → reuses QuickSwap/Transfer/SwapHands
InventoryPresenter (controller, Refresh on ItemPickedUp/InventoryChanged)
        ↑ Render(slots)
InventoryGridView (MonoBehaviour, GridLayoutGroup 3×2, 6× InventorySlotView)
InventorySlotView (Button + Image icon + TMP_Text qty + Border, IDrag/IDrop/IClick)
        ↓ OnSlotClicked / OnDragDrop → Presenter.MoveSlot
Canvas (ScreenSpaceOverlay, sorting 100, no hotbar)
```

Tidak ubah `IInventoryService`/`CheckpointData` — `0,1` Hand di service tetap, UI hanya reinterpret `0,1` sebagai Hand highlight.

### Key Interfaces
```cs
// Presenter
void MoveSlot(int fromIndex, int toIndex); // from/to 0-5, unified
void OnSlotClicked(int index); // gamepad select/place
void OnDragBegin(int index); void OnDrop(int from, int to);

// View
class InventoryGridView : MonoBehaviour {
  void Render(InventorySlot[] slots); // 6
  event Action<int,int> OnMoveRequested;
}
class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler {
  int Index; void Set(ItemDefinition item, bool isHand, bool isSelected);
}

// Service reuse (no new service method, just UI route):
// MoveSlot -> if slots[to].IsOccupied Swap else Transfer (pocket↔hand auto via index 0,1 check)
```

### Data Flow
`PhysicalInventoryService.GetSlots()[6]` → `InventoryPresenter.Refresh()` → `GridView.Render` (icon dari `ItemDefinition.DisplayName`/`InspectionPrefab`, qty, Hand highlight) → `Save` `CheckpointService` `Inventory[6]` DTO → `Load` `ImportInventory` → `Render` lagi. `Drop` tetap `DropWithRecovery` untuk objective.

### Error Handling
- `CanAccept` false → `InventoryHudView.SetReason` tooltip merah `Inventory full (6)` (bukan log saja).
- `IsObjectiveItem` + `target` bukan Hand → `EnqueueRecovery` tetap, tidak hilang.
- `drag` ke slot sama → no-op. `full` → reject. `Take` inspection aktif → `controlsLocked` block grid input.
- `TransferToPocket Type` bug `F07` sudah fix, `MoveSlot` pakai `InventorySlotType` dari index jadi aman.

## Alternatives Considered

### B) Hand Flexible
Hand tidak fixed `0,1` tapi flag di slot, Hand bisa di posisi grid mana saja. Pros: lebih bebas. Cons: ubah `PhysicalInventoryService` `IsInHand` jadi cek flag, `GetSlots` urutan bebas, save migrasi `SlotType→Hand` flag, `F07` lebih kompleks, 2-3 hari.

**Rejection:** YAGNI, `A` sudah hilangkan pusing tanpa ubah Domain save.

### C) Click-only Minimal
Tanpa drag, hanya `A select → A place`. Pros: paling simple, no `IDragHandler`. Cons: mouse user RE/Minecraft biasa drag jadi lambat.

**Rejection:** User minta `Hybrid drag`, jadi `C` ditolak.

## Consequences

### Positive
- Pusing `Shift+1` hilang — 1 drag/click langsung, Hand jelas border kuning.
- Tidak ubah `Domain`/`Save` — `CheckpointData v4` tetap, test lama jalan.
- Hybrid cover mouse + gamepad, no hotbar sesuai minta, 1-2 hari.

### Negative
- Hand masih fixed `0,1` secara data — tidak bisa custom posisi Hand di grid (trade-off `A` vs `B`).
- `1-4` `Shift+` shortcut lama masih ada sebagai fallback (bisa di-hide).

### Risks
- Drag ghost alloc → mitigasi pool 1 ghost `Image` reuse.
- Gamepad nav `3×2` wrap → mitigasi `GridLayoutGroup` + `Selectable` nav explicit.

## GDD Requirements Addressed
| GDD System | Requirement | How |
|------------|-------------|-----|
| `03` Inventory `2+4` | 2 Hand + 4 Pocket, anti soft-lock | Grid unified 6, `0,1` Hand highlight, `IsObjectiveItem` auto-return tetap |
| `07` HUD minimal | HUD jelas, 0 GC | `GridView` hanya `Refresh` on event, `TMP` `Oblata` 1 font |

## Performance Implications
- **CPU:** `Render` hanya on `ItemPickedUp/InventoryChanged`, tidak per-frame.
- **Memory:** 6 `SlotView` + 1 ghost, pool `PuzzlePanel` tetap.
- **GC:** 0B `Update` (grid tidak `StringBuilder` per-frame).

## Migration Plan
1. `InventoryGridView` + `InventorySlotView` prefab di `InventoryHUD` Canvas (ganti `InventoryHudView` `Text` list).
2. `InventoryPresenter` tambah `MoveSlot`/`OnSlotClicked`/`OnDrag` route ke `PhysicalInventoryService` existing.
3. Hapus `H`/`Shift` hint dari `InventoryHudView.Refresh` string, ganti grid.
4. `CheckpointService` tidak ubah (sudah `ExportInventory[6]`).

## Validation Criteria
- `EditMode` `PhysicalInventoryServiceTests` tetap + baru `MoveSlot empty→swap→full`.
- `PlayMode` `PunyaPandu` `pickup brass_key/crowbar` → grid `3×2` muncul, `drag 2→0` jadi Hand, `A select→A place` swap, `G` drop objective auto-return, `Save/Load` `Inventory` tetap.

## Related Decisions
- `ADR-0005` Inventory `Critical`→`Log`
- `ADR-0012` `IRecoveryPoint` auto-return
- `ADR-0022` `CheckpointData v4` full snapshot
