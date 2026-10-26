# Item Inspection & Pickup — Setup & Architecture

Resident Evil-style flow: Interact → `ItemInspectionState` (close-up, rotate/zoom,
name + description, Take/Back) → Take adds to inventory exactly once, Back leaves
the world untouched. Implements `pickup.json`.

## 1. Folder structure (new files)

```
Assets/_Yumiwa/Domain/
  ItemDefinition.cs  InspectionTuningData.cs  IInventoryService.cs
  InspectionStartedEvent.cs  InspectionEndedEvent.cs
  ItemPickedUpEvent.cs  InspectionTakeFailedEvent.cs
  InspectionRotationModel.cs          # pure yaw/pitch/zoom math, unit-tested
Assets/_Yumiwa/Application/
  InspectionSessionService.cs         # pure session state machine, unit-tested
  InspectionTakeResult.cs  SimpleInventoryService.cs
  Tests/InspectionSessionServiceTests.cs
  Tests/SimpleInventoryServiceTests.cs
  Tests/InspectionRotationModelTests.cs  Tests/Fakes.cs (+FakeInventoryService)
Assets/_Yumiwa/Infrastructure/
  ItemData.cs  InspectionConfig.cs  InspectionInputHandler.cs
Assets/_Yumiwa/Presentation/
  IInspectable.cs  WorldInspectableItem.cs  PlayerInteractor.cs
  ItemInspectionController.cs  ItemInspectionPresenter.cs
  ItemInspectionRotator.cs  ItemInspectionUI.cs
  GameLifetimeScope.cs (registrations)  PlayerMovementPresenter.cs (input lock)
Assets/InputSystem_Actions.inputactions  # + "Inspection" action map
```

## 2. Architecture

```
WorldInspectableItem (IInspectable) ──scan──▶ PlayerInteractor ──Begin──▶ ItemInspectionController
        │                                                              │ session.Begin/Take/Cancel
        │ Consume (only after Take)                    InspectionSessionService (pure, tested)
        │                                                              │ TryAdd / events
        └◀────────────────────────────── SimpleInventoryService : IInventoryService
Controller fans out to: ItemInspectionPresenter (spawn preview prefab on
InspectionRoot, Inspection layer), ItemInspectionRotator (model → pivot/camera),
ItemInspectionUI (name/desc/message/Take/Back), InspectionInputHandler
(Inspection map only). State changes broadcast as structs on the existing
IEventBus: InspectionStarted/EndedEvent (movement + interactor lock),
ItemPickedUpEvent, InspectionTakeFailedEvent.
```

Rules enforced: world item never moved (dedicated inspection prefab instance);
Take path is `ConfirmTake → Consume → EndAfterTake`, guarded by `_takeInFlight`
and session idempotency (double-press adds exactly one); inventory rejection
keeps the session active with a message; Cancel calls `session.Cancel()` only.

## 3. Input Actions setup

`Assets/InputSystem_Actions.inputactions` already contains the `Inspection` map:

- `InspectRotate` (Value/Vector2): `<Pointer>/delta`, `<Gamepad>/rightStick`
- `InspectZoom` (Value/Vector2): `<Mouse>/scroll`, `<Gamepad>/dpad` (Y used)
- `InspectTake` (Button): `E`, `Enter`, `Gamepad/buttonSouth`
- `InspectCancel` (Button): `Escape`, `Gamepad/buttonEast`
- `InspectResetRotation` (Button): `R`, `Gamepad/buttonNorth`

If you re-import or regenerate the asset, re-add the same map. `Player/Interact`
(`E` + Hold, `Gamepad/buttonNorth`) is untouched; the Player map is disabled
during inspection so gameplay input cannot leak.

## 4. Inspector / scene setup

1. **Layer (recommended):** add layer `Inspection` (Edit → Project Settings → Tags
   and Layers, first free slot). The presenter uses it when present and otherwise
   keeps the prefab's own layer — nothing breaks if you skip this.
2. **Inspection camera:** create `InspectionCamera` (disabled by default? keep
   enabled with `Culling Mask = Inspection` only, Clear Flags = Solid Color dark).
   Place `InspectionRoot` (empty Transform = `ItemInspectionPresenter.inspectionRoot`,
   also the rotator `pivot`) ~0.9 m in front of it. Optional: assign a dedicated
   `Light` to the presenter (auto-enabled only while showing).
3. **Rig:** create `InspectionRig` with `ItemInspectionController`,
   `ItemInspectionPresenter`, `ItemInspectionRotator` (assign camera + pivot,
   starts disabled — the controller enables it). Wire the three serialized refs
   on the controller. `GameLifetimeScope` auto-injects all of them (no Find-calls
   in gameplay code).
4. **UI:** Canvas with `ItemInspectionUI`: panel + `nameText`, `descriptionText`,
   `messageText` (`UnityEngine.UI.Text`), `takeButton`, `backButton`.
5. **Scope:** assign `InspectionConfig` on `GameScope` (or leave empty for
   defaults). `PlayerInteractor` goes on the player root (radius 2.2 m) with the
   rig's `ItemInspectionController` dragged into its inspector slot.
6. **World item:** any GameObject + Collider + `WorldInspectableItem` with an
   `ItemData` assigned. No per-item code.

## 5. Example ItemData

Right-click → Create → Yumiwa → ItemData:

- `ItemId = "brass_key"`, `DisplayName = "Brass Key"`,
  `Description = "Small brass key, warm to the touch."`
- `InspectionPrefab` = close-up model (reads fine without world lighting thanks
  to the inspection light), `Quantity = 1`. Works for any number of items.

## 6. Inventory integration

`SimpleInventoryService` (16 slots / 99 stack) is registered as the default
`IInventoryService`. To use your own (e.g. save-backed), implement the three
methods of `IInventoryService` and change one line in `GameLifetimeScope`:

```csharp
builder.Register<IInventoryService, YourInventoryService>(Lifetime.Singleton);
```

Inspection code never references the concrete class.

## 7. Interaction flow

1. Player in range → `E` (`Player/Interact`) → interactor scans nearest
   available `IInspectable` → `controller.BeginInspection`.
2. Session opens (`InspectionStartedEvent`): Player map off, Inspection map on,
   cursor visible, movement/look frozen via `PlayerMovementPresenter` lock,
   preview prefab spawned centered on `InspectionRoot`, UI bound.
3. Rotate (mouse drag / right stick), zoom (wheel / d-pad), reset (`R`).
4. **Take:** `ConfirmTake` → inventory accepts → `ItemPickedUpEvent` →
   world item consumed → `EndAfterTake` → controls restored. Rejected →
   message shown, session stays open.
5. **Back / Esc:** `Cancel` → `InspectionEndedEvent(Taken=false)` → world
   untouched, controls restored.

## 8. Failure cases handled

Rapid Interact (session busy → ignored) · double Take (`_takeInFlight` +
`AlreadyCompleted` → exactly one add) · full inventory mid-inspection (message,
stays open) · missing prefab (text-only inspection, Take still works) · null
`ItemData`/invalid definition (ignored + warning) · cancel mid-rotation (state
reset) · scene transition (`OnDestroy` teardown) · world item destroyed
externally (Unity-null guard, no `Consume`, no crash) · pause (unscaled time in
rotator; session is event-driven) · missing Inspection map (fail-fast exception
with the fix in the message, same as `NewInputAdapter`).

## 9. Extension points (advanced pickup.json features)

Hotspots/puzzle clues: extend `IInspectable` (e.g. `IInspectionHotspot`) and
read the rotator yaw/pitch for contextual Examine — the session service and
controller need no changes.
