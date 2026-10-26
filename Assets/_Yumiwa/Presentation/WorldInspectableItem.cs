using UnityEngine;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// World representation of an inspectable item. Never enters the inventory
    /// directly; the interactor hands it to the inspection controller, and it is
    /// consumed only after the player confirms Take.
    /// ADR-0018: decoupled from Infrastructure.ItemData — uses itemId string + IConfigProvider for Domain data + direct inspectionPrefab ref.
    /// Legacy `data` kept as Object for migration (same serialized name, type changed from ItemData to Object) so existing scene refs survive.
    /// FIX-01: reconciles against CheckpointLoadedEvent — a consumed item absent
    /// from the restored snapshot restores itself; a snapshot-owned id stays
    /// consumed, so reload can neither lose nor duplicate world items.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WorldInspectableItem : MonoBehaviour, IInspectable
    {
        // New decoupled fields
        [SerializeField] private string itemId;
        [SerializeField] private GameObject inspectionPrefab;

        [Tooltip("When true the GameObject is destroyed on Take; otherwise it is deactivated. ADR-0012: prefer false for objective items so they can auto-return.")]
        [SerializeField] private bool destroyOnTake = false;

        // Legacy migration: old serialized `data` (ItemData) is kept as Object with same name "data" so existing PunyaPandu.unity refs survive without Infrastructure reference.
#pragma warning disable 0649
        [SerializeField] private UnityEngine.Object data;
#pragma warning restore 0649

        private bool taken;
        private IRecoveryPoint recovery;
        private IEventBus bus;
        private IConfigProvider config;
        private System.Action restoreAction;
        private System.Action<CheckpointLoadedEvent> checkpointLoadedHandler;
        private string resolvedItemId;
        private GameObject resolvedPrefab;

        // IInspectable
        public string ItemId => !string.IsNullOrEmpty(itemId) ? itemId : resolvedItemId;
        public ItemDefinition Definition => config != null && !string.IsNullOrEmpty(ItemId) ? config.GetItemDefinition(ItemId) : default;
        public Evidence LinkedEvidence => config != null && !string.IsNullOrEmpty(ItemId) ? config.GetEvidence(ItemId) : default;
        public InspectionHotspot[] Hotspots => config != null && !string.IsNullOrEmpty(ItemId) ? config.GetHotspots(ItemId) : System.Array.Empty<InspectionHotspot>();
        public GameObject InspectionPrefab => inspectionPrefab != null ? inspectionPrefab : resolvedPrefab;
        public bool IsAvailable => !taken && !string.IsNullOrEmpty(ItemId) && gameObject.activeInHierarchy;

        public ItemDefinition DataDefinition => Definition;

        [Inject]
        public void Construct(IRecoveryPoint recovery, IEventBus bus, IConfigProvider config)
        {
            this.recovery = recovery;
            this.bus = bus;
            this.config = config;
            ResolveLegacy();
            SubscribeCheckpoint();
        }

        private void ResolveLegacy()
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                resolvedItemId = itemId;
                if (inspectionPrefab == null && data != null)
                {
                    var pf = TryGetLegacyPrefab(data);
                    if (pf != null) resolvedPrefab = pf;
                }
                return;
            }
            if (data != null)
            {
                try
                {
                    var t = data.GetType();
                    var fId = t.GetField("ItemId");
                    if (fId != null)
                    {
                        var val = fId.GetValue(data) as string;
                        if (!string.IsNullOrEmpty(val))
                        {
                            resolvedItemId = val;
                            var fPrefab = t.GetField("InspectionPrefab");
                            if (fPrefab != null)
                            {
                                var pv = fPrefab.GetValue(data) as GameObject;
                                if (pv != null) resolvedPrefab = pv;
                            }
                            // Copy to new serialized fields so next save persists correctly
                            itemId = val;
                            if (inspectionPrefab == null && resolvedPrefab != null) inspectionPrefab = resolvedPrefab;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Yumiwa] WorldInspectableItem legacy resolve failed for '{name}': {e.Message}", this);
                }
            }
        }

        private static GameObject TryGetLegacyPrefab(UnityEngine.Object obj)
        {
            if (obj == null) return null;
            try
            {
                var f = obj.GetType().GetField("InspectionPrefab");
                return f != null ? f.GetValue(obj) as GameObject : null;
            }
            catch { return null; }
        }

        private void Awake()
        {
            ResolveLegacy();
        }

        private void OnEnable()
        {
            string id = ItemId;
            if (!string.IsNullOrEmpty(id))
            {
                restoreAction = Restore;
                recovery?.Register(id, restoreAction);
            }
            SubscribeCheckpoint();
        }

        private void OnDisable()
        {
            if (taken) return;
            string id = ItemId;
            if (!string.IsNullOrEmpty(id) && restoreAction != null)
                recovery?.Unregister(id, restoreAction);
        }

        private void OnDestroy()
        {
            // A destroyed object can never be restored: drop the callback so
            // EnqueueRecovery cannot invoke a dead target and silently clear pending.
            string id = ItemId;
            if (!string.IsNullOrEmpty(id) && restoreAction != null)
                recovery?.Unregister(id, restoreAction);
            if (bus != null && checkpointLoadedHandler != null)
                bus.Unsubscribe(checkpointLoadedHandler);
            checkpointLoadedHandler = null;
        }

        private void SubscribeCheckpoint()
        {
            // Subscribed for the object lifetime (not per-enable): consumed items
            // are disabled and must still hear the load event to restore.
            if (checkpointLoadedHandler != null || bus == null)
                return;
            checkpointLoadedHandler = OnCheckpointLoaded;
            bus.Subscribe(checkpointLoadedHandler);
        }

        private void OnCheckpointLoaded(CheckpointLoadedEvent e)
        {
            string id = ItemId;
            if (string.IsNullOrEmpty(id))
                return;
            if (e.Contains(id))
            {
                // Snapshot owns this id: stay consumed, never duplicate.
                if (!taken)
                {
                    taken = true;
                    if (gameObject.activeSelf)
                        gameObject.SetActive(false);
                }
                return;
            }
            // Snapshot predates the Take: restore availability.
            if (taken)
                Restore();
        }

        public void Consume()
        {
            if (taken)
                return;
            taken = true;
            if (destroyOnTake && Definition.IsObjectiveItem)
            {
                // GDD §03 anti-soft-lock: objective items must stay recoverable, never destroyed.
                Debug.LogWarning($"[Yumiwa] '{name}' is objective and must stay recoverable; deactivating instead of destroy.", this);
                gameObject.SetActive(false);
                return;
            }
            if (destroyOnTake)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        public void Restore()
        {
            taken = false;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            bus?.Publish(new ItemRecoveredEvent(!string.IsNullOrEmpty(ItemId) ? ItemId : ""));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveLegacy();
            if (string.IsNullOrEmpty(ItemId))
                Debug.LogWarning($"[Yumiwa] WorldInspectableItem '{name}' has no itemId (migration pending: set itemId from legacy data ItemId).", this);
        }
#endif
    }
}
