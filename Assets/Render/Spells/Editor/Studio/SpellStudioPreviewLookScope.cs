using System;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Look;

namespace HealerLike.Render.Spells.Editor.Studio
{
    // A render-only scope: stage callbacks may publish their look at beginFrameRendering,
    // so apply again for this camera and restore every touched global in Dispose.
    public sealed class SpellStudioPreviewLookScope : IDisposable
    {
        static readonly string[] FloatNames =
        {
            "_HLShadowStrength", "_HLToonThreshold", "_HLToonSoftness", "_HLOutlineWidthPixels",
            "_HLFogStart", "_HLFogEnd", "_HLFogBands", "_HLInkStrength", "_HLInkScale", "_HLInkWidth",
            "_HLInkStart", "_HLInkRange", "_HLDensityMul", "_HLInkWarp", "_HLInkWarpFreq", "_HLDashAmount",
            "_HLDashScale", "_HLInkDistStart", "_HLInkFarSpacing", "_HLContrast", "_HLLookApplied"
        };
        static readonly float[] Values =
        {
            .32f, .38f, .14f, 1f, 500f, 1000f, 6f, .16f, .075f, .0001f,
            .65f, 1f, 1f, .025f, 2.44f, .1f, .01f, 100f, .6f, 1f, 1f
        };
        static readonly string[] VectorNames = { "_HLShadowTint", "_HLOutlineColor", "_HLFogColor" };
        readonly float[] _previousFloats = new float[FloatNames.Length];
        readonly Vector4[] _previousVectors = new Vector4[VectorNames.Length];
        readonly Camera _camera;

        public SpellStudioPreviewLookScope(Camera camera)
        {
            _camera = camera;
            for (int i = 0; i < FloatNames.Length; i++) _previousFloats[i] = Shader.GetGlobalFloat(FloatNames[i]);
            for (int i = 0; i < VectorNames.Length; i++) _previousVectors[i] = Shader.GetGlobalVector(VectorNames[i]);
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            Apply();
        }

        void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera) Apply();
        }

        static void Apply()
        {
            for (int i = 0; i < FloatNames.Length; i++) Shader.SetGlobalFloat(FloatNames[i], Values[i]);
            Shader.SetGlobalVector("_HLShadowTint", LookController.ToWorkingColor(new Color(.18f, .27f, .31f), QualitySettings.activeColorSpace));
            Shader.SetGlobalVector("_HLOutlineColor", LookController.ToWorkingColor(new Color(.08f, .12f, .16f), QualitySettings.activeColorSpace));
            Shader.SetGlobalVector("_HLFogColor", LookController.ToWorkingColor(new Color(.075f, .095f, .115f), QualitySettings.activeColorSpace));
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            for (int i = 0; i < FloatNames.Length; i++) Shader.SetGlobalFloat(FloatNames[i], _previousFloats[i]);
            for (int i = 0; i < VectorNames.Length; i++) Shader.SetGlobalVector(VectorNames[i], _previousVectors[i]);
        }
    }
}
