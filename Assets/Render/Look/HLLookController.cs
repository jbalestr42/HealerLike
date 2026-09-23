using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Look
{
    public class HLLookController : MonoBehaviour
    {
        [SerializeField] HLLookSettings settings = HLLookSettings.Default;
        static HLLookController owner;
        static readonly int ShadowTintId = Shader.PropertyToID("_HLShadowTint");
        static readonly int OutlineColorId = Shader.PropertyToID("_HLOutlineColor");
        static readonly int FogColorId = Shader.PropertyToID("_HLFogColor");
        static readonly int ShadowStrengthId = Shader.PropertyToID("_HLShadowStrength");
        static readonly int ToonThresholdId = Shader.PropertyToID("_HLToonThreshold");
        static readonly int OutlineWidthPixelsId = Shader.PropertyToID("_HLOutlineWidthPixels");
        static readonly int FogStartId = Shader.PropertyToID("_HLFogStart");
        static readonly int FogEndId = Shader.PropertyToID("_HLFogEnd");
        static readonly int InkStrengthId = Shader.PropertyToID("_HLInkStrength");
        static readonly int KeyLightDirId = Shader.PropertyToID("_HLKeyLightDir");
        static readonly int InkSpacingPixelsId = Shader.PropertyToID("_HLInkSpacingPixels");
        static readonly int InkScaleId = Shader.PropertyToID("_HLInkScale");
        static readonly int InkWidthId = Shader.PropertyToID("_HLInkWidth");
        static readonly int InkStartId = Shader.PropertyToID("_HLInkStart");
        static readonly int InkRangeId = Shader.PropertyToID("_HLInkRange");
        static readonly int DensityMulId = Shader.PropertyToID("_HLDensityMul");
        static readonly int InkWarpId = Shader.PropertyToID("_HLInkWarp");
        static readonly int InkWarpFreqId = Shader.PropertyToID("_HLInkWarpFreq");
        static readonly int DashAmountId = Shader.PropertyToID("_HLDashAmount");
        static readonly int DashScaleId = Shader.PropertyToID("_HLDashScale");
        static readonly int InkDistStartId = Shader.PropertyToID("_HLInkDistStart");
        static readonly int InkFarSpacingId = Shader.PropertyToID("_HLInkFarSpacing");
        static readonly int FogBandsId = Shader.PropertyToID("_HLFogBands");
        static readonly int LookAppliedId = Shader.PropertyToID("_HLLookApplied");

        static readonly Action<int, float> SetFloat = Shader.SetGlobalFloat;
        static readonly Action<int, Vector4> SetVector = Shader.SetGlobalVector;

        public HLLookSettings Settings { get => settings; set => settings = value.Validated(); }

        void OnEnable()
        {
            settings = settings.Validated();
            if (owner != null && owner != this)
            {
                Debug.LogWarning("HLLookController already has an active owner; this controller remains inactive.", this);
                return;
            }
            owner = this;
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (owner != this) return;
            owner = null;
            Shader.SetGlobalFloat(LookAppliedId, 0f);
            Shader.SetGlobalVector(KeyLightDirId, Vector4.zero);
        }

        void OnValidate() => settings = settings.Validated();

        void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras) => ApplyGlobals();

        public void ApplyGlobals()
        {
            if (owner != this || !isActiveAndEnabled) return;
            PublishSunDirection(~0);
            UploadGlobals(in settings);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (owner != this || !isActiveAndEnabled) return;
            PublishSunDirection(camera.cullingMask);
        }

        static void PublishSunDirection(int cameraMask)
        {
            var sun = RenderSettings.sun;
            PublishMainLightDirection(sun && sun.isActiveAndEnabled &&
                (cameraMask & (1 << sun.gameObject.layer)) != 0 ? sun : null);
        }

        public static Vector4 SelectKeyLightDirection(Light[] lights, Light sun, int cameraMask)
        {
            Light selected = null;
            float brightest = 0f;
            foreach (var light in lights)
            {
                if (!light || !light.isActiveAndEnabled || light.type != LightType.Directional ||
                    (cameraMask & (1 << light.gameObject.layer)) == 0) continue;
                if (light == sun) { selected = light; break; }
                if (light.intensity > brightest) { selected = light; brightest = light.intensity; }
            }
            if (!selected) return Vector4.zero;
            Vector3 direction = -selected.transform.forward;
            return new Vector4(direction.x, direction.y, direction.z, 0f);
        }

        public static void PublishMainLightDirection(Light mainLight)
        {
            if (!owner || !owner.isActiveAndEnabled) return;
            Vector3 direction = mainLight && mainLight.type == LightType.Directional ? -mainLight.transform.forward : Vector3.zero;
            Shader.SetGlobalVector(KeyLightDirId, new Vector4(direction.x, direction.y, direction.z, 0));
        }

        public static Vector4 ToWorkingColor(Color srgb, ColorSpace colorSpace)
        {
            Color color = colorSpace == ColorSpace.Linear ? srgb.linear : srgb;
            return new Vector4(color.r, color.g, color.b, 1f);
        }

        public static void UploadGlobals(in HLLookSettings settings) =>
            PublishGlobals(settings, QualitySettings.activeColorSpace, SetFloat, SetVector);

        static void PublishGlobals(HLLookSettings settings, ColorSpace colorSpace,
            Action<int, float> setFloat, Action<int, Vector4> setVector)
        {
            var value = settings.Validated();
            setVector(ShadowTintId, ToWorkingColor(value.ShadowTint, colorSpace));
            setVector(OutlineColorId, ToWorkingColor(value.OutlineColor, colorSpace));
            setVector(FogColorId, ToWorkingColor(value.FogColor, colorSpace));
            setFloat(ShadowStrengthId, value.ShadowStrength);
            setFloat(ToonThresholdId, value.ToonThreshold);
            setFloat(OutlineWidthPixelsId, value.OutlineWidthPixels);
            setFloat(FogStartId, value.FogStart);
            setFloat(FogEndId, value.FogEnd);
            setFloat(InkStrengthId, value.InkStrength);
            setFloat(InkSpacingPixelsId, value.InkSpacingPixels);
            setFloat(InkScaleId, value.InkScale);
            setFloat(InkWidthId, value.InkWidth);
            setFloat(InkStartId, value.InkStart);
            setFloat(InkRangeId, value.InkRange);
            setFloat(DensityMulId, value.DensityMul);
            setFloat(InkWarpId, value.InkWarp);
            setFloat(InkWarpFreqId, value.InkWarpFreq);
            setFloat(DashAmountId, value.DashAmount);
            setFloat(DashScaleId, value.DashScale);
            setFloat(InkDistStartId, value.InkDistStart);
            setFloat(InkFarSpacingId, value.InkFarSpacing);
            setFloat(FogBandsId, value.FogBands);
            setFloat(LookAppliedId, 1f);
        }
    }
}
