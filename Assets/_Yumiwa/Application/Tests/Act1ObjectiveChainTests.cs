using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public sealed class Act1ObjectiveChainTests
    {
        private static Act1ChapterConfig Config() => new Act1ChapterConfig(
            "M01_Alley", ChapterState.Chapter1_BloodTrail,
            "b1_enter", "Masuki TKP gang",
            "b1_observe", "Dokumentasikan 3 bukti",
            "b1_deduce", "Susun rekonstruksi",
            new[] { "b1_body", "b1_scratch", "b1_blood" },
            "ev_b1_deduction", "case_b1");

        private static MissionFlowService Mission(EventBus bus) =>
            new MissionFlowService(new[]
            {
                new MissionDefinition("M01_Alley", "Jejak Darah Pertama", "", ChapterState.Chapter1_BloodTrail, 0),
                new MissionDefinition("M02_Motel", "Pembunuhan di Motel", "", ChapterState.Chapter2_MotelMurder, 1),
            }, bus);

        private static Evidence Ev(string id) =>
            new Evidence(id, id, "Desc", "hs", "Fact", "", "Conseq", EvidenceType.Critical, EvidenceCategory.Fact);

        private static Act1ObjectiveChain Build(
            EventBus bus, ObjectiveService objectives, MissionFlowService mission, EvidenceLogService log)
        {
            return new Act1ObjectiveChain(bus, objectives, mission, log, Config());
        }

        [Test]
        public void MissionStart_SetsUpThreeObjectives()
        {
            var bus = new EventBus();
            var objectives = new ObjectiveService();
            var mission = Mission(bus);
            var log = new EvidenceLogService(bus);
            Build(bus, objectives, mission, log);

            mission.Start();

            Assert.IsTrue(objectives.IsObjectiveActive("b1_enter"));
            Assert.IsTrue(objectives.IsObjectiveActive("b1_observe"));
            Assert.IsTrue(objectives.IsObjectiveActive("b1_deduce"));
        }

        [Test]
        public void FullChain_EnterObserveDeduce_AdvancesMissionOnce()
        {
            var bus = new EventBus();
            var objectives = new ObjectiveService();
            var mission = Mission(bus);
            var log = new EvidenceLogService(bus);
            var chain = Build(bus, objectives, mission, log);
            int completions = 0;
            bus.Subscribe<Act1ChapterCompletedEvent>(e => completions++);
            mission.Start();

            chain.OnEnterCrimeScene();
            Assert.IsTrue(objectives.IsObjectiveCompleted("b1_enter"));

            log.AddEvidence(Ev("b1_body"));
            Assert.IsTrue(objectives.IsObjectiveActive("b1_observe")); // not yet
            log.AddEvidence(Ev("b1_scratch"));
            log.AddEvidence(Ev("b1_blood"));
            Assert.IsTrue(objectives.IsObjectiveCompleted("b1_observe"));

            bus.Publish(new ReconstructionSolvedEvent("case_b1", "opt_panic"));
            Assert.IsTrue(chain.ChapterCompleted);
            Assert.IsTrue(objectives.IsObjectiveCompleted("b1_deduce"));
            Assert.AreEqual("M02_Motel", mission.Current?.MissionId);
            Assert.AreEqual(1, completions);

            // Repeated solve never advances twice.
            bus.Publish(new ReconstructionSolvedEvent("case_b1", "opt_panic"));
            Assert.AreEqual("M02_Motel", mission.Current?.MissionId);
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void WrongCaseSolve_DoesNotComplete()
        {
            var bus = new EventBus();
            var objectives = new ObjectiveService();
            var mission = Mission(bus);
            var log = new EvidenceLogService(bus);
            var chain = Build(bus, objectives, mission, log);
            mission.Start();

            bus.Publish(new ReconstructionSolvedEvent("case_other", "opt_x"));
            Assert.IsFalse(chain.ChapterCompleted);
            Assert.AreEqual("M01_Alley", mission.Current?.MissionId);
        }

        [Test]
        public void ReloadDerivation_ResumedLog_RecompletesWithoutAdvancing()
        {
            var bus = new EventBus();
            var objectives = new ObjectiveService();
            var mission = Mission(bus);
            var log = new EvidenceLogService(bus);
            var chain = Build(bus, objectives, mission, log);
            // Simulate a resumed chapter: log already holds everything,
            // then the mission index import re-fires the mission event.
            log.AddEvidence(Ev("b1_body"));
            log.AddEvidence(Ev("b1_scratch"));
            log.AddEvidence(Ev("b1_blood"));
            log.AddEvidence(Ev("ev_b1_deduction"));

            mission.Start(); // re-fires M01 setup like ImportMissionIndex does

            Assert.IsTrue(chain.ChapterCompleted);
            Assert.IsTrue(objectives.IsObjectiveCompleted("b1_observe"));
            Assert.IsTrue(objectives.IsObjectiveCompleted("b1_deduce"));
            // Still on M01: setup derivation never advances the mission.
            Assert.AreEqual("M01_Alley", mission.Current?.MissionId);
        }
    }
}
