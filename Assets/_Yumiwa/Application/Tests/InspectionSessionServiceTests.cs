using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for the pure inspection session state machine.</summary>
    [TestFixture]
    public sealed class InspectionSessionServiceTests
    {
        // Refresh compilation check
        private EventBus bus;
        private SimpleInventoryService inventory;
        private InspectionSessionService session;

        private static ItemDefinition Herb() =>
            new ItemDefinition("herb_green", "Green Herb", "Restores health.", 1);

        [SetUp]
        public void SetUp()
        {
            bus = new EventBus();
            inventory = new SimpleInventoryService(16, 99);
            session = new InspectionSessionService(inventory, bus);
        }

        [Test]
        public void BeginOpensSessionAndPublishesStarted()
        {
            InspectionStartedEvent evt = default;
            bus.Subscribe<InspectionStartedEvent>(e => evt = e);
            Assert.IsTrue(session.Begin(Herb()));
            Assert.IsTrue(session.IsActive);
            Assert.AreEqual("herb_green", evt.ItemId);
        }

        [Test]
        public void SecondBeginWhileActiveIsRejectedWithoutEvent()
        {
            int startedCount = 0;
            bus.Subscribe<InspectionStartedEvent>(_ => startedCount++);
            Assert.IsTrue(session.Begin(Herb()));
            Assert.IsFalse(session.Begin(new ItemDefinition("key", "Key", "Opens doors.", 1)));
            Assert.AreEqual(1, startedCount);
        }

        [Test]
        public void BeginWithInvalidItemIsRejected()
        {
            Assert.IsFalse(session.Begin(default));
            Assert.IsFalse(session.IsActive);
        }

        [Test]
        public void ConfirmTakeAddsExactlyOnceAndPublishesPickedUp()
        {
            int pickedUpCount = 0;
            bus.Subscribe<ItemPickedUpEvent>(e => pickedUpCount++);
            session.Begin(Herb());
            Assert.AreEqual(InspectionTakeResult.Success, session.ConfirmTake());
            Assert.AreEqual(1, inventory.GetCount("herb_green"));
            Assert.AreEqual(InspectionTakeResult.AlreadyCompleted, session.ConfirmTake());
            Assert.AreEqual(1, inventory.GetCount("herb_green"));
            Assert.AreEqual(1, pickedUpCount);
        }

        [Test]
        public void ConfirmTakeWithoutSessionReturnsNotActive()
        {
            Assert.AreEqual(InspectionTakeResult.SessionNotActive, session.ConfirmTake());
        }

        [Test]
        public void FullInventoryKeepsSessionActiveAndPublishesFailure()
        {
            var full = new SimpleInventoryService(1, 99);
            full.TryAdd(new ItemDefinition("ammo", "Ammo", "Bullets.", 1), out _);
            var blocked = new InspectionSessionService(full, bus);
            InspectionTakeFailedEvent failed = default;
            bus.Subscribe<InspectionTakeFailedEvent>(e => failed = e);
            Assert.IsTrue(blocked.Begin(Herb()));
            Assert.AreEqual(InspectionTakeResult.RejectedByInventory, blocked.ConfirmTake());
            Assert.IsTrue(blocked.IsActive);
            Assert.AreEqual("herb_green", failed.ItemId);
            Assert.IsFalse(string.IsNullOrEmpty(failed.Reason));
            Assert.AreEqual(0, full.GetCount("herb_green"));
        }

        [Test]
        public void CancelLeavesInventoryUntouchedAndPublishesEndedNotTaken()
        {
            InspectionEndedEvent evt = default;
            bus.Subscribe<InspectionEndedEvent>(e => evt = e);
            session.Begin(Herb());
            Assert.IsTrue(session.Cancel());
            Assert.IsFalse(session.IsActive);
            Assert.AreEqual("herb_green", evt.ItemId);
            Assert.IsFalse(evt.Taken);
            Assert.AreEqual(0, inventory.GetCount("herb_green"));
        }

        [Test]
        public void CancelWithoutSessionDoesNothing()
        {
            Assert.IsFalse(session.Cancel());
        }

        [Test]
        public void EndAfterTakePublishesEndedTaken()
        {
            InspectionEndedEvent evt = default;
            bus.Subscribe<InspectionEndedEvent>(e => evt = e);
            session.Begin(Herb());
            Assert.AreEqual(InspectionTakeResult.Success, session.ConfirmTake());
            Assert.IsTrue(session.EndAfterTake());
            Assert.IsFalse(session.IsActive);
            Assert.IsTrue(evt.Taken);
        }

        [Test]
        public void EndAfterTakeWithoutCompletedTakeFails()
        {
            session.Begin(Herb());
            Assert.IsFalse(session.EndAfterTake());
            Assert.IsTrue(session.IsActive);
        }

        [Test]
        public void TryExamineFirstValidDiscoversFirstValidHotspot()
        {
            var item = new ItemDefinition("watch_bloody", "Bloody Watch", "Stopped at 23:47.", 1);
            var evidence = new Evidence("watch_bloody_blood", "Bloody Watch", "Blood on crystal.", "hotspot_blood", "Blood splatter links to victim.", "", "Inspection hotspot", EvidenceType.Critical, EvidenceCategory.Fact);
            var hotspots = new[]
            {
                new InspectionHotspot("hotspot_blood", "Blood splatter detail.", "Blood splatter links to victim.", -10f, 10f, -10f, 10f, 0.5f, 5f),
                new InspectionHotspot("hotspot_engraving", "Engraving detail.", "A.T. engraved.", 80f, 100f, -10f, 10f, 0.5f, 5f)
            };
            session.Begin(item, evidence, hotspots);
            var rotValid = new InspectionRotationModel(InspectionTuningData.Defaults());
            var result = session.TryExamineFirstValid(rotValid);
            Assert.AreEqual("hotspot_blood", result);
        }

        [Test]
        public void TryExamineFirstValidReturnsNullWhenNoAngleValid()
        {
            var item = new ItemDefinition("key_brass", "Brass Key", "Old key.", 1);
            var hotspots = new[]
            {
                new InspectionHotspot("hp1", "Top detail.", "Top of item.", -10f, 10f, -10f, 10f, 0.5f, 5f)
            };
            session.Begin(item, default, hotspots);
            var rotInvalid = new InspectionRotationModel(InspectionTuningData.Defaults());
            rotInvalid.ApplyRotateDelta(50f, 50f);
            rotInvalid.Tick(1f);
            var result = session.TryExamineFirstValid(rotInvalid);
            Assert.IsNull(result);
        }

        [Test]
        public void TryExamineFirstValidReturnsNullWhenAlreadyDiscovered()
        {
            var item = new ItemDefinition("watch_bloody", "Bloody Watch", "Stopped at 23:47.", 1);
            var hotspots = new[]
            {
                new InspectionHotspot("hotspot_blood", "Blood detail.", "Blood splatter.", -10f, 10f, -10f, 10f, 0.5f, 5f)
            };
            session.Begin(item, default, hotspots);
            var rotValid = new InspectionRotationModel(InspectionTuningData.Defaults());
            Assert.AreEqual("hotspot_blood", session.TryExamineFirstValid(rotValid));
            Assert.IsNull(session.TryExamineFirstValid(rotValid));
        }

        [Test]
        public void TryExamineFirstValidReturnsNullWhenSessionInactive()
        {
            var rotValid = new InspectionRotationModel(InspectionTuningData.Defaults());
            Assert.IsNull(session.TryExamineFirstValid(rotValid));
        }
    }
}
