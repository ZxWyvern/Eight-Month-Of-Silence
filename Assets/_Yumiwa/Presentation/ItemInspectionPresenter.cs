using System.Collections.Generic;
using UnityEngine;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Spawns the dedicated inspection prefab under a designer-placed root in
    /// front of the inspection camera. The real world item is never moved or
    /// touched; it stays exactly where it was until Take is confirmed.
    /// ADR-0018: now takes GameObject prefab / IInspectable, not Infrastructure.ItemData.
    /// Pooled: Hide() now SetActive(false) not Destroy — reused for same prefab (0B per open after first).
    /// </summary>
    public sealed class ItemInspectionPresenter : MonoBehaviour
    {
        [SerializeField] private Transform inspectionRoot;
        [Tooltip("Optional dedicated light, enabled only while an item is shown.")]
        [SerializeField] private Light inspectionLight;
        [SerializeField] private string inspectionLayerName = "Inspection";

        private GameObject instance;
        private GameObject activePrefab;
        private readonly Dictionary<GameObject, GameObject> pool = new Dictionary<GameObject, GameObject>();
        private readonly List<Renderer> renderersCache = new List<Renderer>();
        private bool lightWasOn;

        private void Awake()
        {
            if (inspectionRoot == null)
                inspectionRoot = transform;
            if (inspectionLight != null)
                inspectionLight.enabled = false;
        }

        /// <summary>Shows the item; returns false when no preview prefab exists. Pooled (0B after first per prefab).</summary>
        public bool Show(GameObject inspectionPrefab)
        {
            Hide();
            if (inspectionPrefab == null)
            {
                Debug.LogWarning("[Yumiwa] Inspection preview missing; showing text only.", this);
                return false;
            }
            // Pool: reuse existing instance for same prefab, else deactivate old and get/create pooled
            if (pool.TryGetValue(inspectionPrefab, out var pooled) && pooled != null)
            {
                instance = pooled;
                instance.transform.SetParent(inspectionRoot, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(inspectionPrefab, inspectionRoot);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                pool[inspectionPrefab] = instance;
            }
            activePrefab = inspectionPrefab;
            CenterBoundsOnRoot(instance.transform);
            ApplyInspectionLayer(instance);
            if (inspectionLight != null)
            {
                lightWasOn = inspectionLight.enabled;
                inspectionLight.enabled = true;
            }
            return true;
        }

        /// <summary>ADR-0018 shim: Show via IInspectable (resolves prefab internally).</summary>
        public bool Show(IInspectable inspectable) => Show(inspectable != null ? inspectable.InspectionPrefab : null);

        public void Hide()
        {
            if (instance != null)
            {
                // Pooled: deactivate, keep in pool for reuse (no Destroy GC)
                instance.SetActive(false);
                instance = null;
                activePrefab = null;
            }
            if (inspectionLight != null)
                inspectionLight.enabled = lightWasOn;
        }

        private void OnDisable() => Hide();

        private void CenterBoundsOnRoot(Transform root)
        {
            renderersCache.Clear();
            root.GetComponentsInChildren<Renderer>(true, renderersCache);
            if (renderersCache.Count == 0) return;
            var bounds = renderersCache[0].bounds;
            for (int i = 1; i < renderersCache.Count; i++)
                bounds.Encapsulate(renderersCache[i].bounds);
            Vector3 offset = root.position - bounds.center;
            root.position += offset;
        }

        private void ApplyInspectionLayer(GameObject target)
        {
            int layer = LayerMask.NameToLayer(inspectionLayerName);
            if (layer < 0)
                return;
            SetLayerRecursive(target.transform, layer);
        }

        private static void SetLayerRecursive(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            for (int i = 0; i < node.childCount; i++)
                SetLayerRecursive(node.GetChild(i), layer);
        }
    }
}
