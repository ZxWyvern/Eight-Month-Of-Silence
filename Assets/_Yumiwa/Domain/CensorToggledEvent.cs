namespace Yumiwa.Domain
{
    /// <summary>Raised whenever censor VFX enabled state or intensity changes.</summary>
    public readonly struct CensorToggledEvent
    {
        public readonly bool Enabled;
        public readonly float Intensity;

        public CensorToggledEvent(bool enabled, float intensity)
        {
            Enabled = enabled;
            Intensity = intensity;
        }
    }
}
