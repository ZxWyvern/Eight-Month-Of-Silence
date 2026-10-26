namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure dialogue line definition per plan.json data model.
    /// DurationOverrideSeconds &lt;= 0 means auto timing via <see cref="DialogueTiming"/>.
    /// ExpiresAfterSeconds &lt;= 0 means a queued line never expires.
    /// ResumableIfInterrupted is reserved data (CLARIFY decision: v1 always drops
    /// interrupted lines per GDD-005 anti-softlock); the flag is carried, not honored.
    /// </summary>
    public readonly struct DialogueLine
    {
        public readonly string LineId;
        public readonly string SpeakerId;
        public readonly string Text;
        public readonly DialoguePriority Priority;
        public readonly float DurationOverrideSeconds;
        public readonly float ExpiresAfterSeconds;
        public readonly bool ResumableIfInterrupted;

        public DialogueLine(
            string lineId,
            string speakerId,
            string text,
            DialoguePriority priority,
            float durationOverrideSeconds = 0f,
            float expiresAfterSeconds = 0f,
            bool resumableIfInterrupted = false)
        {
            LineId = lineId;
            SpeakerId = speakerId;
            Text = text;
            Priority = priority;
            DurationOverrideSeconds = durationOverrideSeconds;
            ExpiresAfterSeconds = expiresAfterSeconds;
            ResumableIfInterrupted = resumableIfInterrupted;
        }

        public bool IsValid => !string.IsNullOrEmpty(LineId) && !string.IsNullOrEmpty(Text);
    }
}
