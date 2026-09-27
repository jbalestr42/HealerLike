using System;
using HealerLike.Render.Creatures;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Spells
{
    // One synchronous capture on a cache miss. No gameplay camera, material, or scene object is modified.
    public sealed class SpellIconRenderer : ISpellIconCapture
    {
        public static readonly int Resolution = 256;
        public static readonly int CaptureLayer = 31;
        public static readonly string RootName = "Spell Icon Capture";
        static readonly Vector3 Origin = new Vector3(64, -4096, 0);
        readonly PrimitiveMeshes _meshes;
        readonly Material _material;
        GameObject _root;
        Camera _camera;
        bool _disposed;

        public SpellIconRenderer(PrimitiveMeshes meshes, Material material)
        {
            _meshes = meshes;
            _material = material;
        }

        public Texture2D Capture(SpellIconRecipe recipe)
        {
            if (_disposed || _meshes == null || _material == null || !SpellIconSubject.IsValid(recipe))
            {
                return null;
            }
            InitCamera();
            UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
            if (!RenderPipeline.SupportsRenderRequest(_camera, request))
            {
                return null;
            }

            RenderTexture target = null;
            Texture2D image = null;
            RenderTexture previous = RenderTexture.active;
            float fogStart = Shader.GetGlobalFloat("_HLFogStart");
            float fogEnd = Shader.GetGlobalFloat("_HLFogEnd");
            using (SpellIconSubject subject = new SpellIconSubject(recipe, _meshes, _material, Origin))
            {
                try
                {
                    _root.SetActive(true);
                    target = RenderTexture.GetTemporary(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
                    request.destination = target;
                    Shader.SetGlobalFloat("_HLFogStart", 10000);
                    Shader.SetGlobalFloat("_HLFogEnd", 20000);
                    RenderPipeline.SubmitRenderRequest(_camera, request);
                    RenderTexture.active = target;
                    image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
                    {
                        name = "Composed spell icon", hideFlags = HideFlags.HideAndDontSave,
                        filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                    };
                    image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0, false);
                    image.Apply(false, false);
                    Texture2D result = image;
                    image = null;
                    return result;
                }
                finally
                {
                    _root.SetActive(false);
                    Shader.SetGlobalFloat("_HLFogStart", fogStart);
                    Shader.SetGlobalFloat("_HLFogEnd", fogEnd);
                    RenderTexture.active = previous;
                    if (target != null)
                    {
                        RenderTexture.ReleaseTemporary(target);
                    }
                    RenderObjects.Release(image);
                }
            }
        }

        void InitCamera()
        {
            if (_root != null)
            {
                return;
            }
            _root = new GameObject(RootName) { hideFlags = HideFlags.HideAndDontSave };
            _root.SetActive(false);
            _camera = _root.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.orthographicSize = 1.48f;
            _camera.aspect = 1;
            _camera.nearClipPlane = .01f;
            _camera.farClipPlane = 12;
            _camera.transform.SetPositionAndRotation(Origin + Vector3.back * 6, Quaternion.identity);
            _camera.cullingMask = 1 << CaptureLayer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.useOcclusionCulling = false;
            UniversalAdditionalCameraData data = _root.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.On;
            data.volumeLayerMask = 0;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_root != null)
            {
                _root.SetActive(false);
            }
            RenderObjects.Release(_root);
            _root = null;
            _camera = null;
        }
    }
}
