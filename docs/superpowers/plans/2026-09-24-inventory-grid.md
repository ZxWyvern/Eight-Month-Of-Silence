# Inventory Grid Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace confusing `Pocket↔Hand` shortcuts with Minecraft-like unified `3×2` 6-slot grid, Hand = first 2 highlighted, hybrid drag (mouse) / A-select→A-place (gamepad) with swap.

**Architecture:** Keep `PhysicalInventoryService` 6 `InventorySlot[6]` (`0,1 Hand 2-5 Pocket`) — UI reinterprets `0,1` as Hand highlight. New `InventoryGridView` (`GridLayoutGroup 3×2`) + 6× `InventorySlotView` (`Image` icon + `TMP_Text` qty + `Border` + `Button`/`IDragHandler`). `InventoryPresenter` adds `MoveSlot(from,to)` reusing `QuickSwap/Transfer/SwapHands`. No `Domain` save change, `CheckpointData Inventory[6]` remains.

**Tech Stack:** Unity 6, `TMPro` `Oblata SDF`, `UnityEngine.UI` `GridLayoutGroup`/`Button`/`Image`, `VContainer`, `New Input System`, `PhysicalInventoryService`, `CheckpointService`

**Spec:** `docs/superpowers/specs/2026-09-24-inventory-grid-design.md`

## Global Constraints
- Unity 6 (6000.x LTS) — `using TMPro;` `TMP_Text` `TextMeshProUGUI` already migrated, `Oblata SDF` single font `Assets/Assets/AssetFont/OblataDisplayRegular-Zp8o8 SDF.asset`
- `Domain` remains `noEngineReferences` — no `UnityEngine` in `Domain`
- 6 slots total, `0,1` Hand, `2-5` Pocket, `CheckpointData.Inventory[6]` DTO `CheckpointInventoryItem`
- Hybrid: mouse `IBeginDrag/IDrag/IEndDrag/IDrop` + gamepad `A select→A place` + `B` cancel, `swap` if occupied
- No hotbar, Minecraft 1 slot per item, no `RE` 2×1 size
- `0 GC` in `Update` — `Render` only on `ItemPickedUp/InventoryChanged`

## Review Focus
- Drag to same slot → no-op, no `Swap`/`Transfer` called, `Reason` not set — player expects nothing happens
- Full inventory `6/6` drag to occupied → `CanAccept` false → tooltip `Inventory full` not silent fail
- `IsObjectiveItem` drag Hand→Pocket → `DropWithRecovery` auto-return, not permanent loss
- Gamepad nav wrap `3×2` → `Left` from `0` wraps to `2`, `Right` from `2` wraps to `0` — player expects wrap, not stuck
- Rapid drag during `Take` inspection `controlsLocked` → grid input blocked, no `MoveSlot` during inspection

---

### Task 1: InventorySlotView Prefab + Grid Shell

**Files:**
- Create: `Assets/_Yumiwa/Presentation/InventorySlotView.cs`
- Create: `Assets/_Yumiwa/Presentation/InventorySlotView.prefab` (via `InventoryGridView` creation)
- Modify: `Assets/_Yumiwa/Presentation/InventoryGridView.cs` (new file, Grid `3×2`)
- Modify: `Assets/_Yumiwa/Presentation/InventoryHudView.cs:1-65` (keep for `Canvas` fallback, but `GridView` becomes primary)
- Test: `Assets/_Yumiwa/Application/Tests/InventoryGridViewTests.cs` (new)

**Interfaces:**
- Consumes: `ItemDefinition` (Domain), `TMP_Text`/`Image` (TMPro/UI)
- Produces: `InventorySlotView.Set(ItemDefinition item, bool isHand, bool isSelected)` , `InventorySlotView.Index`, `InventorySlotView.OnClicked(int)`, `InventorySlotView.OnDropped(int from, int to)`

- [ ] **Step 1: Write failing test for SlotView Set**

```csharp
// Assets/_Yumiwa/Application/Tests/InventoryGridViewTests.cs
[Test]
public void SlotView_Set_HandHighlight_And_Qty(){
  var go = new GameObject(); var view = go.AddComponent<Yumiwa.Presentation.InventorySlotView>();
  // assign via reflection for test
  var item = new Yumiwa.Domain.ItemDefinition("crowbar","Crowbar","",1,Yumiwa.Domain.ItemKind.PhysicalTool,false);
  view.Set(item, true, false);
  Assert.IsTrue(view.IsHandHighlighted);
  Assert.AreEqual("crowbar", view.CurrentItemId);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test` or Unity Test Framework `EditMode` `InventoryGridViewTests`
Expected: FAIL `InventorySlotView` not found

- [ ] **Step 3: Create InventorySlotView.cs minimal**

