using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>Presenter driving URP censor materials from config and censor events. ADR-0018: now uses IConfigProvider CensorEffectData not Infrastructure.CensorEffectConfig directly.</summary>
    public sealed class CensorEffectPresenter : MonoBehaviour
    {
        [SerializeField] private List<Material> targetMaterials = new List<Material>();

        private IEventBus bus;
        private ICensorService censor;
        private IConfigProvider configProvider;

        private static readonly int PixelateId = Shader.PropertyToID("_PixelateSize");
        private static readonly int HueId = Shader.PropertyToID("_HueShift");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int LightnessId = Shader.PropertyToID("_LightnessValue");
        private static readonly int ErosionId = Shader.PropertyToID("_MaskErosion");
        private static readonly int ErosionSmoothId = Shader.PropertyToID("_MaskErosionSmooth");
        private static readonly int EmissionId = Shader.PropertyToID("_Emission");
        private static readonly int DepthFadeId = Shader.PropertyToID("_DepthFade");
        private static readonly int AlwaysOnTopId = Shader.PropertyToID("_AlwaysOnTopDeactivated");

        [Inject]
        public void Construct(IEventBus bus, ICensorService censor, IConfigProvider configProvider)
        {
            this.bus = bus;
            this.censor = censor;
            this.configProvider = configProvider;
        }

        private void OnEnable()
        {
            ApplyConfig();
            if (bus != null)
                bus.Subscribe<CensorToggledEvent>(OnCensorToggled);
        }

        private void OnDisable()
        {
            if (bus != null)
                bus.Unsubscribe<CensorToggledEvent>(OnCensorToggled);
        }

        private void OnCensorToggled(CensorToggledEvent e)
        {
            ApplyIntensity(e.Enabled ? e.Intensity : 0f);
        }

        private void ApplyConfig()
        {
            CensorEffectData data = configProvider != null ? configProvider.GetCensor() : CensorEffectData.Defaults();
            foreach (var mat in targetMaterials)
            {
                if (mat == null)
                    continue;
                if (mat.HasProperty(PixelateId)) mat.SetFloat(PixelateId, data.PixelateSize);
                if (mat.HasProperty(HueId)) mat.SetFloat(HueId, data.HueShift);
                if (mat.HasProperty(SaturationId)) mat.SetFloat(SaturationId, data.Saturation);
                if (mat.HasProperty(LightnessId)) mat.SetFloat(LightnessId, data.LightnessValue);
                if (mat.HasProperty(ErosionId)) mat.SetFloat(ErosionId, data.MaskErosion);
                if (mat.HasProperty(ErosionSmoothId)) mat.SetFloat(ErosionSmoothId, data.MaskErosionSmooth);
                if (mat.HasProperty(EmissionId)) mat.SetFloat(EmissionId, data.Emission);
                if (mat.HasProperty(DepthFadeId)) mat.SetFloat(DepthFadeId, data.DepthFade);
                if (mat.HasProperty(AlwaysOnTopId)) mat.SetFloat(AlwaysOnTopId, data.AlwaysOnTopDeactivated);
            }
            if (censor != null)
                ApplyIntensity(censor.Enabled ? censor.Intensity : 0f);
        }

        private void ApplyIntensity(float intensity)
        {
            foreach (var mat in targetMaterials)
            {
                if (mat == null || !mat.HasProperty(EmissionId))
                    continue;
                float baseEmission = configProvider != null ? configProvider.GetCensor().Emission : 1f;
                mat.SetFloat(EmissionId, baseEmission * intensity);
            }
        }
    }
}
