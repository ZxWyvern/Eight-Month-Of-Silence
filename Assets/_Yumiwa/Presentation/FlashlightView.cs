using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>Flashlight view — drives Light component from FlashlightService, Alt key toggle gated by inventory hand (docs/03:30).
    /// Wounded flicker now wired via IHealthService (was dead false).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class FlashlightView : MonoBehaviour
    {
        private IFlashlightService flashlight;
        private IInventoryService inventory;
        private IHealthService health;
        private Light flashlightLight;
        private float baseIntensity = 1.2f;

        [Inject]
        public void Construct(IFlashlightService flashlight, IInventoryService inventory = null, IHealthService health = null)
        {
            this.flashlight = flashlight;
            this.inventory = inventory;
            this.health = health;
        }

        private void Awake()
        {
            flashlightLight = GetComponent<Light>();
            if (flashlightLight != null) baseIntensity = flashlightLight.intensity;
        }

        private void OnEnable()
        {
            if (flashlight != null) flashlight.Toggled += OnToggled;
            Refresh();
        }

        private void OnDisable()
        {
            if (flashlight != null) flashlight.Toggled -= OnToggled;
        }

        private bool IsHeld()
        {
            if (inventory == null) return false;
            return inventory.IsInHand("flashlight");
        }

        private void Update()
        {
            if (flashlight == null || flashlightLight == null) return;
            bool held = IsHeld();
            // Alt toggle only when held (GDD: hanya hand yang bisa langsung dipakai)
            var kb = Keyboard.current;
            if (kb != null && (kb.leftAltKey.wasPressedThisFrame || kb.rightAltKey.wasPressedThisFrame))
            {
                if (!held)
                {
                    // Feedback when trying to use without holding
                    Debug.Log("[Yumiwa] Flashlight Alt ignored — not in hand (ambil flashlight dulu).", this);
                }
                else
                {
                    flashlight.Toggle();
                    Debug.Log($"[Yumiwa] Flashlight toggle via Alt → {(flashlight.IsOn ? "ON" : "OFF")} Battery:{flashlight.Battery:F0}", this);
                }
            }
            if (!held && flashlight.IsOn) flashlight.ForceOff();
            bool wounded = health != null && health.IsWounded;
            flashlight.Tick(Time.deltaTime, wounded);
            bool shouldBeOn = held && flashlight.IsOn;
            if (flashlightLight.enabled != shouldBeOn) flashlightLight.enabled = shouldBeOn;
            if (shouldBeOn)
            {
                float flicker = flashlight.State.Flicker;
                float targetIntensity = baseIntensity * (1f - flicker * 0.5f);
                if (Mathf.Abs(flashlightLight.intensity - targetIntensity) > 0.01f)
                    flashlightLight.intensity = targetIntensity;
            }
        }

        private void OnToggled(FlashlightToggledEvent e)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (flashlightLight == null) return;
            bool held = IsHeld();
            flashlightLight.enabled = held && flashlight != null && flashlight.IsOn;
            if (flashlightLight.enabled) flashlightLight.intensity = baseIntensity;
        }
    }
}
