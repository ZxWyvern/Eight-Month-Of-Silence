namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure ordered dialogue sequence per plan.json — lines play back-to-back with
    /// <see cref="GapBetweenLinesSeconds"/> between them. OnCompleteEventId is carried
    /// in <see cref="SequenceCompleted"/> so subscribers can chain consequences.
    /// </summary>
    public readonly struct DialogueSequence
    {
        public readonly string SequenceId;
        public readonly string[] LineIds;
        public readonly float GapBetweenLinesSeconds;
        public readonly string OnCompleteEventId;

        public DialogueSequence(string sequenceId, string[] lineIds, float gapBetweenLinesSeconds, string onCompleteEventId = null)
        {
            SequenceId = sequenceId;
            LineIds = lineIds;
            GapBetweenLinesSeconds = gapBetweenLinesSeconds;
            OnCompleteEventId = onCompleteEventId;
        }

        public bool IsValid => !string.IsNullOrEmpty(SequenceId) && LineIds != null && LineIds.Length > 0;
    }
}