```csharp
using UnityEngine; using UnityEngine.UI; using TMPro; using UnityEngine.EventSystems;
namespace Yumiwa.Presentation{
  public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler{
    [SerializeField] private Image icon; [SerializeField] private TMP_Text qty; [SerializeField] private Image border;
    public int Index {get; set;} public string CurrentItemId {get; private set;} public bool IsHandHighlighted {get; private set;}
    public System.Action<int> OnClicked; public System.Action<int,int> OnDropped;
    public void Set(ItemDefinition item, bool isHand, bool isSelected){
      CurrentItemId = item.ItemId; IsHandHighlighted = isHand;
      if(icon!=null) icon.enabled = ItemDefinition.IsValid(item);
      if(qty!=null) qty.text = ItemDefinition.IsValid(item) ? $"x{item.Quantity}" : "";
      if(border!=null) border.color = isHand ? Color.yellow : (isSelected ? Color.white : Color.gray);
    }
    public void OnPointerClick(PointerEventData e)=>OnClicked?.Invoke(Index);
    public void OnBeginDrag(PointerEventData e){ /* create ghost */ }
    public void OnDrop(PointerEventData e){ var from = e.pointerDrag?.GetComponent<InventorySlotView>()?.Index ?? -1; if(from>=0) OnDropped?.Invoke(from, Index); }
    public void OnDrag(PointerEventData e){} public void OnEndDrag(PointerEventData e){}
  }
}
```

- [ ] **Step 4: Create InventoryGridView.cs shell**

```csharp
using UnityEngine; using UnityEngine.UI; namespace Yumiwa.Presentation{
  public sealed class InventoryGridView : MonoBehaviour{
    [SerializeField] private GridLayoutGroup grid; [SerializeField] private InventorySlotView slotPrefab;
    private InventorySlotView[] slots = new InventorySlotView[6];
    public System.Action<int,int> OnMoveRequested;
    void Awake(){ if(grid==null) grid=GetComponent<GridLayoutGroup>(); }
    public void Render(InventorySlot[] inv){
      for(int i=0;i<6;i++) slots[i].Set(inv[i].IsOccupied?inv[i].Item:default, i<2, false);
    }
  }
}
```

- [ ] **Step 5: Run test again**

Expected: PASS `IsHandHighlighted` true for `isHand:true`

- [ ] **Step 6: Commit**

```bash
git add Assets/_Yumiwa/Presentation/InventorySlotView.cs Assets/_Yumiwa/Presentation/InventoryGridView.cs Assets/_Yumiwa/Application/Tests/InventoryGridViewTests.cs
git commit -m "feat(inventory): add grid SlotView + 3x2 GridView shell"
```

---

### Task 2: InventoryPresenter MoveSlot + Hybrid Input

**Files:**
- Modify: `Assets/_Yumiwa/Presentation/InventoryPresenter.cs:12-50,81-120`
- Modify: `Assets/_Yumiwa/Presentation/InventoryGridView.cs` (add Drag/Click wiring)
- Test: `Assets/_Yumiwa/Application/Tests/InventoryGridMoveTests.cs`

**Interfaces:**
- Consumes: `InventoryGridView.OnMoveRequested(int from,int to)`, `PhysicalInventoryService.GetSlots()`, `InventorySlotView.OnClicked/OnDropped`
- Produces: `InventoryPresenter.MoveSlot(int from,int to)` , `InventoryPresenter.OnSlotClicked(int)`

- [ ] **Step 1: Write failing test for MoveSlot empty→swap**

```csharp
[Test]
public void MoveSlot_EmptySlot_NoOp(){
  var inv = new PhysicalInventoryService(new EventBus(), new AutoReturnService(new EventBus()));
  var presenter = new GameObject().AddComponent<InventoryPresenter>(); // inject via reflection
  // setup inv with item at 0
  var item = new ItemDefinition("crowbar","", "",1, ItemKind.PhysicalTool,false);
  string r; inv.TryAdd(item, out r);
  // try move 0->0
  presenter.MoveSlot(0,0); // via reflection
  Assert.AreEqual("crowbar", inv.GetSlots()[0].Item.ItemId);
}
```

- [ ] **Step 2: Run test fails**

Expected: FAIL `MoveSlot` not defined

- [ ] **Step 3: Implement MoveSlot in InventoryPresenter**

```csharp
public void MoveSlot(int from,int to){
  if(from<0||from>=6||to<0||to>=6||from==to) return;
  var slots = physical.GetSlots();
  if(!slots[from].IsOccupied) return;
  if(!slots[to].IsOccupied){
    // empty → transfer via pocket/hand logic unified: just swap Item
    var item = slots[from].Item;
    // Use existing Transfer: if to is hand (0,1) use TransferToHand else TransferToPocket
    // Simplified unified: directly swap slots (preserves Type via index)
    var tmp = slots[to];
    slots[to] = new InventorySlot((InventorySlotType)to, item);
    slots[from] = new InventorySlot((InventorySlotType)from, default);
    // Need to reflect back to service — add ImportInventory helper or do via service API
    // For plan, use physical.ImportInventory with new array
  } else {
    // occupied → swap
    var a = slots[from].Item; var b = slots[to].Item;
    // swap via ReplaceHand/Transfer
  }
  RefreshHud();
}
```

- [ ] **Step 4: Wire GridView + Gamepad select**

