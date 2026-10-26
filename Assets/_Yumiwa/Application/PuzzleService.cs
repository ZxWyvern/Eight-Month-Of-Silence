using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Puzzle/reconstruction service per GDD §03 — environmental/evidence-based/combination/trap-observation.
    /// Wrong answer = logical feedback + retry, no progress loss; solved opens deduction/new location.
    /// Pure Application, data-driven via PuzzleDefinition.
    /// </summary>
    public sealed class PuzzleService
    {
        private readonly Dictionary<string, PuzzleDefinition> defs = new Dictionary<string, PuzzleDefinition>();
        private readonly Dictionary<string, PuzzleState> states = new Dictionary<string, PuzzleState>();
        private readonly IEvidenceLog evidenceLog;
        private readonly IInventoryService inventory;
        private readonly IEventBus bus;

        public PuzzleService(IEnumerable<PuzzleDefinition> definitions, IEvidenceLog evidenceLog, IInventoryService inventory, IEventBus bus)
        {
            this.evidenceLog = evidenceLog;
            this.inventory = inventory;
            this.bus = bus;
            foreach (var d in definitions)
            {
                if (d.IsValid)
                {
                    defs[d.PuzzleId] = d;
                    states[d.PuzzleId] = PuzzleState.Locked;
                    // Auto-unlock if no prereqs (MVP) else solvable if prereqs met
                    if (string.IsNullOrEmpty(d.RequiredEvidenceId) && string.IsNullOrEmpty(d.RequiredItemId))
                        states[d.PuzzleId] = PuzzleState.Solvable;
                }
            }
        }

        public PuzzleState GetState(string puzzleId) => states.TryGetValue(puzzleId, out var s) ? s : PuzzleState.Locked;

        public bool CanAttempt(string puzzleId)
        {
            if (!defs.TryGetValue(puzzleId, out var d))
                return false;
            if (GetState(puzzleId) == PuzzleState.Solved)
                return false;
            if (!string.IsNullOrEmpty(d.RequiredEvidenceId) && !evidenceLog.HasEvidence(d.RequiredEvidenceId))
                return false;
            if (!string.IsNullOrEmpty(d.RequiredItemId) && inventory.GetCount(d.RequiredItemId) <= 0)
                return false;
            if (states[puzzleId] == PuzzleState.Locked)
                states[puzzleId] = PuzzleState.Solvable;
            return states[puzzleId] == PuzzleState.Solvable || states[puzzleId] == PuzzleState.Failed;
        }

        public Yumiwa.Domain.PuzzleSnapshot[] ExportSnapshots()
        {
            var list = new System.Collections.Generic.List<Yumiwa.Domain.PuzzleSnapshot>();
            foreach (var kv in states) list.Add(new Yumiwa.Domain.PuzzleSnapshot{ PuzzleId = kv.Key, State = kv.Value });
            return list.ToArray();
        }

        public void ImportSnapshots(Yumiwa.Domain.PuzzleSnapshot[] snapshots)
        {
            if (snapshots == null) return;
            foreach (var s in snapshots) if (!string.IsNullOrEmpty(s.PuzzleId) && states.ContainsKey(s.PuzzleId)) states[s.PuzzleId] = s.State;
        }

        public void Clear()
        {
            foreach (var k in new System.Collections.Generic.List<string>(states.Keys)) states[k] = Yumiwa.Domain.PuzzleState.Locked;
        }

        public bool TrySolve(string puzzleId, string attempt, out string feedback)
        {
            feedback = null;
            if (!defs.TryGetValue(puzzleId, out var d))
            {
                feedback = "Puzzle not found.";
                return false;
            }
            if (!CanAttempt(puzzleId))
            {
                feedback = "Requirements not met — check Evidence Log and inventory.";
                return false;
            }
            if (d.SolutionCode == attempt)
            {
                states[puzzleId] = PuzzleState.Solved;
                bus?.Publish(new PuzzleSolvedEvent(puzzleId));
                feedback = "Correct — deduction unlocked.";
                return true;
            }
            states[puzzleId] = PuzzleState.Failed;
            feedback = "Incorrect — review clues and retry. No progress lost.";
            bus?.Publish(new PuzzleFailedEvent(puzzleId, attempt));
            // Failed goes back to solvable for retry after feedback
            states[puzzleId] = PuzzleState.Solvable;
            return false;
        }
    }

    public readonly struct PuzzleSolvedEvent { public readonly string PuzzleId; public PuzzleSolvedEvent(string id) => PuzzleId = id; }
    public readonly struct PuzzleFailedEvent { public readonly string PuzzleId; public readonly string Attempt; public PuzzleFailedEvent(string id, string attempt) { PuzzleId = id; Attempt = attempt; } }
}
