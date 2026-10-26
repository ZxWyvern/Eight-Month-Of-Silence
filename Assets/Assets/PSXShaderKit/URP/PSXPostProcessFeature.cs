using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

namespace PSXShaderKit.URP
{
    /// <summary>URP port of the kit's fullscreen look: pixelation, color depth, dither, interlace.</summary>
    public sealed class PSXPostProcessFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public sealed class Settings
        {
            [Range(0.05f, 1f)]
            [Tooltip("1 = off. Lower = chunkier pixels.")]
            public float pixelationFactor = 0.5f;
            [Tooltip("Values per color channel. 256 matches the kit default.")]
            public Vector3 colorDepth = new Vector3(256f, 256f, 256f);
            [Tooltip("Dithered output depth. 32 matches the kit default.")]
            public Vector3 ditherDepth = new Vector3(32f, 32f, 32f);
            [Range(0f, 1f)]
            [Tooltip("0 = 2x2, 0.5 = 4x4, 1 = PS1 4x4 pattern.")]
            public float ditherPattern = 1f;
            [Range(0.25f, 4f)]
            public float ditheringScale = 1f;
            [Range(0, 8)]
            [Tooltip("0 = off. 1 is console-accurate at low resolutions.")]
            public int interlaceSize = 1;
            [Tooltip("On = fixed CRT scanlines. Off = temporal interlace flicker (kit behavior).")]
            public bool interlaceStatic = true;
            public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public Settings settings = new Settings();
        public Shader postShader;

        private PSXPostProcessPass pass;

        public override void Create()
        {
            pass = new PSXPostProcessPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (postShader == null || pass == null)
                return;
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;
            pass.Setup(postShader, settings);
            pass.renderPassEvent = settings.passEvent;
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
        }

        private sealed class PSXPostProcessPass : ScriptableRenderPass
        {
            private static readonly int PixelationId = Shader.PropertyToID("_PixelationFactor");
            private static readonly int ColorResId = Shader.PropertyToID("_ColorResolution");
            private static readonly int DitherResId = Shader.PropertyToID("_DitherResolution");
            private static readonly int DitherMatrixId = Shader.PropertyToID("_HighResDitherMatrix");
            private static readonly int DitherScaleId = Shader.PropertyToID("_DitheringScale");
            private static readonly int InterlaceSizeId = Shader.PropertyToID("_InterlacingSize");
            private static readonly int InterlaceFrameId = Shader.PropertyToID("_InterlacedFrameIndex");
            private static readonly int InterlaceStaticId = Shader.PropertyToID("_InterlaceStatic");
            private static readonly int PrevFrameId = Shader.PropertyToID("_PreviousFrame");
            private static readonly int HistoryValidId = Shader.PropertyToID("_HistoryValid");

            private Material material;
            private Settings settings;
            private RTHandle history;
            private bool historyReady;

            public void Setup(Shader shader, Settings settings)
            {
                this.settings = settings;
                if (material == null || material.shader != shader)
                {
                    CoreUtils.Destroy(material);
                    material = CoreUtils.CreateEngineMaterial(shader);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (cameraData.cameraType != CameraType.Game)
                    return;
                if (material == null || settings == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle src = resourceData.activeColorTexture;

                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                RenderingUtils.ReAllocateHandleIfNeeded(
                    ref history, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_PSXHistory");
                TextureHandle historyTex = renderGraph.ImportTexture(history);
                TextureHandle temp = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, desc, "_PSXTemp", false);

                material.SetFloat(PixelationId, settings.pixelationFactor);
                material.SetVector(ColorResId, settings.colorDepth);
                material.SetVector(DitherResId, settings.ditherDepth);
                material.SetFloat(DitherMatrixId, settings.ditherPattern);
                material.SetFloat(DitherScaleId, settings.ditheringScale);
                material.SetFloat(InterlaceSizeId, settings.interlaceSize);
                material.SetFloat(InterlaceFrameId, Time.frameCount % 2);
                material.SetFloat(InterlaceStaticId, settings.interlaceStatic ? 1f : 0f);
                material.SetFloat(HistoryValidId, historyReady ? 1f : 0f);

                var blitParams = new BlitMaterialParameters(src, temp, material, 0);
                renderGraph.AddBlitPass(blitParams, passName: "PSX Post Effect");

                using (var builder = renderGraph.AddBlitPass(
                    src, historyTex, Vector2.one, Vector2.zero, returnBuilder: true, passName: "PSX History Update"))
                {
                    builder.SetGlobalTextureAfterPass(historyTex, PrevFrameId);
                }
                historyReady = true;

                renderGraph.AddBlitPass(temp, src, Vector2.one, Vector2.zero, passName: "PSX Composite");
            }

            public void Dispose()
            {
                history?.Release();
                history = null;
                historyReady = false;
            }
        }
    }
}
