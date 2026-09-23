using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Look
{
    // The one look of the render stage, set up by the RenderManager at attach
    public class LookController : MonoBehaviour
    {
        [FormerlySerializedAs("settings")]
        [SerializeField] LookSettings _settings = LookSettings.Default;

        static readonly int shadowTintId = Shader.PropertyToID("_HLShadowTint");
        static readonly int outlineColorId = Shader.PropertyToID("_HLOutlineColor");
        static readonly int fogColorId = Shader.PropertyToID("_HLFogColor");
        static readonly int shadowStrengthId = Shader.PropertyToID("_HLShadowStrength");
        static readonly int toonThresholdId = Shader.PropertyToID("_HLToonThreshold");
        static readonly int outlineWidthPixelsId = Shader.PropertyToID("_HLOutlineWidthPixels");
        static readonly int fogStartId = Shader.PropertyToID("_HLFogStart");
        static readonly int fogEndId = Shader.PropertyToID("_HLFogEnd");
        static readonly int inkStrengthId = Shader.PropertyToID("_HLInkStrength");
        static readonly int keyLightDirId = Shader.PropertyToID("_HLKeyLightDir");
        static readonly int inkSpacingPixelsId = Shader.PropertyToID("_HLInkSpacingPixels");
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
        static readonly int fogBandsId = Shader.PropertyToID("_HLFogBands");
        static readonly int lookAppliedId = Shader.PropertyToID("_HLLookApplied");

        // Tests swap these to see the real write order without a renderer
        static readonly Action<int, float> setGlobalFloat = Shader.SetGlobalFloat;
        static readonly Action<int, Vector4> setGlobalVector = Shader.SetGlobalVector;

        Bounds _board;

        public LookSettings settings { get { return _settings; } set { _settings = value.Validated(); } }

        void OnEnable()
        {
            _settings = _settings.Validated();
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        // Fog and hatching scale follow the camera distance to the board
        public void Init(Camera camera, Bounds board)
        {
            if (camera == null)
            {
                Debug.LogError("[HLLookController] Init needs the camera the look is calibrated for.");
                return;
            }

            _board = board;
            Vector3 position = camera.transform.position;
            Vector2 fog = StageCalibration.BackgroundFog(position, board);
            float depth = Vector3.Distance(position, board.center);
            float spacing = StageCalibration.HatchSpacing(camera, depth, StageCalibration.PortraitHeight);

            LookSettings value = _settings;
            value.fogStart = fog.x;
            value.fogEnd = fog.y;
            value.inkScale = spacing;
            value.inkWidth = spacing * 0.04f;
            value.inkDistStart = fog.x;
            value.inkFarSpacing = spacing * 1.2f;
            settings = value;
            ApplyGlobals();
        }

        // The fog starts past every playable corner seen from wherever the camera moved
        public void UpdateFog(Vector3 cameraPosition)
        {
            Vector2 fog = StageCalibration.BackgroundFog(cameraPosition, _board);
            LookSettings value = _settings;
            value.fogStart = fog.x;
            value.fogEnd = fog.y;
            settings = value;
            ApplyGlobals();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Shader.SetGlobalFloat(lookAppliedId, 0f);
            Shader.SetGlobalVector(keyLightDirId, Vector4.zero);
        }

        void OnValidate()
        {
            _settings = _settings.Validated();
        }

        void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            ApplyGlobals();
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            PublishSunDirection(camera.cullingMask);
        }

        public void ApplyGlobals()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            PublishSunDirection(~0);
            UploadGlobals(in _settings);
        }

        // Same choice as URP: the sun first, otherwise the brightest directional light
        public static Vector4 SelectKeyLightDirection(Light[] lights, Light sun, int cameraMask)
        {
            Light selected = null;
            float brightest = 0f;
            foreach (Light light in lights)
            {
                if (!light || !light.isActiveAndEnabled || light.type != LightType.Directional
                    || (cameraMask & (1 << light.gameObject.layer)) == 0)
                {
                    continue;
                }

                if (light == sun)
                {
                    selected = light;
                    break;
                }

                if (light.intensity > brightest)
                {
                    selected = light;
                    brightest = light.intensity;
                }
            }

            if (!selected)
            {
                return Vector4.zero;
            }

            Vector3 direction = -selected.transform.forward;
            return new Vector4(direction.x, direction.y, direction.z, 0f);
        }

        // Outlines calls this with the main light URP actually culled, before drawing. The outlines live only
        // in the stage renderer, so only the stage publishes.
        public static void PublishMainLightDirection(Light mainLight)
        {
            Vector3 direction = Vector3.zero;
            if (mainLight && mainLight.type == LightType.Directional)
            {
                direction = -mainLight.transform.forward;
            }

            Shader.SetGlobalVector(keyLightDirId, new Vector4(direction.x, direction.y, direction.z, 0f));
        }

        public static Vector4 ToWorkingColor(Color srgb, ColorSpace colorSpace)
        {
            Color color = colorSpace == ColorSpace.Linear ? srgb.linear : srgb;
            return new Vector4(color.r, color.g, color.b, 1f);
        }

        public static void UploadGlobals(in LookSettings settings)
        {
            PublishGlobals(settings, QualitySettings.activeColorSpace, setGlobalFloat, setGlobalVector);
        }

        // Fallback until Outlines publishes the culled main light, allocates nothing
        static void PublishSunDirection(int cameraMask)
        {
            Light sun = RenderSettings.sun;
            bool isVisible = sun && sun.isActiveAndEnabled && (cameraMask & (1 << sun.gameObject.layer)) != 0;
            PublishMainLightDirection(isVisible ? sun : null);
        }

        static void PublishGlobals(LookSettings settings, ColorSpace colorSpace, Action<int, float> setFloat,
                                   Action<int, Vector4> setVector)
        {
            LookSettings value = settings.Validated();
            setVector(shadowTintId, ToWorkingColor(value.shadowTint, colorSpace));
            setVector(outlineColorId, ToWorkingColor(value.outlineColor, colorSpace));
            setVector(fogColorId, ToWorkingColor(value.fogColor, colorSpace));
            setFloat(shadowStrengthId, value.shadowStrength);
            setFloat(toonThresholdId, value.toonThreshold);
            setFloat(outlineWidthPixelsId, value.outlineWidthPixels);
            setFloat(fogStartId, value.fogStart);
            setFloat(fogEndId, value.fogEnd);
            setFloat(inkStrengthId, value.inkStrength);
            setFloat(inkSpacingPixelsId, value.inkSpacingPixels);
            setFloat(inkScaleId, value.inkScale);
            setFloat(inkWidthId, value.inkWidth);
            setFloat(inkStartId, value.inkStart);
            setFloat(inkRangeId, value.inkRange);
            setFloat(densityMulId, value.densityMul);
            setFloat(inkWarpId, value.inkWarp);
            setFloat(inkWarpFreqId, value.inkWarpFreq);
            setFloat(dashAmountId, value.dashAmount);
            setFloat(dashScaleId, value.dashScale);
            setFloat(inkDistStartId, value.inkDistStart);
            setFloat(inkFarSpacingId, value.inkFarSpacing);
            setFloat(fogBandsId, value.fogBands);
            // The flag goes last so no shader reads a half published set
            setFloat(lookAppliedId, 1f);
        }
    }
}
