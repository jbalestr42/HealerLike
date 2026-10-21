using System;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Look;

namespace HealerLike.Render.Studio.Editor
{
    // The studio's own look globals for the length of one preview render. The stage may publish its look at
    // beginFrameRendering, so they are applied again when the preview camera begins, and End puts back every
    // global it touched.
    public class StudioLookScope : IDisposable
    {
        static readonly LookSettings settings = CreateSettings();
        LookShaderProperties.Snapshot _previous;
        Camera _camera;
        bool _isActive;

        public void Begin(Camera camera)
        {
            if (_isActive)
            {
                return;
            }
            _isActive = true;
            _camera = camera;
            _previous = LookShaderProperties.Capture();

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            Apply();
        }

        public void Dispose()
        {
            End();
        }

        public void End()
        {
            if (!_isActive)
            {
                return;
            }
            _isActive = false;
            _camera = null;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _previous.Restore();
            _previous = null;
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
            LookShaderProperties.Publish(settings, QualitySettings.activeColorSpace);
        }

        static LookSettings CreateSettings()
        {
            LookSettings value = LookSettings.Default;
            value.outlineColor = new Color(0.08f, 0.12f, 0.16f);
            value.fogColor = new Color(0.075f, 0.095f, 0.115f);
            value.outlineWidthPixels = 1f;
            value.fogStart = 500f;
            value.fogEnd = 1000f;
            value.fogBands = 6;
            value.inkStrength = 0.16f;
            value.inkScale = 0.075f;
            value.inkWidth = 0.0001f;
            value.inkStart = 0.65f;
            value.inkRange = 1f;
            value.densityMul = 1f;
            value.inkWarp = 0.025f;
            value.inkWarpFreq = 2.44f;
            value.dashAmount = 0.1f;
            value.dashScale = 0.01f;
            value.inkDistStart = 100f;
            value.inkFarSpacing = 0.6f;
            value.contrast = 1f;
            return value;
        }
    }
}
