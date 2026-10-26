namespace Yumiwa.Domain
{
    /// <summary>Player subtitle text size per plan.json (Small / Medium / Large).</summary>
    public enum DialogueTextSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    /// <summary>Player reading-speed preference per plan.json. Multiplier scales line duration.</summary>
    public enum DialogueReadingSpeed
    {
        Slow = 0,
        Normal = 1,
        Fast = 2
    }

    public static class DialogueReadingSpeedExtensions
    {
        /// <summary>Plan-spec multipliers: Slow 1.3 / Normal 1.0 / Fast 0.75.</summary>
        public static float ToMultiplier(this DialogueReadingSpeed speed)
        {
            switch (speed)
            {
                case DialogueReadingSpeed.Slow: return 1.3f;
                case DialogueReadingSpeed.Fast: return 0.75f;
                default: return 1.0f;
            }
        }
    }
}
