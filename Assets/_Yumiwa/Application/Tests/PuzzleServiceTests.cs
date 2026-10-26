using NUnit.Framework;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for PuzzleService — pure Application, no Unity runtime.</summary>
    [TestFixture]
    public sealed class PuzzleServiceTests
    {
        private EventBus bus;
        private IEvidenceLog evidenceLog;
        private FakeInventoryService inventory;
        private PuzzleService puzzleService;

        [SetUp]
        public void SetUp()
        {
            bus = new EventBus();
            evidenceLog = new EvidenceLogService();
            inventory = new FakeInventoryService();
            var definitions = new[]
            {
                new PuzzleDefinition("puzzle_test", "Test Puzzle", "Desc", "req_evidence", "req_item", "1234"),
                new PuzzleDefinition("puzzle_no_req", "No Req Puzzle", "Desc", "", "", "9999"),
                new PuzzleDefinition("puzzle_ev_only", "Evidence Only", "Desc", "evidence_only", "", "5555")
            };
            puzzleService = new PuzzleService(definitions, evidenceLog, inventory, bus);
        }

        [Test]
        public void GetState_UnknownPuzzle_ReturnsLocked()
        {
            Assert.AreEqual(PuzzleState.Locked, puzzleService.GetState("unknown"));
        }

        [Test]
        public void GetState_DefinedPuzzle_ReturnsInitialState()
        {
            Assert.AreEqual(PuzzleState.Locked, puzzleService.GetState("puzzle_test"));
            Assert.AreEqual(PuzzleState.Solvable, puzzleService.GetState("puzzle_no_req"));
        }

        [Test]
        public void CanAttempt_MissingEvidence_ReturnsFalse()
        {
            Assert.IsFalse(puzzleService.CanAttempt("puzzle_test"));
        }

        [Test]
        public void CanAttempt_MissingItem_ReturnsFalse()
        {
            evidenceLog.AddEvidence(new Evidence("req_evidence", "Req", "Desc", "", "Fact", "", "", EvidenceType.Critical, EvidenceCategory.Fact));
            Assert.IsFalse(puzzleService.CanAttempt("puzzle_test"));
        }

        [Test]
        public void CanAttempt_BothMet_ReturnsTrue()
        {
            evidenceLog.AddEvidence(new Evidence("req_evidence", "Req", "Desc", "", "Fact", "", "", EvidenceType.Critical, EvidenceCategory.Fact));
            inventory.TryAdd(new ItemDefinition("req_item", "Req Item", "Item.", 1), out _);
            Assert.IsTrue(puzzleService.CanAttempt("puzzle_test"));
        }

        [Test]
        public void CanAttempt_NoRequirements_ReturnsTrue()
        {
            Assert.IsTrue(puzzleService.CanAttempt("puzzle_no_req"));
        }

        [Test]
        public void TrySolve_CorrectCode_SucceedsAndPublishesSolved()
        {
            PuzzleSolvedEvent solved = default;
            bus.Subscribe<PuzzleSolvedEvent>(e => solved = e);
            Assert.IsTrue(puzzleService.TrySolve("puzzle_no_req", "9999", out var feedback));
            Assert.AreEqual(PuzzleState.Solved, puzzleService.GetState("puzzle_no_req"));
            Assert.AreEqual("puzzle_no_req", solved.PuzzleId);
            Assert.IsTrue(feedback.Contains("Correct"));
        }

        [Test]
        public void TrySolve_IncorrectCode_FailsPublishesFailedAndRetries()
        {
            PuzzleFailedEvent failed = default;
            bus.Subscribe<PuzzleFailedEvent>(e => failed = e);
            Assert.IsFalse(puzzleService.TrySolve("puzzle_no_req", "0000", out var feedback));
            Assert.AreEqual(PuzzleState.Solvable, puzzleService.GetState("puzzle_no_req"));
            Assert.AreEqual("puzzle_no_req", failed.PuzzleId);
            Assert.AreEqual("0000", failed.Attempt);
            Assert.IsTrue(feedback.Contains("Incorrect"));
        }

        [Test]
        public void TrySolve_RequirementsNotMet_ReturnsFalse()
        {
            Assert.IsFalse(puzzleService.TrySolve("puzzle_test", "1234", out var feedback));
            Assert.IsTrue(feedback.Contains("Requirements not met"));
        }

        [Test]
        public void TrySolve_AlreadySolved_ReturnsFalse()
        {
            evidenceLog.AddEvidence(new Evidence("req_evidence", "Req", "Desc", "", "Fact", "", "", EvidenceType.Critical, EvidenceCategory.Fact));
            inventory.TryAdd(new ItemDefinition("req_item", "Req Item", "Item.", 1), out _);
            puzzleService.TrySolve("puzzle_test", "1234", out _);
            Assert.IsFalse(puzzleService.TrySolve("puzzle_test", "1234", out var feedback));
            Assert.IsTrue(feedback.Contains("Requirements not met"));
        }

        [Test]
        public void TrySolve_UnknownPuzzle_ReturnsFalse()
        {
            Assert.IsFalse(puzzleService.TrySolve("unknown", "1234", out var feedback));
            Assert.IsTrue(feedback.Contains("Puzzle not found"));
        }
    }
}