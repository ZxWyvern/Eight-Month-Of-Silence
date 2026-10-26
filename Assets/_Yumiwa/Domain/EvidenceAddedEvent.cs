namespace Yumiwa.Domain
{
    /// <summary>Raised whenever an evidence entry is added to the log (examine or Take).</summary>
    public readonly struct EvidenceAddedEvent
    {
        public readonly string EvidenceId;

        public EvidenceAddedEvent(string evidenceId)
        {
            EvidenceId = evidenceId;
        }
    }
}
