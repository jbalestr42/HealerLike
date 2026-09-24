using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Look
{
    // The one look of the render stage, set up by the RenderManager at attach
    public class LookController : MonoBehaviour
    {
        [SerializeField] LookSettings _settings = LookSettings.Default;

        static readonly int shadowTintId = Shader.PropertyToID("_HLShadowTint");
        static readonly int outlineColorId = Shader.PropertyToID("_HLOutlineColor");
        static readonly int fogColorId = Shader.PropertyToID("_HLFogColor");
        static readonly int shadowStrengthId = Shader.PropertyToID("_HLShadowStrength");
        static readonly int toonThresholdId = Shader.PropertyToID("_HLToonThreshold");
        static readonly int toonSoftnessId = Shader.PropertyToID("_HLToonSoftness");
        static readonly int outlineWidthPixelsId = Shader.PropertyToID("_HLOutlineWidthPixels");
        static readonly int fogStartId = Shader.PropertyToID("_HLFogStart");
        static readonly int fogEndId = Shader.PropertyToID("_HLFogEnd");
        static readonly int inkStrengthId = Shader.PropertyToID("_HLInkStrength");
        static readonly int inkScaleId = Shader.PropertyToID("_HLInkScale");
        static readonly int inkWidthId = Shader.PropertyToID("_HLInkWidth");
        static readonly int inkStartId = Shader.PropertyToID("_HLInkStart");
        static readonly int inkRangeId = Shader.PropertyToID("_HLInkRange");
        static readonly int densityMulId = Shader.PropertyToID("_HLDensityMul");
        static readonly int inkWarpId = Shader.PropertyToID("_HLInkWarp");
        static readonly int inkWarpFreqId = Shader.PropertyToID("_HLInkWarpFreq");
        static readonly int dashAmountId = Shader.PropertyToID("_HLDashAmount");
        static readonly int dashScaleId = Shader.PropertyToID("_HLDashScale");
        static readonly int inkDistStartId = Shader.PropertyToID("_HLInkDistStart");
        static readonly int inkFarSpacingId = Shader.PropertyToID("_HLInkFarSpacing");
        static readonly int contrastId = Shader.PropertyToID("_HLContrast");
        static readonly int fogBandsId = Shader.PropertyToID("_HLFogBands");
        static readonly int lookAppliedId = Shader.PropertyToID("_HLLookApplied");

        public LookSettings settings { get { return _settings; } set { _settings = value.Validated(); } }

        void OnEnable()
        {
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
        }

        // The fog range comes from the camera distance to the board, the hatch keeps its authored world spacing
        public void Init(Vector2 fogRange)
        {
            UpdateFog(fogRange);
        }

        // The fog starts past every playable corner seen from wherever the camera moved
        public void UpdateFog(Vector2 fogRange)
        {
            LookSettings value = _settings;
            value.fogStart = fogRange.x;
            value.fogEnd = fogRange.y;
            settings = value;
            ApplyGlobals();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            Shader.SetGlobalFloat(lookAppliedId, 0f);
        }

        void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            ApplyGlobals();
        }

        public void ApplyGlobals()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            PublishGlobals(_settings, QualitySettings.activeColorSpace);
        }

        public static Vector4 ToWorkingColor(Color srgb, ColorSpace colorSpace)
        {
            Color color = colorSpace == ColorSpace.Linear ? srgb.linear : srgb;
            return new Vector4(color.r, color.g, color.b, 1f);
        }

        // The settings setter already validated these
        static void PublishGlobals(LookSettings value, ColorSpace colorSpace)
        {
            Shader.SetGlobalVector(shadowTintId, ToWorkingColor(value.shadowTint, colorSpace));
            Shader.SetGlobalVector(outlineColorId, ToWorkingColor(value.outlineColor, colorSpace));
            Shader.SetGlobalVector(fogColorId, ToWorkingColor(value.fogColor, colorSpace));
            Shader.SetGlobalFloat(shadowStrengthId, value.shadowStrength);
            Shader.SetGlobalFloat(toonThresholdId, value.toonThreshold);
            Shader.SetGlobalFloat(toonSoftnessId, value.toonSoftness);
            Shader.SetGlobalFloat(outlineWidthPixelsId, value.outlineWidthPixels);
            Shader.SetGlobalFloat(fogStartId, value.fogStart);
            Shader.SetGlobalFloat(fogEndId, value.fogEnd);
            Shader.SetGlobalFloat(inkStrengthId, value.inkStrength);
            Shader.SetGlobalFloat(inkScaleId, value.inkScale);
            Shader.SetGlobalFloat(inkWidthId, value.inkWidth);
            Shader.SetGlobalFloat(inkStartId, value.inkStart);
            Shader.SetGlobalFloat(inkRangeId, value.inkRange);
            Shader.SetGlobalFloat(densityMulId, value.densityMul);
            Shader.SetGlobalFloat(inkWarpId, value.inkWarp);
            Shader.SetGlobalFloat(inkWarpFreqId, value.inkWarpFreq);
            Shader.SetGlobalFloat(dashAmountId, value.dashAmount);
            Shader.SetGlobalFloat(dashScaleId, value.dashScale);
            Shader.SetGlobalFloat(inkDistStartId, value.inkDistStart);
            Shader.SetGlobalFloat(inkFarSpacingId, value.inkFarSpacing);
            Shader.SetGlobalFloat(contrastId, value.contrast);
            Shader.SetGlobalFloat(fogBandsId, value.fogBands);
            // The flag goes last so no shader reads a half published set
            Shader.SetGlobalFloat(lookAppliedId, 1f);
        }
    }
}
