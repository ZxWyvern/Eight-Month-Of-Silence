using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    /// <summary>
    /// FIX-01 (world-item reconcile signal) + FIX-02 (stale hotspot progress)
    /// regression tests at the CheckpointService boundary.
    /// </summary>
    [TestFixture]
    public sealed class CheckpointLoadEventTests
    {
        private sealed class MemoryRepo : ISaveRepository
        {
            public CheckpointData Chapter;
            public bool HasChapter;
            public bool HasChapterSave => HasChapter;
            public void SaveChapter(CheckpointData data) { Chapter = data; HasChapter = true; }
            public CheckpointData LoadChapter() => Chapter;
            public void ClearChapter() { HasChapter = false; }
            public bool HasHouseCheckpoint => false;
            public void SaveHouseCheckpoint(CheckpointData data, CheckpointMilestone milestone) { }
            public CheckpointData LoadHouseCheckpoint(out CheckpointMilestone milestone)
            {
                milestone = CheckpointMilestone.None;
                return default;
            }
            public void ClearHouseCheckpoint() { }
            public void ClearAll() { HasChapter = false; }
        }

        private static InspectionSessionService Session(
            EventBus bus, IInventoryService inv, IEvidenceLog log) =>
            new InspectionSessionService(inv, bus, log);

        [Test]
        public void Load_PublishesCheckpointLoadedEvent_WithSnapshotIds()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var inv = new PhysicalInventoryService(bus, new AutoReturnService(bus));
            var session = Session(bus, inv, log);
            var svc = new CheckpointService(bus, null, null, session, inv, log, null, null);
            CheckpointLoadedEvent evt = default;
            int calls = 0;
            bus.Subscribe<CheckpointLoadedEvent>(e => { evt = e; calls++; });

            var data = new CheckpointData
            {
                Inventory = new[] { new CheckpointInventoryItem { ItemId = "crowbar" } },
                EvidenceLog = new[] { new CheckpointEvidence { EvidenceId = "b1_body" } },
            };
            svc.SaveCheckpoint(data);
            svc.LoadCheckpoint();

            Assert.AreEqual(1, calls);
            Assert.IsTrue(evt.Contains("crowbar"));
            Assert.IsTrue(evt.Contains("b1_body"));
            Assert.IsFalse(evt.Contains("nope"));
        }

        [Test]
        public void Load_ClearsStaleHotspotProgress_BeforeImport()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var inv = new FakeInventoryService();
            var session = Session(bus, inv, log);
            var svc = new CheckpointService(bus, null, null, session, inv, log, null, null);
            var rotation = new InspectionRotationModel(InspectionTuningData.Defaults());
            var hotspot = new InspectionHotspot("hs", "D", "F", -180f, 180f, -90f, 90f, 0f, 10f);
            var item = new ItemDefinition("tool", "Tool", "", 1, ItemKind.PhysicalTool, false);

            // Snapshot first (pre-examination: no keys), examine after.
            svc.SaveCheckpoint(new CheckpointData());
            Assert.IsTrue(session.Begin(item, default, new[] { hotspot }));
            Assert.IsTrue(session.TryExamineHotspot("hs", rotation));
            Assert.AreEqual(1, session.GetDiscoveredHotspotCount("tool"));
            Assert.IsTrue(session.Cancel());

            svc.LoadCheckpoint();

            Assert.AreEqual(0, session.GetDiscoveredHotspotCount("tool"));
        }

        [Test]
        public void SaveChapter_PersistsHotspotKeys_ForResume()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var inv = new FakeInventoryService();
            var session = Session(bus, inv, log);
            var repo = new MemoryRepo();
            var svc = new CheckpointService(bus, repo, null, session, inv, log, null, null);
            var rotation = new InspectionRotationModel(InspectionTuningData.Defaults());
            var hotspot = new InspectionHotspot("hs", "D", "F", -180f, 180f, -90f, 90f, 0f, 10f);
            var item = new ItemDefinition("b1_body", "Tubuh", "", 1, ItemKind.CriticalEvidence, false);

            Assert.IsTrue(session.Begin(item, default, new[] { hotspot }));
            Assert.IsTrue(session.TryExamineHotspot("hs", rotation));
            Assert.IsTrue(session.Cancel());
            svc.SaveChapter(new CheckpointData());

            Assert.IsTrue(repo.HasChapter);
            Assert.IsNotNull(repo.Chapter.DiscoveredHotspotKeys);
            Assert.AreEqual("b1_body|hs", repo.Chapter.DiscoveredHotspotKeys[0]);

            // A fresh session loading that chapter re-discovers the key.
            var session2 = Session(bus, inv, log);
            var svc2 = new CheckpointService(bus, repo, null, session2, inv, log, null, null);
            svc2.LoadChapter();
            Assert.AreEqual(1, session2.GetDiscoveredHotspotCount("b1_body"));
        }
    }
}
