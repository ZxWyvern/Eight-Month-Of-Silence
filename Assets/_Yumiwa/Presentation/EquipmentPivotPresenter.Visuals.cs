using UnityEngine;

namespace Yumiwa.Presentation
{
    public sealed partial class EquipmentPivotPresenter
    {
        private void UpdateHandVisual(GameObject visual, bool inLeft, bool inRight, Transform leftPivot, Transform rightPivot)
        {
            if (visual == null) return;
            Transform target = null;
            if (inLeft) target = leftPivot;
            else if (inRight) target = rightPivot;
            if (target == null)
            {
                if (visual.activeSelf) visual.SetActive(false);
                return;
            }
            if (visual.transform.parent != target)
                visual.transform.SetParent(target, false);
            if (!visual.activeSelf) visual.SetActive(true);
        }

        private void ReparentLight(bool held, bool leftIsFlash = false, bool rightIsFlash = false)
        {
            if (flashlightLight == null) return;
            if (!held)
            {
                flashlightLight.enabled = false;
                return;
            }
            Transform target = leftIsFlash ? leftPivot : (rightIsFlash ? rightPivot : leftPivot);
            if (target != null && flashlightLight.transform.parent != target)
                flashlightLight.transform.SetParent(target, false);
        }

        private void ApplyAtPivot(GameObject visual, Transform pivot, bool active)
        {
            if (visual == null) return;
            if (pivot != null && visual.transform.parent != pivot)
                visual.transform.SetParent(pivot, false);
            if (visual.activeSelf != active) visual.SetActive(active);
        }

        private static bool IsAtCorrectPivot(Transform t, Transform pivot)
        {
            if (pivot == null) return t.parent == null;
            return t.parent == pivot;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(pistolItemId)) pistolItemId = "pistol";
            if (string.IsNullOrEmpty(flashlightItemId)) flashlightItemId = "flashlight";
        }
#endif
    }
}
