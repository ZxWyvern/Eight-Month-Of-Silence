using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Non-blocking subtitle queue per plan.json: lines appear bottom-screen, timed by
    /// <see cref="DialogueTiming"/>, arbitrated by <see cref="DialoguePriority"/>
    /// (Story &gt; Radio &gt; Ambient). Gameplay never pauses; Presentation polls
    /// <see cref="Tick"/> each frame and renders the active line.
    /// CLARIFY decisions: interrupted lines always drop (GDD-005); queue is transient
    /// across checkpoint reloads (SAVE-001 untouched); concrete line structs (KISS).
    /// Zero per-frame allocation: struct queue entries, index scans, no LINQ/boxing.
    /// </summary>
    public sealed class DialogueQueueService
    {
        private readonly struct QueuedEntry
        {
            public readonly DialogueLine Line;
            public readonly float EnqueueTime;
            public QueuedEntry(DialogueLine line, float enqueueTime) { Line = line; EnqueueTime = enqueueTime; }
        }

        private readonly Dictionary<string, DialogueLine> lines = new Dictionary<string, DialogueLine>();
        private readonly Dictionary<string, Speaker> speakers = new Dictionary<string, Speaker>();
        private readonly Dictionary<string, DialogueSequence> sequences = new Dictionary<string, DialogueSequence>();
        private readonly List<QueuedEntry> queue = new List<QueuedEntry>();
        private readonly IEventBus bus;
        private readonly ITimeSource clock;

        private bool hasActive;
        private DialogueLine activeLine;
        private float activeStartTime;
        private float activeEndTime;

        private bool hasSequence;
        private DialogueSequence activeSequence;
        private int sequenceIndex;
        private float sequenceNextTime;
        private string sequenceCurrentLineId;

        private float speedMultiplier = 1f;

        public DialogueQueueService(
            IEnumerable<DialogueLine> lineDefinitions,
            IEnumerable<Speaker> speakerDefinitions,
            IEnumerable<DialogueSequence> sequenceDefinitions,
            IEventBus bus,
            ITimeSource clock)
        {
            this.bus = bus;
            this.clock = clock;
            if (lineDefinitions != null)
                foreach (var l in lineDefinitions)
                    if (l.IsValid && !lines.ContainsKey(l.LineId))
                        lines[l.LineId] = l;
            if (speakerDefinitions != null)
                foreach (var s in speakerDefinitions)
                    if (s.IsValid && !speakers.ContainsKey(s.SpeakerId))
                        speakers[s.SpeakerId] = s;
            if (sequenceDefinitions != null)
                foreach (var s in sequenceDefinitions)
                    if (s.IsValid && !sequences.ContainsKey(s.SequenceId))
                        sequences[s.SequenceId] = s;
        }

        public bool HasActive => hasActive;
        public DialogueLine? ActiveLine => hasActive ? (DialogueLine?)activeLine : null;
        public float ActiveEndTime => activeEndTime;
        public int QueuedCount => queue.Count;
        public bool IsSequenceActive => hasSequence;
        public float SpeedMultiplier => speedMultiplier;

        public bool TryGetSpeaker(string speakerId, out Speaker speaker)
        {
            if (!string.IsNullOrEmpty(speakerId) && speakers.TryGetValue(speakerId, out speaker))
                return true;
            speaker = default;
            return false;
        }

        /// <summary>Applies a new reading-speed multiplier; active line duration is recomputed from its start.</summary>
        public void SetReadingSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier > 0f ? multiplier : 1f;
            if (hasActive)
                activeEndTime = activeStartTime + DialogueTiming.ComputeDuration(activeLine.Text, activeLine.DurationOverrideSeconds, speedMultiplier);
        }

        public bool PlayLine(string lineId)
        {
            if (string.IsNullOrEmpty(lineId) || !lines.TryGetValue(lineId, out var line) || !line.IsValid)
                return false;
            float now = Now();
            if (!hasActive)
            {
                StartLine(line, now);
                return true;
            }
            if (IsHigherPriority(line.Priority, activeLine.Priority))
            {
                InterruptActive(line.LineId);
                StartLine(line, now);
                return true;
            }
            queue.Add(new QueuedEntry(line, now));
            return true;
        }

        public bool PlaySequence(string sequenceId)
        {
            if (string.IsNullOrEmpty(sequenceId) || !sequences.TryGetValue(sequenceId, out var seq) || !seq.IsValid)
                return false;
            for (int i = 0; i < seq.LineIds.Length; i++)
                if (!lines.ContainsKey(seq.LineIds[i]))
                    return false;
            if (hasSequence)
                CompleteSequence(false);
            hasSequence = true;
            activeSequence = seq;
            sequenceIndex = 0;
            sequenceCurrentLineId = null;
            sequenceNextTime = Now();
            Tick(sequenceNextTime);
            return true;
        }

        public bool CancelLine(string lineId)
        {
            if (string.IsNullOrEmpty(lineId))
                return false;
            bool removed = false;
            for (int i = queue.Count - 1; i >= 0; i--)
                if (queue[i].Line.LineId == lineId)
                {
                    queue.RemoveAt(i);
                    removed = true;
                }
            if (hasActive && activeLine.LineId == lineId)
            {
                bool wasSequenceLine = hasSequence && sequenceCurrentLineId == lineId;
                hasActive = false;
                if (bus != null) bus.Publish(new DialogueEnded(lineId));
                if (wasSequenceLine)
                    CompleteSequence(false);
                removed = true;
            }
            return removed;
        }

        public void CancelAll()
        {
            queue.Clear();
            if (hasActive)
            {
                string id = activeLine.LineId;
                hasActive = false;
                if (bus != null) bus.Publish(new DialogueEnded(id));
            }
            if (hasSequence)
                CompleteSequence(false);
        }

        /// <summary>Presentation calls this once per frame: expires queued lines, ends elapsed lines, chains sequences.</summary>
        public void Tick(float now)
        {
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                var e = queue[i];
                if (e.Line.ExpiresAfterSeconds > 0f && now - e.EnqueueTime >= e.Line.ExpiresAfterSeconds)
                    queue.RemoveAt(i);
            }
            if (hasActive && now >= activeEndTime)
            {
                string ended = activeLine.LineId;
                hasActive = false;
                if (bus != null) bus.Publish(new DialogueEnded(ended));
                if (hasSequence && sequenceCurrentLineId == ended)
                {
                    sequenceCurrentLineId = null;
                    if (sequenceIndex >= activeSequence.LineIds.Length)
                        CompleteSequence(true);
                    else
                        sequenceNextTime = now + (activeSequence.GapBetweenLinesSeconds > 0f ? activeSequence.GapBetweenLinesSeconds : 0f);
                }
            }
            if (!hasActive)
            {
                if (hasSequence && now >= sequenceNextTime)
                    TryStartNextSequenceLine(now);
                if (!hasActive && !hasSequence)
                    StartHighestPriorityQueued(now);
            }
        }

        private void TryStartNextSequenceLine(float now)
        {
            string[] ids = activeSequence.LineIds;
            if (sequenceIndex >= ids.Length)
            {
                CompleteSequence(true);
                return;
            }
            if (!lines.TryGetValue(ids[sequenceIndex], out var line) || !line.IsValid)
            {
                CompleteSequence(false);
                return;
            }
            if (!hasActive)
            {
                StartLine(line, now);
                sequenceCurrentLineId = line.LineId;
                sequenceIndex++;
            }
            else if (IsHigherPriority(line.Priority, activeLine.Priority))
            {
                InterruptActive(line.LineId);
                StartLine(line, now);
                sequenceCurrentLineId = line.LineId;
                sequenceIndex++;
            }
        }

        private void StartHighestPriorityQueued(float now)
        {
            if (queue.Count == 0)
                return;
            int best = 0;
            for (int i = 1; i < queue.Count; i++)
                if (CompareEntries(queue[i], queue[best]) < 0)
                    best = i;
            var entry = queue[best];
            queue.RemoveAt(best);
            StartLine(entry.Line, now);
        }

        private static int CompareEntries(QueuedEntry a, QueuedEntry b)
        {
            int p = a.Line.Priority.CompareTo(b.Line.Priority);
            if (p != 0) return p;
            return a.EnqueueTime.CompareTo(b.EnqueueTime);
        }

        private static bool IsHigherPriority(DialoguePriority incoming, DialoguePriority active) => incoming < active;

        private void StartLine(DialogueLine line, float now)
        {
            hasActive = true;
            activeLine = line;
            activeStartTime = now;
            activeEndTime = now + DialogueTiming.ComputeDuration(line.Text, line.DurationOverrideSeconds, speedMultiplier);
            if (bus != null) bus.Publish(new DialogueStarted(line.LineId, line.SpeakerId));
        }

        private void InterruptActive(string byLineId)
        {
            string interrupted = activeLine.LineId;
            bool wasSequenceLine = hasSequence && sequenceCurrentLineId == interrupted;
            hasActive = false;
            if (bus != null) bus.Publish(new DialogueInterrupted(interrupted, byLineId));
            if (wasSequenceLine)
                CompleteSequence(false);
        }

        private void CompleteSequence(bool completed)
        {
            string id = activeSequence.SequenceId;
            string onComplete = activeSequence.OnCompleteEventId;
            hasSequence = false;
            sequenceCurrentLineId = null;
            if (bus != null) bus.Publish(new SequenceCompleted(id, completed, onComplete));
        }

        private float Now() => clock != null ? clock.Now : 0f;
    }
}
