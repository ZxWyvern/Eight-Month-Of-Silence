namespace Yumiwa.Domain
{
    /// <summary>Typed dialogue bus events per plan.json gameplay_integration (struct-only for IEventBus).</summary>
    public readonly struct DialogueStarted
    {
        public readonly string LineId;
        public readonly string SpeakerId;
        public DialogueStarted(string lineId, string speakerId) { LineId = lineId; SpeakerId = speakerId; }
    }

    public readonly struct DialogueEnded
    {
        public readonly string LineId;
        public DialogueEnded(string lineId) => LineId = lineId;
    }

    public readonly struct DialogueInterrupted
    {
        public readonly string LineId;
        public readonly string ByLineId;
        public DialogueInterrupted(string lineId, string byLineId) { LineId = lineId; ByLineId = byLineId; }
    }

    public readonly struct SequenceCompleted
    {
        public readonly string SequenceId;
        public readonly bool Completed;
        public readonly string OnCompleteEventId;
        public SequenceCompleted(string sequenceId, bool completed, string onCompleteEventId)
        {
            SequenceId = sequenceId;
            Completed = completed;
            OnCompleteEventId = onCompleteEventId;
        }
    }
}
