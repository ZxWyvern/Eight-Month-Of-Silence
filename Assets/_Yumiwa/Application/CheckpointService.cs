using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Split save per GDD §52: Chapter save (Ch1-2, non-checkpoint) vs House milestones (display-case/shovel/basement).
    /// Uses ISaveRepository for persistence; in-memory fallback keeps EditMode tests off disk.
    /// FIX-01: every load publishes CheckpointLoadedEvent so consumed world items
    /// reconcile against the snapshot (absent ids restore, owned ids stay consumed).
    /// FIX-02: hotspot progress is cleared before import so a pre-examine snapshot
    /// truly restores (stale discoveries cannot linger past evidence).
    /// </summary>
    public sealed class CheckpointService : ICheckpointService
    {
        private CheckpointData? savedData;
        private CheckpointMilestone lastMilestone = CheckpointMilestone.None;
        private readonly IEventBus bus;
        private readonly ISaveRepository repo;
        private readonly IRecoveryPoint recovery;
        private readonly InspectionSessionService inspection;
        private readonly IInventoryService inventory;
        private readonly IEvidenceLog evidenceLog;
        private readonly PuzzleService puzzleService;
        private readonly MissionFlowService missionFlow;

        public CheckpointService(IEventBus bus, ISaveRepository repo = null, IRecoveryPoint recovery = null, InspectionSessionService inspection = null, IInventoryService inventory = null, IEvidenceLog evidenceLog = null, PuzzleService puzzleService = null, MissionFlowService missionFlow = null)
        {
            this.bus = bus;
            this.repo = repo;
            this.recovery = recovery;
            this.inspection = inspection;
            this.inventory = inventory;
            this.evidenceLog = evidenceLog;
            this.puzzleService = puzzleService;
            this.missionFlow = missionFlow;
        }

        public bool HasCheckpoint => savedData.HasValue || (repo != null && repo.HasHouseCheckpoint);
        public bool HasChapterSave => repo != null && repo.HasChapterSave;
        public CheckpointMilestone LastMilestone => lastMilestone;

        // Legacy ICheckpointService
        public void SaveCheckpoint(CheckpointData data)
        {
            // ADR-0012: persist pending recoveries
            if (recovery != null && recovery.PendingRecoveries.Count > 0)
            {
                var ids = new string[recovery.PendingRecoveries.Count];
                for (int i = 0; i < ids.Length; i++) ids[i] = recovery.PendingRecoveries[i].ItemId;
                data.PendingRecoveredIds = ids;
            }
            // P1-M7: persist hotspot progress
            if (inspection != null)
            {
                var keys = inspection.ExportDiscoveredKeys();
                data.DiscoveredHotspotKeys = keys.Length > 0 ? keys : null;
            }
            if (inventory != null)
            {
                var inv = inventory.ExportInventory();
                var dto = new CheckpointInventoryItem[inv.Length];
                for (int i = 0; i < inv.Length; i++) dto[i] = new CheckpointInventoryItem{ ItemId = inv[i].ItemId, DisplayName = inv[i].DisplayName, Description = inv[i].Description, Quantity = inv[i].Quantity, Kind = (int)inv[i].Kind, IsObjectiveItem = inv[i].IsObjectiveItem };
                data.Inventory = dto;
            }
            if (evidenceLog != null)
            {
                var evs = evidenceLog.GetAllEvidence();
                var dtoEv = new CheckpointEvidence[evs.Count];
                for (int i = 0; i < evs.Count; i++) dtoEv[i] = new CheckpointEvidence{ EvidenceId = evs[i].EvidenceId, DisplayName = evs[i].DisplayName, Description = evs[i].Description, Hotspot = evs[i].Hotspot, Fact = evs[i].Fact, Dependency = evs[i].Dependency, Consequence = evs[i].Consequence, Type = (int)evs[i].Type, Category = (int)evs[i].Category };
                data.EvidenceLog = dtoEv;
            }
            if (puzzleService != null) data.Puzzles = puzzleService.ExportSnapshots();
            if (missionFlow != null) data.MissionIndex = missionFlow.ExportMissionIndex();
            savedData = data;
            repo?.SaveHouseCheckpoint(data, data.Milestone);
        }

        public CheckpointData LoadCheckpoint()
        {
            CheckpointData result;
            if (repo != null && repo.HasHouseCheckpoint)
            {
                result = repo.LoadHouseCheckpoint(out var m);
                lastMilestone = m;
            }
            else
            {
                result = savedData ?? default;
            }
            // Rehydrate pending recoveries if any
            if (result.PendingRecoveredIds != null && recovery != null)
            {
                foreach (var id in result.PendingRecoveredIds)
                    if (!string.IsNullOrEmpty(id))
                        recovery.EnqueueRecovery(new ItemDefinition(id, id, "", 1, ItemKind.PhysicalTool, true));
                recovery.ResolveAll();
            }
            // FIX-02: clear stale progress first so a pre-examine snapshot restores truly.
            if (inspection != null)
            {
                inspection.ClearAllProgress();
                if (result.DiscoveredHotspotKeys != null)
                    inspection.ImportDiscoveredKeys(result.DiscoveredHotspotKeys);
            }
            if (result.Inventory != null && inventory != null)
            {
                var arr = new ItemDefinition[result.Inventory.Length];
                for (int i = 0; i < result.Inventory.Length; i++) arr[i] = new ItemDefinition(result.Inventory[i].ItemId, result.Inventory[i].DisplayName, result.Inventory[i].Description, result.Inventory[i].Quantity, (ItemKind)result.Inventory[i].Kind, result.Inventory[i].IsObjectiveItem);
                inventory.ImportInventory(arr);
            }
            if (result.EvidenceLog != null && evidenceLog != null)
            {
                evidenceLog.Clear();
                var evs = new Evidence[result.EvidenceLog.Length];
                for (int i = 0; i < result.EvidenceLog.Length; i++) evs[i] = new Evidence(result.EvidenceLog[i].EvidenceId, result.EvidenceLog[i].DisplayName, result.EvidenceLog[i].Description, result.EvidenceLog[i].Hotspot, result.EvidenceLog[i].Fact, result.EvidenceLog[i].Dependency, result.EvidenceLog[i].Consequence, (EvidenceType)result.EvidenceLog[i].Type, (EvidenceCategory)result.EvidenceLog[i].Category);
                evidenceLog.Import(evs);
            }
            if (result.Puzzles != null && puzzleService != null) puzzleService.ImportSnapshots(result.Puzzles);
            if (missionFlow != null && !string.IsNullOrEmpty(result.SaveVersion) && result.SaveVersion.CompareTo("4") >= 0)
                missionFlow.ImportMissionIndex(result.MissionIndex);
            // FIX-01: let world items reconcile against the restored snapshot.
            bus?.Publish(BuildLoadedEvent(result));
            return result;
        }

        public void ClearCheckpoint()
        {
            savedData = null;
            lastMilestone = CheckpointMilestone.None;
            repo?.ClearHouseCheckpoint();
        }

        public void ClearChapter()
        {
            repo?.ClearChapter();
        }

        // New API — GDD §52 milestones
        public void SaveHouseMilestone(CheckpointData data, CheckpointMilestone milestone)
        {
            data.Milestone = milestone;
            lastMilestone = milestone;
            if (recovery != null && recovery.PendingRecoveries.Count > 0)
            {
                var ids = new string[recovery.PendingRecoveries.Count];
                for (int i = 0; i < ids.Length; i++) ids[i] = recovery.PendingRecoveries[i].ItemId;
                data.PendingRecoveredIds = ids;
            }
            if (inspection != null)
            {
                var keys = inspection.ExportDiscoveredKeys();
                data.DiscoveredHotspotKeys = keys.Length > 0 ? keys : null;
            }
            if (inventory != null)
            {
                var inv = inventory.ExportInventory();
                var dto = new CheckpointInventoryItem[inv.Length];
                for (int i = 0; i < inv.Length; i++) dto[i] = new CheckpointInventoryItem{ ItemId = inv[i].ItemId, DisplayName = inv[i].DisplayName, Description = inv[i].Description, Quantity = inv[i].Quantity, Kind = (int)inv[i].Kind, IsObjectiveItem = inv[i].IsObjectiveItem };
                data.Inventory = dto;
            }
            if (evidenceLog != null)
            {
                var evs = evidenceLog.GetAllEvidence();
                var dtoEv = new CheckpointEvidence[evs.Count];
                for (int i = 0; i < evs.Count; i++) dtoEv[i] = new CheckpointEvidence{ EvidenceId = evs[i].EvidenceId, DisplayName = evs[i].DisplayName, Description = evs[i].Description, Hotspot = evs[i].Hotspot, Fact = evs[i].Fact, Dependency = evs[i].Dependency, Consequence = evs[i].Consequence, Type = (int)evs[i].Type, Category = (int)evs[i].Category };
                data.EvidenceLog = dtoEv;
            }
            if (puzzleService != null) data.Puzzles = puzzleService.ExportSnapshots();
            if (missionFlow != null) data.MissionIndex = missionFlow.ExportMissionIndex();
            savedData = data;
            repo?.SaveHouseCheckpoint(data, milestone);
            bus?.Publish(new CheckpointReachedEvent(milestone));
        }

        public void SaveChapter(CheckpointData data)
        {
            // FIX gap: chapter saves carry the same progression snapshot as
            // house saves (pending recoveries + hotspot progress), reusing the
            // existing v4 fields — no schema change.
            if (recovery != null && recovery.PendingRecoveries.Count > 0)
            {
                var ids = new string[recovery.PendingRecoveries.Count];
                for (int i = 0; i < ids.Length; i++) ids[i] = recovery.PendingRecoveries[i].ItemId;
                data.PendingRecoveredIds = ids;
            }
            if (inspection != null)
            {
                var keys = inspection.ExportDiscoveredKeys();
                data.DiscoveredHotspotKeys = keys.Length > 0 ? keys : null;
            }
            if (inventory != null)
            {
                var inv = inventory.ExportInventory();
                var dto = new CheckpointInventoryItem[inv.Length];
                for (int i = 0; i < inv.Length; i++) dto[i] = new CheckpointInventoryItem{ ItemId = inv[i].ItemId, DisplayName = inv[i].DisplayName, Description = inv[i].Description, Quantity = inv[i].Quantity, Kind = (int)inv[i].Kind, IsObjectiveItem = inv[i].IsObjectiveItem };
                data.Inventory = dto;
            }
            if (evidenceLog != null)
            {
                var evs = evidenceLog.GetAllEvidence();
                var dtoEv = new CheckpointEvidence[evs.Count];
                for (int i = 0; i < evs.Count; i++) dtoEv[i] = new CheckpointEvidence{ EvidenceId = evs[i].EvidenceId, DisplayName = evs[i].DisplayName, Description = evs[i].Description, Hotspot = evs[i].Hotspot, Fact = evs[i].Fact, Dependency = evs[i].Dependency, Consequence = evs[i].Consequence, Type = (int)evs[i].Type, Category = (int)evs[i].Category };
                data.EvidenceLog = dtoEv;
            }
            if (puzzleService != null) data.Puzzles = puzzleService.ExportSnapshots();
            if (missionFlow != null) data.MissionIndex = missionFlow.ExportMissionIndex();
            repo?.SaveChapter(data);
        }

        public CheckpointData LoadChapter()
        {
            var data = repo != null && repo.HasChapterSave ? repo.LoadChapter() : default;
            if (data.Inventory != null && inventory != null)
            {
                var arr2 = new ItemDefinition[data.Inventory.Length];
                for (int i = 0; i < data.Inventory.Length; i++) arr2[i] = new ItemDefinition(data.Inventory[i].ItemId, data.Inventory[i].DisplayName, data.Inventory[i].Description, data.Inventory[i].Quantity, (ItemKind)data.Inventory[i].Kind, data.Inventory[i].IsObjectiveItem);
                inventory.ImportInventory(arr2);
            }
            if (data.EvidenceLog != null && evidenceLog != null)
            {
                evidenceLog.Clear();
                var evs2 = new Evidence[data.EvidenceLog.Length];
                for (int i = 0; i < data.EvidenceLog.Length; i++) evs2[i] = new Evidence(data.EvidenceLog[i].EvidenceId, data.EvidenceLog[i].DisplayName, data.EvidenceLog[i].Description, data.EvidenceLog[i].Hotspot, data.EvidenceLog[i].Fact, data.EvidenceLog[i].Dependency, data.EvidenceLog[i].Consequence, (EvidenceType)data.EvidenceLog[i].Type, (EvidenceCategory)data.EvidenceLog[i].Category);
                evidenceLog.Import(evs2);
            }
            if (data.Puzzles != null && puzzleService != null) puzzleService.ImportSnapshots(data.Puzzles);
            if (missionFlow != null && !string.IsNullOrEmpty(data.SaveVersion) && data.SaveVersion.CompareTo("4") >= 0)
                missionFlow.ImportMissionIndex(data.MissionIndex);
            if (data.PendingRecoveredIds != null && recovery != null)
            {
                foreach (var id in data.PendingRecoveredIds)
                    if (!string.IsNullOrEmpty(id))
                        recovery.EnqueueRecovery(new ItemDefinition(id, id, "", 1, ItemKind.PhysicalTool, true));
                recovery.ResolveAll();
            }
            // FIX-02: clear stale progress first (see LoadCheckpoint).
            if (inspection != null)
            {
                inspection.ClearAllProgress();
                if (data.DiscoveredHotspotKeys != null)
                    inspection.ImportDiscoveredKeys(data.DiscoveredHotspotKeys);
            }
            // FIX-01: reconcile world items against the restored snapshot.
            bus?.Publish(BuildLoadedEvent(data));
            return data;
        }

        public CheckpointData LoadHouseMilestone(out CheckpointMilestone milestone)
        {
            CheckpointData data;
            if (repo != null && repo.HasHouseCheckpoint)
                data = repo.LoadHouseCheckpoint(out milestone);
            else
            {
                milestone = lastMilestone;
                data = savedData ?? default;
            }
            if (data.PendingRecoveredIds != null && recovery != null)
            {
                foreach (var id in data.PendingRecoveredIds)
                    if (!string.IsNullOrEmpty(id))
                        recovery.EnqueueRecovery(new ItemDefinition(id, id, "", 1, ItemKind.PhysicalTool, true));
                recovery.ResolveAll();
            }
            // FIX-02: clear stale progress first (see LoadCheckpoint).
            if (inspection != null)
            {
                inspection.ClearAllProgress();
                if (data.DiscoveredHotspotKeys != null)
                    inspection.ImportDiscoveredKeys(data.DiscoveredHotspotKeys);
            }
            if (data.Inventory != null && inventory != null)
            {
                var arr2 = new ItemDefinition[data.Inventory.Length];
                for (int i = 0; i < data.Inventory.Length; i++) arr2[i] = new ItemDefinition(data.Inventory[i].ItemId, data.Inventory[i].DisplayName, data.Inventory[i].Description, data.Inventory[i].Quantity, (ItemKind)data.Inventory[i].Kind, data.Inventory[i].IsObjectiveItem);
                inventory.ImportInventory(arr2);
            }
            if (data.EvidenceLog != null && evidenceLog != null)
            {
                evidenceLog.Clear();
                var evs2 = new Evidence[data.EvidenceLog.Length];
                for (int i = 0; i < data.EvidenceLog.Length; i++) evs2[i] = new Evidence(data.EvidenceLog[i].EvidenceId, data.EvidenceLog[i].DisplayName, data.EvidenceLog[i].Description, data.EvidenceLog[i].Hotspot, data.EvidenceLog[i].Fact, data.EvidenceLog[i].Dependency, data.EvidenceLog[i].Consequence, (EvidenceType)data.EvidenceLog[i].Type, (EvidenceCategory)data.EvidenceLog[i].Category);
                evidenceLog.Import(evs2);
            }
            if (data.Puzzles != null && puzzleService != null) puzzleService.ImportSnapshots(data.Puzzles);
            if (missionFlow != null && !string.IsNullOrEmpty(data.SaveVersion) && data.SaveVersion.CompareTo("4") >= 0)
                missionFlow.ImportMissionIndex(data.MissionIndex);
            // FIX-01: reconcile world items against the restored snapshot.
            bus?.Publish(BuildLoadedEvent(data));
            return data;
        }

        private static CheckpointLoadedEvent BuildLoadedEvent(CheckpointData data)
        {
            string[] invIds = System.Array.Empty<string>();
            string[] evIds = System.Array.Empty<string>();
            if (data.Inventory != null)
            {
                var list = new System.Collections.Generic.List<string>();
                for (int i = 0; i < data.Inventory.Length; i++)
                    if (!string.IsNullOrEmpty(data.Inventory[i].ItemId))
                        list.Add(data.Inventory[i].ItemId);
                invIds = list.ToArray();
            }
            if (data.EvidenceLog != null)
            {
                var list = new System.Collections.Generic.List<string>();
                for (int i = 0; i < data.EvidenceLog.Length; i++)
                    if (!string.IsNullOrEmpty(data.EvidenceLog[i].EvidenceId))
                        list.Add(data.EvidenceLog[i].EvidenceId);
                evIds = list.ToArray();
            }
            return new CheckpointLoadedEvent(invIds, evIds);
        }
    }

    public readonly struct CheckpointReachedEvent
    {
        public readonly CheckpointMilestone Milestone;
        public CheckpointReachedEvent(CheckpointMilestone m) => Milestone = m;
    }
}
