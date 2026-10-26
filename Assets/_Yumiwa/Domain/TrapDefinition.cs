namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure trap definition per GDD §03 Horror / §08 Audio learnable cues.
    /// Trap cue must be learnable (consistent pattern), not random jumpscare.
    /// </summary>
    public readonly struct TrapDefinition
    {
        public readonly string TrapId;
        public readonly string DisplayName;
        public readonly float Damage;
        public readonly string AudioCueId;
        public readonly string VisualCueId;
        public readonly bool IsLearnable;

        public TrapDefinition(string trapId, string displayName, float damage, string audioCueId, string visualCueId, bool isLearnable = true)
        {
            TrapId = trapId;
            DisplayName = displayName;
            Damage = damage;
            AudioCueId = audioCueId;
            VisualCueId = visualCueId;
            IsLearnable = isLearnable;
        }

        public bool IsValid => !string.IsNullOrEmpty(TrapId);
    }
}
