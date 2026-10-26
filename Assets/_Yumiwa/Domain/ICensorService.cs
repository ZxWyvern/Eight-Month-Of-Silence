namespace Yumiwa.Domain
{
    /// <summary>Service contract for anatomical censor VFX state (no UnityEngine dependency).</summary>
    public interface ICensorService
    {
        bool Enabled { get; }
        float Intensity { get; }
        void SetEnabled(bool enabled);
        void SetIntensity(float intensity);
    }
}
