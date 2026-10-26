using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Pure inspection session: one active item at most, idempotent Take, explicit
    /// Cancel. Publishes struct events; never touches UnityEngine or inventory
    /// internals beyond the IInventoryService boundary.
    /// </summary>
    public sealed class InspectionSessionService
    {
        private readonly IInventoryService inventory;
        private readonly IEvidenceLog evidenceLog;
        private readonly IEventBus bus;
        private readonly Dictionary<string, InspectionProgress> progressByItem = new Dictionary<string, InspectionProgress>();

        private bool active;
        private ItemDefinition current;
        private Evidence pendingEvidence;
        private InspectionHotspot[] currentHotspots;
        private bool takeCompleted;

        public InspectionSessionService(IInventoryService inventory, IEventBus bus, IEvidenceLog evidenceLog = null)
        {
            this.inventory = inventory;
            this.evidenceLog = evidenceLog;
            this.bus = bus;
        }

        public bool IsActive => active;
        public bool TakeCompleted => takeCompleted;
        public ItemDefinition Current => current;

        /// <summary>Opens a session; returns false when busy or the item is invalid.</summary>
        public bool Begin(ItemDefinition item)
        {
            return Begin(item, default, null);
        }

        public bool Begin(ItemDefinition item, Evidence evidence, InspectionHotspot[] hotspots = null)
        {
            if (active || !ItemDefinition.IsValid(item))
                return false;
            active = true;
            current = item;
            pendingEvidence = evidence;
            currentHotspots = hotspots ?? System.Array.Empty<InspectionHotspot>();
            takeCompleted = false;
            bus.Publish(new InspectionStartedEvent(item.ItemId));
            return true;
        }

        public InspectionProgress GetProgress(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            if (!progressByItem.TryGetValue(itemId, out var p))
            {
                p = new InspectionProgress();
                progressByItem[itemId] = p;
            }
            return p;
        }

        /// <summary>
        /// Examines the first hotspot valid at the current rotation (yaw/pitch/distance).
        /// Returns the examined hotspot id on success, null when none is valid or discoverable.
        /// </summary>
        public string TryExamineFirstValid(InspectionRotationModel rotation)
        {
            if (!active || currentHotspots == null || rotation == null)
                return null;
            foreach (var hs in currentHotspots)
            {
                if (hs.HotspotId != null && ExamineHotspot(hs, rotation))
                    return hs.HotspotId;
            }
            return null;
        }

        public bool TryExamineHotspot(string hotspotId, InspectionRotationModel rotation)
        {
            if (!active || currentHotspots == null)
                return false;
            foreach (var hs in currentHotspots)
            {
                if (hs.HotspotId == hotspotId)
                    return ExamineHotspot(hs, rotation);
            }
            return false;
        }

        private bool ExamineHotspot(InspectionHotspot hs, InspectionRotationModel rotation)
        {
            if (!hs.IsValid)
                return false;
            if (!hs.IsAngleValid(rotation.Yaw, rotation.Pitch, rotation.Distance))
                return false;
            var prog = GetProgress(current.ItemId);
            if (prog.IsDiscovered(hs.HotspotId))
                return false;
            prog.MarkDiscovered(hs.HotspotId);
            // GDD: examine memicu observasi James + update Evidence Log jika relevan
            if (pendingEvidence.IsValid && evidenceLog != null && hs.HotspotId == pendingEvidence.Hotspot)
            {
                evidenceLog.AddEvidence(pendingEvidence);
            }
            else if (hs.Fact != null && evidenceLog != null)
            {
                // Hotspot fact as optional evidence
                evidenceLog.AddEvidence(new Evidence(
                    current.ItemId + "_" + hs.HotspotId,
                    hs.Description, hs.Description, hs.HotspotId, hs.Fact, "", "Hotspot discovered", EvidenceType.Optional, EvidenceCategory.Fact));
            }
            return true;
        }

        public int GetDiscoveredHotspotCount(string itemId)
        {
            if (!progressByItem.TryGetValue(itemId, out var p))
                return 0;
            return p.DiscoveredCount;
        }

        /// <summary>P1-M7: export all discovered keys as "itemId|hotspotId" for CheckpointData persistence.</summary>
        public string[] ExportDiscoveredKeys()
        {
            var list = new List<string>();
            foreach (var kv in progressByItem)
            {
                string itemId = kv.Key;
                foreach (var hid in kv.Value.DiscoveredIds)
                    list.Add(itemId + "|" + hid);
            }
            return list.ToArray();
        }

        /// <summary>P1-M7: import keys after LoadCheckpoint to survive death reload.</summary>
        public void ImportDiscoveredKeys(string[] keys)
        {
            if (keys == null) return;
            foreach (var k in keys)
            {
                if (string.IsNullOrEmpty(k)) continue;
                int sep = k.IndexOf('|');
                if (sep <= 0 || sep >= k.Length - 1) continue;
                string itemId = k.Substring(0, sep);
                string hid = k.Substring(sep + 1);
                GetProgress(itemId).MarkDiscovered(hid);
            }
        }

        /// <summary>P1-M7: clear all progress (e.g., new game).</summary>
        public void ClearAllProgress()
        {
            foreach (var p in progressByItem.Values) p.Clear();
            progressByItem.Clear();
        }

        /// <summary>
        /// Confirms Take. Critical evidence goes to EvidenceLog (no slot), physical tools go to inventory.
        /// Adds at most once; on rejection the session stays active so the player can keep inspecting.
        /// </summary>
        public InspectionTakeResult ConfirmTake()
        {
            if (!active)
                return InspectionTakeResult.SessionNotActive;
            if (takeCompleted)
                return InspectionTakeResult.AlreadyCompleted;
            // GDD §03: Critical/Optional Evidence -> Evidence Log, no slot (P1-M8 Optional bypass).
            if (current.IsCriticalEvidence || current.Kind == ItemKind.OptionalEvidence)
            {
                EvidenceType evType = current.IsCriticalEvidence ? EvidenceType.Critical : EvidenceType.Optional;
                if (evidenceLog != null && pendingEvidence.IsValid)
                {
                    evidenceLog.AddEvidence(pendingEvidence);
                }
                else if (evidenceLog != null)
                {
                    evidenceLog.AddEvidence(new Evidence(current.ItemId, current.DisplayName, current.Description, "", current.DisplayName, "", "Collected via inspection", evType, EvidenceCategory.Fact));
                }
                takeCompleted = true;
                bus.Publish(new ItemPickedUpEvent(current.ItemId, current.Quantity));
                return InspectionTakeResult.Success;
            }
            if (!inventory.TryAdd(current, out string reason))
            {
                bus.Publish(new InspectionTakeFailedEvent(current.ItemId, reason));
                return InspectionTakeResult.RejectedByInventory;
            }
            // If item had linked evidence, also log it (e.g., tool with clue)
            if (pendingEvidence.IsValid && evidenceLog != null)
                evidenceLog.AddEvidence(pendingEvidence);
            takeCompleted = true;
            bus.Publish(new ItemPickedUpEvent(current.ItemId, current.Quantity));
            return InspectionTakeResult.Success;
        }

        /// <summary>Closes after a successful Take; fails when Take never completed.</summary>
        public bool EndAfterTake()
        {
            if (!active || !takeCompleted)
                return false;
            Close(true);
            return true;
        }

        /// <summary>Cancels without touching inventory; the world item stays.</summary>
        public bool Cancel()
        {
            if (!active)
                return false;
            Close(false);
            return true;
        }

        private void Close(bool taken)
        {
            string itemId = current.ItemId;
            active = false;
            current = default;
            takeCompleted = false;
            bus.Publish(new InspectionEndedEvent(itemId, taken));
        }
    }
}
