using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Look
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class HLLookController : MonoBehaviour
    {
        [SerializeField] private HLLookSettings settings = HLLookSettings.Default;
        private static HLLookController owner;
        private static readonly int ShadowTintId = Shader.PropertyToID("_HLShadowTint");
        private static readonly int OutlineColorId = Shader.PropertyToID("_HLOutlineColor");
        private static readonly int FogColorId = Shader.PropertyToID("_HLFogColor");
        private static readonly int ShadowStrengthId = Shader.PropertyToID("_HLShadowStrength");
        private static readonly int ToonThresholdId = Shader.PropertyToID("_HLToonThreshold");
        private static readonly int OutlineWidthPixelsId = Shader.PropertyToID("_HLOutlineWidthPixels");
        private static readonly int FogStartId = Shader.PropertyToID("_HLFogStart");
        private static readonly int FogEndId = Shader.PropertyToID("_HLFogEnd");
        private static readonly int InkStrengthId = Shader.PropertyToID("_HLInkStrength");
        private static readonly int InkScaleId = Shader.PropertyToID("_HLInkScale");
        private static readonly int InkWidthId = Shader.PropertyToID("_HLInkWidth");
        private static readonly int InkStartId = Shader.PropertyToID("_HLInkStart");
        private static readonly int InkRangeId = Shader.PropertyToID("_HLInkRange");
        private static readonly int DensityMulId = Shader.PropertyToID("_HLDensityMul");
        private static readonly int InkWarpId = Shader.PropertyToID("_HLInkWarp");
        private static readonly int InkWarpFreqId = Shader.PropertyToID("_HLInkWarpFreq");
        private static readonly int DashAmountId = Shader.PropertyToID("_HLDashAmount");
        private static readonly int DashScaleId = Shader.PropertyToID("_HLDashScale");
        private static readonly int InkDistStartId = Shader.PropertyToID("_HLInkDistStart");
        private static readonly int InkFarSpacingId = Shader.PropertyToID("_HLInkFarSpacing");
        private static readonly int FogBandsId = Shader.PropertyToID("_HLFogBands");
        private static readonly int LookAppliedId = Shader.PropertyToID("_HLLookApplied");

        public HLLookSettings Settings { get => settings; set => settings = value.Validated(); }

        private void OnEnable()
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
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            if (owner != this) return;
            owner = null;
            Shader.SetGlobalFloat(LookAppliedId, 0f);
        }

        private void OnValidate() => settings = settings.Validated();

        private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras) => ApplyGlobals();

        public void ApplyGlobals()
        {
            if (owner == this && isActiveAndEnabled) UploadGlobals(in settings);
        }

        public static Vector4 ToWorkingColor(Color srgb, ColorSpace colorSpace)
        {
            Color color = colorSpace == ColorSpace.Linear ? srgb.linear : srgb;
            return new Vector4(color.r, color.g, color.b, 1f);
        }

        // Explicit tooling entry point; ordinary frame publication belongs to the active owner.
        public static void UploadGlobals(in HLLookSettings settings) =>
            PublishGlobals(settings, QualitySettings.activeColorSpace, Shader.SetGlobalFloat, Shader.SetGlobalVector);

        // Isolated publication seam lets tests observe the real write ordering without a renderer.
        private static void PublishGlobals(HLLookSettings settings, ColorSpace colorSpace,
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
            // Publish the complete set before selecting it in any shader.
            setFloat(LookAppliedId, 1f);
        }
    }
}
