using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Visual-only hand pivot presenter — shows any hand item at the correct hand pivot (PivotTanganKiri/Kanan).
    /// Hand 1 = Right (slot 1), Hand 2 = Left (slot 0) per user request. Q Swap swaps slots 0↔1 and visuals follow.
    /// Handles pistol/flashlight FBX visuals (primary model fbx) and auto-offs flashlight when not held (Opsi B).
    /// Generic items (crowbar etc.) also swap via inventory, even if no hand mesh (HUD shows).
    /// </summary>
    public sealed partial class EquipmentPivotPresenter : MonoBehaviour
    {
        [FormerlySerializedAs("flashlightPivot")]
        [SerializeField] private Transform leftPivot; // PivotTanganKiri = Hand 2 (left)

        [FormerlySerializedAs("pistolPivot")]
        [SerializeField] private Transform rightPivot; // PivotTanganKanan = Hand 1 (right)

        [SerializeField] private GameObject pistolVisual; // FBX pistol_mp_1 (4 meshes)
        [SerializeField] private GameObject flashlightVisual; // FBX flashlight_mp_1 (1 mesh)
        [SerializeField] private Light flashlightLight;
        [SerializeField] private string pistolItemId = "pistol";
        [SerializeField] private string flashlightItemId = "flashlight";

        private IInventoryService inventory;
        private IFlashlightService flashlight;
        private IEventBus bus;
        private System.Action<ItemPickedUpEvent> onPicked;
        private System.Action<ItemUsedEvent> onUsed;
        private System.Action<ItemDroppedEvent> onDropped;
        private System.Action<ItemRecoveredEvent> onRecovered;

        private string lastLeftId;
        private string lastRightId;
        private bool visualsInitialized;

        [Inject]
        public void Construct(IInventoryService inventory, IEventBus bus, IFlashlightService flashlight = null)
        {
            this.inventory = inventory;
            this.bus = bus;
            this.flashlight = flashlight;
        }

        private void Awake()
        {
            if (leftPivot != null) leftPivot.gameObject.SetActive(true);
            if (rightPivot != null) rightPivot.gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            if (bus != null)
            {
                onPicked = _ => Refresh();
                onUsed = _ => Refresh();
                onDropped = _ => Refresh();
                onRecovered = _ => Refresh();
                bus.Subscribe(onPicked);
                bus.Subscribe(onUsed);
                bus.Subscribe(onDropped);
                bus.Subscribe(onRecovered);
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (bus != null)
            {
                if (onPicked != null) bus.Unsubscribe(onPicked);
                if (onUsed != null) bus.Unsubscribe(onUsed);
                if (onDropped != null) bus.Unsubscribe(onDropped);
                if (onRecovered != null) bus.Unsubscribe(onRecovered);
            }
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (inventory == null) return;
            var physical = inventory as PhysicalInventoryService;
            if (physical == null)
            {
                bool pistolInHand = inventory.IsInHand(pistolItemId);
                bool flashInHand = inventory.IsInHand(flashlightItemId);
                if (!visualsInitialized)
                {
                    ApplyAtPivot(pistolVisual, rightPivot, pistolInHand);
                    ApplyAtPivot(flashlightVisual, leftPivot, flashInHand);
                    if (flashlightLight != null && !flashInHand) flashlightLight.enabled = false;
                    visualsInitialized = true;
                    return;
                }
                ApplyAtPivot(pistolVisual, rightPivot, pistolInHand);
                ApplyAtPivot(flashlightVisual, leftPivot, flashInHand);
                if (!flashInHand && flashlight != null && flashlight.IsOn) flashlight.ForceOff();
                if (flashlightLight != null && !flashInHand) flashlightLight.enabled = false;
                return;
            }

            // 0B per-frame: no GetSlots().Clone() alloc, use direct hand query
            string leftId = inventory.GetHandItemId(0);  // Hand 2 = left
            string rightId = inventory.GetHandItemId(1); // Hand 1 = right

            bool leftIsPistol = leftId == pistolItemId;
            bool rightIsPistol = rightId == pistolItemId;
            bool leftIsFlash = leftId == flashlightItemId;
            bool rightIsFlash = rightId == flashlightItemId;
            bool flashHeld = leftIsFlash || rightIsFlash;

            if (!flashHeld && flashlight != null && flashlight.IsOn)
                flashlight.ForceOff();

            if (!visualsInitialized)
            {
                UpdateHandVisual(pistolVisual, leftIsPistol, rightIsPistol, leftPivot, rightPivot);
                UpdateHandVisual(flashlightVisual, leftIsFlash, rightIsFlash, leftPivot, rightPivot);
                ReparentLight(flashHeld, leftIsFlash, rightIsFlash);
                if (flashlightLight != null && !flashHeld) flashlightLight.enabled = false;
                lastLeftId = leftId;
                lastRightId = rightId;
                visualsInitialized = true;
                return;
            }

            if (leftId != lastLeftId || rightId != lastRightId)
            {
                UpdateHandVisual(pistolVisual, leftIsPistol, rightIsPistol, leftPivot, rightPivot);
                UpdateHandVisual(flashlightVisual, leftIsFlash, rightIsFlash, leftPivot, rightPivot);
                ReparentLight(flashHeld, leftIsFlash, rightIsFlash);
                if (flashlightLight != null && !flashHeld) flashlightLight.enabled = false;
                lastLeftId = leftId;
                lastRightId = rightId;
            }
            else
            {
                if (pistolVisual != null)
                {
                    bool pistolHeld = leftIsPistol || rightIsPistol;
                    bool atCorrect = IsAtCorrectPivot(pistolVisual.transform, leftIsPistol ? leftPivot : (rightIsPistol ? rightPivot : null));
                    if (pistolVisual.activeSelf != pistolHeld || (pistolHeld && !atCorrect))
                        UpdateHandVisual(pistolVisual, leftIsPistol, rightIsPistol, leftPivot, rightPivot);
                }
                if (flashlightVisual != null)
                {
                    bool atCorrect = IsAtCorrectPivot(flashlightVisual.transform, leftIsFlash ? leftPivot : (rightIsFlash ? rightPivot : null));
                    if (flashlightVisual.activeSelf != flashHeld || (flashHeld && !atCorrect))
                        UpdateHandVisual(flashlightVisual, leftIsFlash, rightIsFlash, leftPivot, rightPivot);
                }
                if (!flashHeld && flashlightLight != null && flashlightLight.enabled)
                    flashlightLight.enabled = false;
            }
        }


    }
}
