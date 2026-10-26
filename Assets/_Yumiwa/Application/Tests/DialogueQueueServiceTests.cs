using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for DialogueQueueService — priority, expiry, ordering, sequences, multiplier.</summary>
    [TestFixture]
    public sealed class DialogueQueueServiceTests
    {
        private EventBus bus;
        private FakeTimeSource clock;
        private DialogueQueueService service;

        private static DialogueLine Line(string id, DialoguePriority p, string text = "Sample line text for timing.", float expires = 0f) =>
            new DialogueLine(id, "james", text, p, 0f, expires);

        [SetUp]
        public void SetUp()
        {
            bus = new EventBus();
            clock = new FakeTimeSource();
            service = new DialogueQueueService(
                new[]
                {
                    Line("story_1", DialoguePriority.Story),
                    Line("story_2", DialoguePriority.Story),
                    Line("radio_1", DialoguePriority.Radio),
                    Line("ambient_1", DialoguePriority.Ambient),
                    Line("ambient_2", DialoguePriority.Ambient),
                    Line("expiring", DialoguePriority.Radio, "Expiring line.", 5f)
                },
                new[] { new Speaker("james", "James", "#FFFFFF") },
                new[] { new DialogueSequence("seq_1", new[] { "ambient_1", "ambient_2" }, 0.5f, "on_done") },
                bus, clock);
        }

        [Test]
        public void PlayLine_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(service.PlayLine("missing"));
            Assert.IsFalse(service.HasActive);
        }

        [Test]
        public void PlayLine_NoActive_StartsImmediatelyAndPublishesStarted()
        {
            DialogueStarted started = default;
            bus.Subscribe<DialogueStarted>(e => started = e);
            Assert.IsTrue(service.PlayLine("ambient_1"));
            Assert.IsTrue(service.HasActive);
            Assert.AreEqual("ambient_1", started.LineId);
            Assert.AreEqual("james", started.SpeakerId);
        }

        [Test]
        public void HighPriority_InterruptsLowerAndDropsIt()
        {
            DialogueInterrupted interrupted = default;
            bus.Subscribe<DialogueInterrupted>(e => interrupted = e);
            service.PlayLine("ambient_1");
            Assert.IsTrue(service.PlayLine("story_1"));
            Assert.AreEqual("ambient_1", interrupted.LineId);
            Assert.AreEqual("story_1", interrupted.ByLineId);
            Assert.AreEqual("story_1", service.ActiveLine.Value.LineId);
            Assert.AreEqual(0, service.QueuedCount); // dropped, not requeued
        }

        [Test]
        public void EqualOrLowerPriority_Enqueues()
        {
            service.PlayLine("radio_1");
            Assert.IsTrue(service.PlayLine("ambient_1"));
            Assert.AreEqual("radio_1", service.ActiveLine.Value.LineId);
            Assert.AreEqual(1, service.QueuedCount);
        }

        [Test]
        public void ExpiredQueuedLine_DroppedSilently()
        {
            DialogueStarted second = default;
            bus.Subscribe<DialogueStarted>(e => second = e);
            service.PlayLine("radio_1");
            service.PlayLine("expiring"); // enqueued behind radio_1
            clock.Now = 10f; // expiring (5s) lapses; radio_1 duration ~3.3s also ends
            service.Tick(clock.Now);
            Assert.IsFalse(service.HasActive); // nothing valid to advance to
            Assert.AreEqual(0, service.QueuedCount);
            Assert.AreNotEqual("expiring", second.LineId);
        }

        [Test]
        public void SimultaneousEnqueue_CorrectPriorityOrdering()
        {
            service.PlayLine("story_1");
            service.PlayLine("ambient_1");
            service.PlayLine("radio_1");
            service.PlayLine("ambient_2");
            clock.Now = 30f; // story_1 ends
            service.Tick(clock.Now);
            Assert.AreEqual("radio_1", service.ActiveLine.Value.LineId);
            clock.Now = 60f;
            service.Tick(clock.Now);
            Assert.AreEqual("ambient_1", service.ActiveLine.Value.LineId);
        }

        [Test]
        public void Sequence_PlaysStartToFinishWithGaps()
        {
            var completed = new System.Collections.Generic.List<SequenceCompleted>();
            bus.Subscribe<SequenceCompleted>(e => completed.Add(e));
            Assert.IsTrue(service.PlaySequence("seq_1"));
            Assert.AreEqual("ambient_1", service.ActiveLine.Value.LineId);
            clock.Now = 30f; // first line ends; gap 0.5s pending
            service.Tick(clock.Now);
            Assert.IsFalse(service.HasActive);
            Assert.IsTrue(service.IsSequenceActive);
            clock.Now = 30.6f;
            service.Tick(clock.Now);
            Assert.AreEqual("ambient_2", service.ActiveLine.Value.LineId);
            clock.Now = 90f;
            service.Tick(clock.Now);
            Assert.AreEqual(1, completed.Count);
            Assert.IsTrue(completed[0].Completed);
            Assert.AreEqual("on_done", completed[0].OnCompleteEventId);
        }

        [Test]
        public void ReadingSpeedMultiplier_ChangesActiveDuration()
        {
            service.PlayLine("ambient_1");
            float normalEnd = service.ActiveEndTime;
            service.SetReadingSpeedMultiplier(1.3f);
            Assert.Greater(service.ActiveEndTime, normalEnd);
            service.SetReadingSpeedMultiplier(0.75f);
            Assert.Less(service.ActiveEndTime, normalEnd);
        }

        [Test]
        public void CancelLine_RemovesQueuedOrActive()
        {
            service.PlayLine("radio_1");
            service.PlayLine("ambient_1");
            Assert.IsTrue(service.CancelLine("ambient_1"));
            Assert.AreEqual(0, service.QueuedCount);
            Assert.IsTrue(service.CancelLine("radio_1"));
            Assert.IsFalse(service.HasActive);
            Assert.IsFalse(service.CancelLine("missing"));
        }

        [Test]
        public void CancelAll_ClearsQueueAndActive()
        {
            service.PlayLine("radio_1");
            service.PlayLine("ambient_1");
            service.CancelAll();
            Assert.IsFalse(service.HasActive);
            Assert.AreEqual(0, service.QueuedCount);
        }

        [Test]
        public void Stress_ThousandPlayCancelCycles_NoThrowAndClean()
        {
            for (int i = 0; i < 1000; i++)
            {
                clock.Now = i * 0.01f;
                service.PlayLine(i % 2 == 0 ? "ambient_1" : "radio_1");
                service.Tick(clock.Now);
                service.CancelAll();
            }
            Assert.IsFalse(service.HasActive);
            Assert.AreEqual(0, service.QueuedCount);
        }
    }
}
