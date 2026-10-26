using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public sealed class ReconstructionServiceTests
    {
        private static readonly Evidence Deduction =
            new Evidence("ev_b1_deduction", "Deduksi", "Desc", "", "Pelaku kehilangan kendali dan kabur.", "", "Bab 1 selesai", EvidenceType.Critical, EvidenceCategory.Fact);

        private ReconstructionService Build(
            EventBus bus, EvidenceLogService log, InspectionSessionService session,
            int minHotspots = 0)
        {
            var c = new ReconstructionCase(
                "case_b1", "Jejak Darah Pertama",
                new[] { "b1_body", "b1_scratch", "b1_blood" }, minHotspots,
                new[] { "opt_panic", "opt_robbery", "opt_pro" },
                new[] { "Panik dan kabur", "Perampokan rapi", "Profesional tanpa jejak" },
                "opt_panic", Deduction);
            return new ReconstructionService(new[] { c }, log, session, bus);
        }

        private static Evidence Ev(string id) =>
            new Evidence(id, id, "Desc", "hs", "Fact", "", "Conseq", EvidenceType.Critical, EvidenceCategory.Fact);

        [Test]
        public void TryConclude_MissingEvidence_RejectsWithReason_NoLoss()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var session = new InspectionSessionService(new FakeInventoryService(), bus, log);
            var svc = Build(bus, log, session);

            Assert.IsFalse(svc.TryConclude("case_b1", "opt_panic", out string fb));
            Assert.IsTrue(fb.Contains("b1_body"));
            Assert.IsFalse(log.HasEvidence("ev_b1_deduction"));
            Assert.IsFalse(svc.IsSolved("case_b1"));
        }

        [Test]
        public void TryConclude_WrongOption_RetriesWithoutLoss()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var session = new InspectionSessionService(new FakeInventoryService(), bus, log);
            var svc = Build(bus, log, session);
            int fails = 0;
            bus.Subscribe<ReconstructionFailedEvent>(e => fails++);
            log.AddEvidence(Ev("b1_body"));
            log.AddEvidence(Ev("b1_scratch"));
            log.AddEvidence(Ev("b1_blood"));

            Assert.IsFalse(svc.TryConclude("case_b1", "opt_robbery", out _));
            Assert.AreEqual(1, fails);
            Assert.IsFalse(svc.IsSolved("case_b1"));
            // Retry with the right answer still works — no progress lost.
            Assert.IsTrue(svc.TryConclude("case_b1", "opt_panic", out _));
            Assert.IsTrue(svc.IsSolved("case_b1"));
        }

        [Test]
        public void TryConclude_Correct_LogsDeductionOnce_EmitsOnce()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var session = new InspectionSessionService(new FakeInventoryService(), bus, log);
            var svc = Build(bus, log, session);
            int solves = 0;
            bus.Subscribe<ReconstructionSolvedEvent>(e => solves++);
            log.AddEvidence(Ev("b1_body"));
            log.AddEvidence(Ev("b1_scratch"));
            log.AddEvidence(Ev("b1_blood"));

            Assert.IsTrue(svc.TryConclude("case_b1", "opt_panic", out _));
            Assert.AreEqual(1, solves);
            Assert.IsTrue(log.HasEvidence("ev_b1_deduction"));
            // Solved cases reject re-conclusion — no duplicate deduction.
            Assert.IsFalse(svc.TryConclude("case_b1", "opt_panic", out _));
            Assert.AreEqual(1, solves);
        }

        [Test]
        public void CanAttempt_RequiresHotspotDocumentation_WhenConfigured()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var session = new InspectionSessionService(new FakeInventoryService(), bus, log);
            var svc = Build(bus, log, session, minHotspots: 1);
            // Evidence logged directly (e.g., via Take) without examination:
            // log the evidence but register no hotspot discovery.
            log.AddEvidence(Ev("b1_body"));
            log.AddEvidence(Ev("b1_scratch"));
            log.AddEvidence(Ev("b1_blood"));

            Assert.IsFalse(svc.CanAttempt("case_b1", out var missing));
            Assert.IsTrue(missing.Length == 3); // documented 0/1 each
        }

        [Test]
        public void CanAttempt_ExaminedEvidence_PassesDocumentationGate()
        {
            var bus = new EventBus();
            var log = new EvidenceLogService(bus);
            var session = new InspectionSessionService(new FakeInventoryService(), bus, log);
            var svc = Build(bus, log, session, minHotspots: 1);
            var rotation = new InspectionRotationModel(InspectionTuningData.Defaults());
            var hotspot = new InspectionHotspot("hs", "Blood", "Fact",
                -180f, 180f, -90f, 90f, 0f, 10f);
            // Item ids match evidence ids 1:1 for Bab 1 content.
            foreach (string id in new[] { "b1_body", "b1_scratch", "b1_blood" })
            {
                var item = new ItemDefinition(id, id, "", 1, ItemKind.CriticalEvidence, false);
                Assert.IsTrue(session.Begin(item, Ev(id), new[] { hotspot }));
                Assert.IsTrue(session.TryExamineHotspot("hs", rotation));
                Assert.IsTrue(session.Cancel());
            }

            Assert.IsTrue(svc.CanAttempt("case_b1", out var missing));
            Assert.AreEqual(0, missing.Length);
        }
    }
}