In `InventoryGridView.Awake` create 6 `SlotView` instances, set `Index`, hook `OnClicked` → `Presenter.OnSlotClicked`, `OnDropped` → `Presenter.MoveSlot`. In `InventoryPresenter` add `int selectedIndex=-1; OnSlotClicked(i){ if(selectedIndex==-1) selectedIndex=i; else { MoveSlot(selectedIndex,i); selectedIndex=-1; } }` + `B` cancel. Mouse drag already via `IDrop`.

- [ ] **Step 5: Run test passes**

Expected: PASS `MoveSlot` empty no-op, swap works, full inventory tooltip

- [ ] **Step 6: Commit**

```bash
git add Assets/_Yumiwa/Presentation/InventoryPresenter.cs Assets/_Yumiwa/Presentation/InventoryGridView.cs
git commit -m "feat(inventory): add MoveSlot hybrid drag/click swap"
```

---

### Task 3: Integration + Hand Highlight + Save

**Files:**
- Modify: `Assets/_Yumiwa/Presentation/InventoryGridView.cs` (Hand highlight, GridLayoutGroup 3×2, no hotbar)
- Modify: `Assets/_Yumiwa/Presentation/InventoryHudView.cs` (delegate Render to GridView, keep Canvas fallback)
- Modify: `Assets/_Yumiwa/Presentation/GameLifetimeScope.cs:103` (ensure InventoryGridView in presenterRoots)
- Test: `Assets/_Yumiwa/Application/Tests/CheckpointServiceTests.cs` (add Inventory[6] round-trip)

**Interfaces:**
- Consumes: `CheckpointService` `ExportInventory[6]` / `ImportInventory`
- Produces: `GridView` 3×2 visual, Hand `0,1` yellow border

- [ ] **Step 1: Write failing test for Checkpoint Inventory round-trip**

```csharp
[Test]
public void Checkpoint_Inventory_Grid_RoundTrip(){
  var bus=new EventBus(); var repo=new JsonFileSaveRepository(Path.GetTempPath()+Guid.NewGuid());
  var inv=new PhysicalInventoryService(bus, new AutoReturnService(bus));
  var svc=new CheckpointService(bus, repo, null,null, inv, null,null,null);
  inv.TryAdd(new ItemDefinition("crowbar","", "",1, ItemKind.PhysicalTool,false), out _);
  var data=new CheckpointData{ SaveVersion="4"};
  svc.SaveCheckpoint(data);
  inv.Clear();
  svc.LoadCheckpoint();
  Assert.AreEqual(1, inv.GetCount("crowbar"));
}
```

- [ ] **Step 2: Run fails**

Expected: FAIL `inv 0` after load (if not yet wired)

- [ ] **Step 3: Implement Grid 3×2 + Hand highlight**

In `InventoryGridView.Awake` `grid.constraint=FixedColumnCount 3`, `cellSize 80×80`, `spacing 8`, `Slots 0,1` `border.color=yellow` + `icon trowel` vs `Slots 2-5` `gray`. No hotbar `GameObject`.

- [ ] **Step 4: Wire Save/Load already done in Task 2 (F05 v4), verify `PhysicalInventoryService` `ExportInventory[6]` preserved

- [ ] **Step 5: Run test passes**

Expected: PASS `inv 1` after load

- [ ] **Step 6: Commit**

```bash
git add Assets/_Yumiwa/Presentation/InventoryGridView.cs Assets/_Yumiwa/Application/Tests/CheckpointServiceTests.cs
git commit -m "feat(inventory): grid 3x2 Hand highlight + save round-trip"
```

---

### Task 4: PlayMode Polish + Cleanup

**Files:**
- Modify: `Assets/_Yumiwa/Presentation/InventoryPresenter.cs` (remove `Update` digit `1-4` `Shift` hint string, keep `Q` `G` `R` as fallback but hidden)
- Modify: `Assets/Scenes/PunyaPandu.unity` (via `TrapZone` etc. no, just `InventoryHUD` prefab)
- Test: PlayMode `PunyaPandu` manual

**Interfaces:**
- Consumes: `InventoryGridView` `Render` on `ItemPickedUp`
- Produces: `0 GC` `Update` (grid only `Refresh` on event)

- [ ] **Step 1: Write PlayMode test (or manual script)**

```csharp
// Play: pickup brass_key, check Grid 3x2 shows icon, drag slot 2→0 becomes Hand, A select→A place swap, Save/Load retains
```

- [ ] **Step 2: Run Play `PunyaPandu`**

`unity_unity_play_mode:play` → `pickup` → `grid` → `drag` → `swap` → `SaveHouseMilestone` → `Clear` → `LoadCheckpoint` → verify `crowbar` still Hand

- [ ] **Step 3: Remove legacy `Update` shortcut string from `InventoryHudView.Refresh` (`[1-4] Pocket→Hand` text) → replace with `Grid` hint `Drag or A select→A place`

- [ ] **Step 4: Run `get_compilation_errors` 0, `console_log` 0 `MissingComponent`

- [ ] **Step 5: Commit**

```bash
git add Assets/_Yumiwa/Presentation/InventoryPresenter.cs Assets/Scenes/PunyaPandu.unity
git commit -m "feat(inventory): polish grid no hotbar, remove legacy shortcuts"
```
