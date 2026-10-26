namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure text-based timing per plan.json reading_speed_formula.
    /// Spec constants (not TBD-6 balancing): base 1.0s + 0.06s/char, clamped 1.5–6.0s,
    /// then scaled by the reading-speed multiplier (Slow 1.3 / Normal 1.0 / Fast 0.75).
    /// Allocation-free: no string building, pure arithmetic.
    /// </summary>
    public static class DialogueTiming
    {
        public const float BaseDurationSeconds = 1.0f;
        public const float SecondsPerCharacter = 0.06f;
        public const float MinDurationSeconds = 1.5f;
        public const float MaxDurationSeconds = 6.0f;

        public static float ComputeDuration(string text, float durationOverrideSeconds, float speedMultiplier)
        {
            float raw = durationOverrideSeconds > 0f
                ? durationOverrideSeconds
                : BaseDurationSeconds + SecondsPerCharacter * (string.IsNullOrEmpty(text) ? 0 : text.Length);
            float clamped = raw < MinDurationSeconds ? MinDurationSeconds
                : raw > MaxDurationSeconds ? MaxDurationSeconds : raw;
            float mult = speedMultiplier > 0f ? speedMultiplier : 1f;
            return clamped * mult;
        }

        public static float ComputeDuration(string text, float durationOverrideSeconds, DialogueReadingSpeed speed) =>
            ComputeDuration(text, durationOverrideSeconds, speed.ToMultiplier());
    }
}
