using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Look;

namespace HealerLike.Render.Studio.Editor
{
    // The studio's own look globals for the length of one preview render. The stage may publish its look at
    // beginFrameRendering, so they are applied again when the preview camera begins, and End puts back every
    // global it touched.
    public class StudioLookScope
    {
        static readonly string[] floatNames =
        {
            "_HLShadowStrength", "_HLToonThreshold", "_HLToonSoftness", "_HLOutlineWidthPixels",
            "_HLFogStart", "_HLFogEnd", "_HLFogBands", "_HLInkStrength", "_HLInkScale", "_HLInkWidth",
            "_HLInkStart", "_HLInkRange", "_HLDensityMul", "_HLInkWarp", "_HLInkWarpFreq", "_HLDashAmount",
            "_HLDashScale", "_HLInkDistStart", "_HLInkFarSpacing", "_HLContrast", "_HLLookApplied"
        };
        static readonly float[] floatValues =
        {
            0.32f, 0.38f, 0.14f, 1f, 500f, 1000f, 6f, 0.16f, 0.075f, 0.0001f,
            0.65f, 1f, 1f, 0.025f, 2.44f, 0.1f, 0.01f, 100f, 0.6f, 1f, 1f
        };
        static readonly string[] vectorNames = { "_HLShadowTint", "_HLOutlineColor", "_HLFogColor" };
        static readonly Color shadowTint = new Color(0.18f, 0.27f, 0.31f);
        static readonly Color outlineColour = new Color(0.08f, 0.12f, 0.16f);
        static readonly Color fogColour = new Color(0.075f, 0.095f, 0.115f);

        readonly float[] _previousFloats = new float[floatNames.Length];
        readonly Vector4[] _previousVectors = new Vector4[vectorNames.Length];
        Camera _camera;

        public void Begin(Camera camera)
        {
            _camera = camera;
            for (int i = 0; i < floatNames.Length; i++)
            {
                _previousFloats[i] = Shader.GetGlobalFloat(floatNames[i]);
            }

            for (int i = 0; i < vectorNames.Length; i++)
            {
                _previousVectors[i] = Shader.GetGlobalVector(vectorNames[i]);
            }

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            Apply();
        }

        public void End()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            for (int i = 0; i < floatNames.Length; i++)
            {
                Shader.SetGlobalFloat(floatNames[i], _previousFloats[i]);
            }

            for (int i = 0; i < vectorNames.Length; i++)
            {
                Shader.SetGlobalVector(vectorNames[i], _previousVectors[i]);
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                Apply();
            }
        }

        static void Apply()
        {
            for (int i = 0; i < floatNames.Length; i++)
            {
                Shader.SetGlobalFloat(floatNames[i], floatValues[i]);
            }

            ColorSpace space = QualitySettings.activeColorSpace;
            Shader.SetGlobalVector("_HLShadowTint", LookController.ToWorkingColor(shadowTint, space));
            Shader.SetGlobalVector("_HLOutlineColor", LookController.ToWorkingColor(outlineColour, space));
            Shader.SetGlobalVector("_HLFogColor", LookController.ToWorkingColor(fogColour, space));
        }
    }
}
