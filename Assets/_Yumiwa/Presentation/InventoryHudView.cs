using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>HUD renderer for InventoryPresenter (SIZE-001 split). Owns StringBuilder pooling + hudText/reasonText/canvas.</summary>
    public sealed class InventoryHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text hudText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Canvas hudCanvas;

        private readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(512);

        private void Awake()
        {
            if (hudText == null) hudText = GetComponentInChildren<TMP_Text>(true);
            if (hudCanvas == null) hudCanvas = GetComponent<Canvas>();
        }

        public void Bind(TMP_Text hud, TMP_Text reason, Canvas canvas)
        {
            if (hud != null) hudText = hud;
            if (reason != null) reasonText = reason;
            if (canvas != null) hudCanvas = canvas;
        }

        public void Refresh(InventorySlot[] slots, CombatService combat)
        {
            if (hudText == null) return;
            if (slots == null || slots.Length == 0) { hudText.text = "Inventory: (no service)"; return; }
            sb.Clear();
            string ammo = combat != null ? $" Ammo {combat.Ammo}/{combat.MaxAmmo}" : "";
            sb.AppendLine($"Inventory Grid 3×2 (Hand 0,1 yellow) — Drag or A select→A place to swap, [G] Drop, [Click] Use{ammo}");
            var hand1 = slots.Length > 1 ? slots[1] : default;
            var hand2 = slots.Length > 0 ? slots[0] : default;
            sb.AppendLine(hand1.IsOccupied ? $"Hand 1 (Right): {hand1.Item.ItemId} x{hand1.Item.Quantity} [Click to Use/Q Swap]" : "Hand 1 (Right): (empty)");
            sb.AppendLine(hand2.IsOccupied ? $"Hand 2 (Left): {hand2.Item.ItemId} x{hand2.Item.Quantity} [Click to Use/Q Swap]" : "Hand 2 (Left): (empty)");
            for (int i = 2; i < slots.Length; i++)
            {
                var s = slots[i];
                string label = $"Pocket {i-1}";
                sb.AppendLine(s.IsOccupied ? $"{label}: {s.Item.ItemId} x{s.Item.Quantity}" : $"{label}: (empty)");
            }
            hudText.text = sb.ToString();
        }

        public void SetReason(string reason)
        {
            if (reasonText != null) reasonText.text = reason;
            else if (hudText != null && !string.IsNullOrEmpty(reason)) hudText.text += $"\n<color=red>{reason}</color>";
        }

        public void Toggle()
        {
            if (hudCanvas != null)
            {
                hudCanvas.enabled = !hudCanvas.enabled;
                Debug.Log($"[Yumiwa] Inventory HUD toggled: {hudCanvas.enabled}", this);
            }
        }

        public void ToggleOn()
        {
            if (hudCanvas != null) hudCanvas.enabled = true;
        }

        public void ToggleOff()
        {
            if (hudCanvas != null) hudCanvas.enabled = false;
        }
    }
}
