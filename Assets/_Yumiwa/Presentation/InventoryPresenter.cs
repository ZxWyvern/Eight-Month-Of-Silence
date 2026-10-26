using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>Prototype HUD for 2 Hands + 4 Pockets per GDD §03. Hand 1 = Right (slot 1), Hand 2 = Left (slot 0). Q swaps. Delegates HUD to InventoryHudView (SIZE-001).</summary>
    public sealed class InventoryPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text hudText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private InventoryHudView hudView;
        [SerializeField] private InventoryGridView gridView;

        private IInventoryService inventory;
        private PhysicalInventoryService physical;
        private IEventBus bus;
        private IPlayerInput playerInput;
        private IRecoveryPoint recovery;
        private CombatService combat;

        [Inject]
        public void Construct(IInventoryService inventory, IEventBus bus, IPlayerInput playerInput, IRecoveryPoint recovery = null, CombatService combat = null)
        {
            this.inventory = inventory;
            this.physical = inventory as PhysicalInventoryService;
            this.bus = bus;
            this.playerInput = playerInput;
            this.recovery = recovery;
            this.combat = combat;
        }

        private void Awake()
        {
            if (hudText == null) hudText = GetComponentInChildren<TMP_Text>(true);
            if (hudView == null) hudView = GetComponent<InventoryHudView>();
            if (hudView == null) hudView = GetComponentInChildren<InventoryHudView>(true);
            if (hudView == null)
            {
                var go = new GameObject("InventoryHudView");
                go.transform.SetParent(transform, false);
                hudView = go.AddComponent<InventoryHudView>();
                hudView.Bind(hudText, reasonText, GetComponent<Canvas>());
            }
            else
            {
                hudView.Bind(hudText, reasonText, GetComponent<Canvas>());
            }
            if (gridView == null) gridView = GetComponentInChildren<InventoryGridView>(true);
            if (gridView != null)
            {
                gridView.OnMoveRequested = MoveSlot;
                var foundSlots = gridView.GetComponentsInChildren<InventorySlotView>(true);
                foreach (var s in foundSlots)
                {
                    int idx = s.Index;
                    s.OnClicked = OnSlotClicked;
                    s.OnDropped = MoveSlot;
                }
            }
        }

        private bool isInventoryOpen;
        private void OnEnable()
        {
            bus?.Subscribe<ItemPickedUpEvent>(OnPickedUp);
            bus?.Subscribe<InspectionTakeFailedEvent>(OnTakeFailed);
            bus?.Subscribe<AmmoChangedEvent>(OnAmmoChanged);
            if (playerInput != null)
            {
                playerInput.InventoryPressed += OnInventoryToggle;
                playerInput.DropHeldPressed += OnDropHeld;
                playerInput.FirePressed += OnUseHand;
            }
            // Hide initially (Minecraft no hotbar)
            SetInventoryOpen(false);
            RefreshHud();
        }

        private void OnDisable()
        {
            bus?.Unsubscribe<ItemPickedUpEvent>(OnPickedUp);
            bus?.Unsubscribe<InspectionTakeFailedEvent>(OnTakeFailed);
            bus?.Unsubscribe<AmmoChangedEvent>(OnAmmoChanged);
            if (playerInput != null)
            {
                playerInput.InventoryPressed -= OnInventoryToggle;
                playerInput.DropHeldPressed -= OnDropHeld;
                playerInput.FirePressed -= OnUseHand;
            }
            if (isInventoryOpen) SetInventoryOpen(false);
        }

        private void Update()
        {
            if (physical == null) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            // ESC closes inventory if open
            if (isInventoryOpen && kb.escapeKey.wasPressedThisFrame) SetInventoryOpen(false);
            // FIX-03: an open inventory is modal — R reload must not fire while managing slots.
            if (isInventoryOpen) return;
            if (kb.rKey.wasPressedThisFrame && combat != null) { combat.Reload(); hudView?.SetReason($"Reload {combat.Ammo}/{combat.MaxAmmo}"); RefreshHud(); }
        }

        private void OnUseHand()
        {
            // FIX-03: clicking/moving slots while open must not use items or fire.
            if (isInventoryOpen) return;
            if (physical != null && combat != null)
            {
                var sl = physical.GetSlots();
                for (int i = 0; i < 2; i++) if (sl[i].IsOccupied && sl[i].Item.ItemId == "pistol")
                {
                    if (!combat.TryFire()) { hudView?.SetReason("Click! Out of ammo (R)"); return; }
                    hudView?.SetReason($"Bang! {combat.Ammo}/{combat.MaxAmmo}"); RefreshHud(); return;
                }
            }
            if (physical == null) return;
            var s = physical.GetSlots();
            for (int i = 0; i < 2; i++) if (s[i].IsOccupied)
            {
                var item = s[i].Item;
                if (!physical.CanUse(item)) { hudView?.SetReason($"{item.ItemId} not in hand"); return; }
                bus?.Publish(new ItemUsedEvent(item.ItemId, (HandType)i));
                hudView?.SetReason($"Used {item.ItemId}"); RefreshHud(); return;
            }
        }

        private void OnAmmoChanged(AmmoChangedEvent e) { hudView?.SetReason($"Ammo {e.Current}/{e.Max}"); RefreshHud(); }
        private void OnPickedUp(ItemPickedUpEvent e) { RefreshHud(); hudView?.SetReason($"Picked {e.ItemId}"); }
        private void OnTakeFailed(InspectionTakeFailedEvent e) { hudView?.SetReason(e.Reason); Debug.LogWarning($"[Yumiwa] Inventory reject: {e.Reason}", this); }
        private void OnInventoryToggle() => SetInventoryOpen(!isInventoryOpen);

        private void SetInventoryOpen(bool open)
        {
            isInventoryOpen = open;
            // Toggle HUD/grid (InventoryHudView handles Canvas, GridView is child)
            if (open)
            {
                hudView?.ToggleOn();
                gridView?.gameObject.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                bus?.Publish(new InventoryOpenedEvent());
            }
            else
            {
                hudView?.ToggleOff();
                if (gridView != null) gridView.gameObject.SetActive(false);
                selectedIndex = -1;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                bus?.Publish(new InventoryClosedEvent());
            }
            RefreshHud();
        }
        private void OnDropHeld()
        {
            if (physical == null) return;
            var slots = physical.GetSlots();
            for (int i = 0; i < 2; i++) if (slots[i].IsOccupied)
            {
                var item = slots[i].Item;
                if (item.IsObjectiveItem)
                {
                    if (!physical.DropWithRecovery(item, out var recovered, out var reason)) Debug.LogWarning($"[Yumiwa] DropWithRecovery failed: {reason}", this);
                    else Debug.Log($"[Yumiwa] Objective {recovered.ItemId} queued for auto-return.", this);
                }
                else
                {
                    if (!physical.Drop(item, out var reason)) Debug.LogWarning($"[Yumiwa] Drop failed: {reason}", this);
                    else Debug.Log($"[Yumiwa] Dropped {item.ItemId}.", this);
                }
                RefreshHud(); break;
            }
        }

        private int selectedIndex = -1;
        public void MoveSlot(int from, int to)
        {
            if (physical == null) return;
            if (from < 0 || from >= 6 || to < 0 || to >= 6 || from == to) return;
            var slots = physical.GetSlots();
            if (!slots[from].IsOccupied) return;
            if (!slots[to].IsOccupied)
            {
                var item = slots[from].Item;
                var newSlots = (InventorySlot[])slots.Clone();
                newSlots[to] = new InventorySlot((InventorySlotType)to, item);
                newSlots[from] = new InventorySlot((InventorySlotType)from, default);
                var dto = new ItemDefinition[6];
                for (int i = 0; i < 6; i++) dto[i] = newSlots[i].IsOccupied ? newSlots[i].Item : default;
                physical.ImportInventory(dto);
            }
            else
            {
                var newSlots = (InventorySlot[])slots.Clone();
                var a = newSlots[from].Item;
                var b = newSlots[to].Item;
                newSlots[from] = new InventorySlot((InventorySlotType)from, b);
                newSlots[to] = new InventorySlot((InventorySlotType)to, a);
                var dto = new ItemDefinition[6];
                for (int i = 0; i < 6; i++) dto[i] = newSlots[i].IsOccupied ? newSlots[i].Item : default;
                physical.ImportInventory(dto);
            }
            RefreshHud();
        }

        public void OnSlotClicked(int index)
        {
            if (physical == null) return;
            if (selectedIndex == -1)
            {
                var slots = physical.GetSlots();
                if (index < 0 || index >= slots.Length || !slots[index].IsOccupied) return;
                selectedIndex = index;
            }
            else
            {
                if (selectedIndex != index) MoveSlot(selectedIndex, index);
                selectedIndex = -1;
            }
        }

        private void RefreshHud()
        {
            var slots = physical?.GetSlots();
            hudView?.Refresh(slots, combat);
            gridView?.Render(slots);
        }
        public void Drop(string itemId)
        {
            if (physical == null) return;
            var slot = System.Array.Find(physical.GetSlots(), s => s.IsOccupied && s.Item.ItemId == itemId);
            if (!slot.IsOccupied) return;
            var item = slot.Item;
            if (item.IsObjectiveItem)
            {
                if (!physical.DropWithRecovery(item, out _, out var reason)) Debug.LogWarning($"[Yumiwa] DropWithRecovery failed: {reason}", this);
            }
            else if (!physical.Drop(item, out var reason)) Debug.LogWarning($"[Yumiwa] Drop failed: {reason}", this);
            RefreshHud();
        }
        public void Refresh() => RefreshHud();
        public InventorySlot[] GetSlots() => physical?.GetSlots() ?? System.Array.Empty<InventorySlot>();
    }
}
