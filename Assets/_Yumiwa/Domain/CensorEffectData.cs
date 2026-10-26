namespace Yumiwa.Domain
{
    /// <summary>Plain-data censor effect tuning snapshot (no UnityEngine dependency).</summary>
    public readonly struct CensorEffectData
    {
        public readonly float PixelateSize;
        public readonly float HueShift;
        public readonly float Saturation;
        public readonly float LightnessValue;
        public readonly float MaskErosion;
        public readonly float MaskErosionSmooth;
        public readonly float Emission;
        public readonly float DepthFade;
        public readonly float AlwaysOnTopDeactivated;

        public CensorEffectData(
            float pixelateSize, float hueShift, float saturation, float lightnessValue,
            float maskErosion, float maskErosionSmooth, float emission, float depthFade,
            float alwaysOnTopDeactivated)
        {
            PixelateSize = pixelateSize;
            HueShift = hueShift;
            Saturation = saturation;
            LightnessValue = lightnessValue;
            MaskErosion = maskErosion;
            MaskErosionSmooth = maskErosionSmooth;
            Emission = emission;
            DepthFade = depthFade;
            AlwaysOnTopDeactivated = alwaysOnTopDeactivated;
        }

        public static CensorEffectData Defaults() => new CensorEffectData(
            0.03f, 0f, 1f, 1f, 0f, 0.01f, 1f, 0f, 0f);
    }
}
