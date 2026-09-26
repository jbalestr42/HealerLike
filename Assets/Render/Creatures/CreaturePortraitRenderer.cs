using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // A disabled camera performs one URP request on a cache miss. The temporary subject is
    // outside the battlefield and inactive before/after that synchronous call. No gameplay
    // camera, scene light or layer setting is changed. Its materials are the live view's materials.
    public class CreaturePortraitRenderer : ICreaturePortraitCapture
    {
        public static readonly int Resolution = 256;
        public static readonly string RootName = "Creature Portrait Capture";
        static readonly int captureLayer = 31;
        static readonly Vector3 origin = new Vector3(0f, -4096f, 0f);
        static readonly Quaternion rotation = Quaternion.Euler(12f, StageCalibration.PortraitYaw, 0f);
        readonly CreatureLooks _looks;
        readonly PrimitiveMeshes _meshes;
        GameObject _root;
        Camera _camera;
        bool _disposed;
        public CreaturePortraitRenderer(CreatureLooks looks, PrimitiveMeshes meshes)
        {
            _looks = looks;
            _meshes = meshes;
        }

        public Texture2D Capture(EntityData data, Entity.EntityType side)
        {
            if (_disposed || !_looks || !_meshes || !data)
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
            // Fog uses camera distance, not distance from world origin. Suppress it explicitly for
            // the portrait even if the user sets a very short live fog range, then restore exactly.
            float fogStart = Shader.GetGlobalFloat("_HLFogStart");
            float fogEnd = Shader.GetGlobalFloat("_HLFogEnd");
            using (CreaturePreview preview = new CreaturePreview())
            {
                try
                {
                    if (!preview.Init(_looks, data, side, _meshes, _root.transform, 1f))
                    {
                        return null;
                    }

                    preview.CompleteAppearance();
                    preview.Tick(0f, 0f, new FootFrame(origin, Vector3.up, 1f), -(rotation * Vector3.forward));
                    foreach (Transform part in preview.rig.root.GetComponentsInChildren<Transform>(true))
                    {
                        part.gameObject.layer = captureLayer;
                        part.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    }

                    _root.SetActive(true);
                    if (!TryGetBounds(preview.rig.root, out Bounds bounds))
                    {
                        return null;
                    }

                    Frame(_camera, bounds);
                    target = RenderTexture.GetTemporary(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
                    request.destination = target;
                    Shader.SetGlobalFloat("_HLFogStart", 10000f);
                    Shader.SetGlobalFloat("_HLFogEnd", 20000f);
                    RenderPipeline.SubmitRenderRequest(_camera, request);
                    RenderTexture.active = target;
                    image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
                    {
                        name = "Creature Portrait " + data.name + " " + side,
                        hideFlags = HideFlags.HideAndDontSave,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    image.ReadPixels(new Rect(0f, 0f, Resolution, Resolution), 0, 0, false);
                    image.Apply(false, true);
                    Texture2D result = image;
                    image = null;
                    return result;
                }
                finally
                {
                    // Deactivation is immediate even though destruction is deferred in a player.
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

            _root = new GameObject(RootName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _root.SetActive(false);
            GameObject cameraObject = new GameObject("Portrait Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cameraObject.transform.SetParent(_root.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.aspect = 1f;
            _camera.cullingMask = 1 << captureLayer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(12f / 255f, 24f / 255f, 26f / 255f, 1f);
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.useOcclusionCulling = false;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.On;
            cameraData.volumeLayerMask = 0;
        }

        // Fit all eight world-bounds corners in camera space, with 18% of the image reserved as inset.
        public static void Frame(Camera camera, Bounds bounds)
        {
            Vector3 extent = bounds.extents;
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector3 cameraExtent = Vector3.zero;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = inverse * Vector3.Scale(extent, new Vector3(x, y, z));
                        cameraExtent = Vector3.Max(cameraExtent, new Vector3(Mathf.Abs(corner.x),
                            Mathf.Abs(corner.y), Mathf.Abs(corner.z)));
                    }
                }
            }

            float distance = Mathf.Max(2f, extent.magnitude * 2f + 1f);
            camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * distance, rotation);
            camera.aspect = 1f;
            camera.orthographicSize = Mathf.Max(0.1f, Mathf.Max(cameraExtent.x, cameraExtent.y)) / 0.82f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = distance + cameraExtent.z + 1f;
        }

        static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.bounds.size.sqrMagnitude < 0.000001f)
                {
                    continue;
                }

                if (found)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                }

                found = true;
            }

            return found;
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
