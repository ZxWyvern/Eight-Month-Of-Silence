using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>ScriptableObject censor effect tunables; defaults mirror the BIRP material.</summary>
    [CreateAssetMenu(menuName = "Yumiwa/CensorEffectConfig", fileName = "CensorEffectConfig")]
    public sealed class CensorEffectConfig : ScriptableObject
    {
        [Range(0f, 1f)] public float PixelateSize = 0.03f;
        public float HueShift;
        public float Saturation = 1f;
        public float LightnessValue = 1f;
        [Range(0f, 1f)] public float MaskErosion;
        public float MaskErosionSmooth = 0.01f;
        public float Emission = 1f;
        public float DepthFade;
        [Range(0f, 1f)] public float AlwaysOnTopDeactivated;

        public CensorEffectData ToData() => new CensorEffectData(
            PixelateSize, HueShift, Saturation, LightnessValue,
            MaskErosion, MaskErosionSmooth, Emission, DepthFade,
            AlwaysOnTopDeactivated);
    }
}
