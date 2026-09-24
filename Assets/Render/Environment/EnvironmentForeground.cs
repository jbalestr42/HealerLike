using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stage;
using HealerLike.Render.Stones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    // Large dark shapes cropped by the bottom corners of the frame, in front of everything else.
    public class EnvironmentForeground : MonoBehaviour
    {
        public static readonly uint Salt = 0x464F5245u;
        // Longest rosette blade over rosette height, and the widest blade tilt:
        // together they bound the rosette footprint
        public static readonly float BladeLength = 1.15f;
        public static readonly float MaxTilt = 50f;

        [SerializeField] Camera _stageCamera;
        [SerializeField] Material _stoneMaterial;
        [SerializeField] Material _plantMaterial;
        [SerializeField] float _groundY = 0.5f;
        [SerializeField] int _seed = 1707;
        // Frame aspect the corners are computed for, zero or less uses the camera's
        [SerializeField] float _aspect = 0f;
        [SerializeField] Color _tint = new Color32(43, 75, 143, 255);
        [SerializeField] Color _rosetteTint = new Color32(61, 116, 98, 255);

        readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        MaterialPropertyBlock _properties;
        PrimitiveMeshes _meshes;

        List<ForegroundItem> _items = new List<ForegroundItem>();
        public IReadOnlyList<ForegroundItem> items { get { return _items; } }

        Transform _root;
        public Transform root { get { return _root; } }

        public void Init(Camera stageCamera, float surfaceY, RenderManager manager)
        {
            if (manager == null || manager.meshes == null)
            {
                Debug.LogError("[EnvironmentForeground] Init needs the render manager and its primitive meshes.");
                return;
            }

            _meshes = manager.meshes;
            _stageCamera = stageCamera;
            _groundY = surfaceY;
            Build();
        }

        void OnEnable()
        {
            if (_root)
            {
                _root.gameObject.SetActive(true);
            }
        }

        void OnDisable()
        {
            if (_root)
            {
                _root.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            Clear();
        }

        // Ground point seen through a viewport point (0..1, origin bottom-left); false when the ray misses the ground
        public static bool GroundHit(Vector3 cameraPosition, Quaternion cameraRotation, float verticalFov, float aspect,
            Vector2 viewport, float groundY, out Vector3 hit)
        {
            float tan = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            Vector3 local = new Vector3((2f * viewport.x - 1f) * tan * aspect, (2f * viewport.y - 1f) * tan, 1f);
            Vector3 direction = cameraRotation * local;
            float along = (groundY - cameraPosition.y) / direction.y;
            hit = cameraPosition + direction * along;
            return direction.y < 0f && along > 0f && float.IsFinite(along);
        }

        // Viewport coordinates (0..1 inside the frame) of a world point in front of the camera
        public static Vector2 ToViewport(Vector3 world, Vector3 cameraPosition, Quaternion cameraRotation,
            float verticalFov, float aspect)
        {
            Vector3 local = Quaternion.Inverse(cameraRotation) * (world - cameraPosition);
            float tan = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            float x = 0.5f + 0.5f * local.x / (local.z * tan * aspect);
            float y = 0.5f + 0.5f * local.y / (local.z * tan);
            return new Vector2(x, y);
        }

        // Two to three boulders and one to two rosettes per bottom corner, centred on or just past the frame edge.
        // Returns no items and logs when the camera does not see the ground at both bottom corners.
        public static List<ForegroundItem> Layout(Vector3 cameraPosition, Quaternion cameraRotation,
            float verticalFov, float aspect, float groundY, int seed)
        {
            List<ForegroundItem> result = new List<ForegroundItem>();
            bool isLensValid = verticalFov > 1f && verticalFov < 179f && float.IsFinite(aspect) && aspect > 0f;
            if (!isLensValid || !float.IsFinite(groundY))
            {
                Debug.LogError($"[EnvironmentForeground] Rejected field of view {verticalFov}, aspect {aspect}, "
                               + $"ground {groundY}.");
                return result;
            }

            Vector3[] corners = new Vector3[2];
            for (int side = 0; side < 2; side++)
            {
                Vector2 viewport = new Vector2(side, 0f);
                bool isOnGround = GroundHit(cameraPosition, cameraRotation, verticalFov, aspect, viewport, groundY,
                                            out corners[side]);
                if (!isOnGround)
                {
                    Debug.LogError("[EnvironmentForeground] The bottom corners of the frame do not see the ground.");
                    return result;
                }
            }

            uint baseSeed = StoneSeed.ForPart((uint)seed, Salt);
            StoneRandom random = new StoneRandom(baseSeed);
            for (int side = 0; side < 2; side++)
            {
                float outward = side == 0 ? -1f : 1f;
                Vector3 corner = corners[side];
                // Offsets: positive x goes out past the side edge, negative z goes down past the bottom edge
                int boulders = 2 + (int)(random.Next01() * 2f);
                int rosettes = 1 + (int)(random.Next01() * 2f);
                for (int n = 0; n < boulders + rosettes; n++)
                {
                    bool isBoulder = n < boulders;
                    float size = isBoulder ? random.Range(2f, 3.5f) : random.Range(3f, 5f);
                    float aspectScale = aspect > 1f ? 0.5f : 0.7f;
                    float scale = size * aspectScale;
                    float radius = isBoulder ? scale : scale * BladeLength * Mathf.Sin(MaxTilt * Mathf.Deg2Rad);
                    Vector3 position = corner;
                    for (int attempt = 0; attempt < 16; attempt++)
                    {
                        float dx = isBoulder ? random.Range(-1f, 2.5f) : random.Range(0f, 3f);
                        float dz = isBoulder ? random.Range(-2.5f, 0.5f) : random.Range(-2.5f, 0f);
                        position = new Vector3(corner.x + outward * dx, groundY, corner.z + dz);
                        bool isClear = true;
                        foreach (ForegroundItem other in result)
                        {
                            float minDistance = 0.8f * (other.radius + radius);
                            if (other.kind == ForegroundKind.Boulder && isBoulder
                                && (other.position - position).sqrMagnitude < minDistance * minDistance)
                            {
                                isClear = false;
                            }
                        }

                        if (isClear)
                        {
                            break;
                        }
                    }

                    ForegroundKind kind = isBoulder ? ForegroundKind.Boulder : ForegroundKind.Rosette;
                    result.Add(new ForegroundItem
                    {
                        kind = kind,
                        position = position,
                        scale = scale,
                        radius = radius,
                        yaw = random.Range(0f, 360f),
                        seed = StoneSeed.ForPart(baseSeed, (uint)(side * 256 + n + 1))
                    });
                }
            }

            return result;
        }

        public void Build()
        {
            if (!_stageCamera)
            {
                return;
            }

            float frameAspect = _aspect > 0f ? _aspect : _stageCamera.aspect;
            Transform cameraTransform = _stageCamera.transform;
            Build(cameraTransform.position, cameraTransform.rotation, _stageCamera.fieldOfView, frameAspect);
        }

        public void Build(Vector3 cameraPosition, Quaternion cameraRotation, float verticalFov, float frameAspect)
        {
            Clear();
            _items = Layout(cameraPosition, cameraRotation, verticalFov, frameAspect, _groundY, _seed);
            _root = new GameObject("ForegroundItems").transform;
            _root.SetParent(transform, false);
            _properties = new MaterialPropertyBlock();
            foreach (ForegroundItem item in _items)
            {
                Spawn(item);
            }
        }

        public void Clear()
        {
            if (_root)
            {
                Destroy(_root.gameObject);
            }

            _root = null;
            foreach (Mesh mesh in _ownedMeshes)
            {
                Destroy(mesh);
            }

            _ownedMeshes.Clear();
        }

        void Spawn(ForegroundItem item)
        {
            Transform pivot = new GameObject(item.kind.ToString()).transform;
            pivot.SetParent(_root, true);
            pivot.position = item.position;
            pivot.rotation = Quaternion.Euler(0f, item.yaw, 0f);
            StoneRandom random = new StoneRandom(item.seed);
            if (item.kind == ForegroundKind.Boulder)
            {
                Mesh mesh = StoneMesh.CreateMesh(item.seed, StonePresets.Boulder);
                mesh.name = "ForegroundStone";
                _ownedMeshes.Add(mesh);
                float k = item.scale / Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z, 0.0001f);
                // Sink a quarter of the height so the boulder reads as half buried
                Vector3 bottom = -Vector3.up * (mesh.bounds.size.y * k * 0.25f);
                Part(pivot, mesh, _stoneMaterial, bottom, Quaternion.identity, Vector3.one * k, _tint, "Stone");
                return;
            }

            // Rosette: long solid leaves fanned from one root, each tilted outward, thin across its tilt plane
            int blades = 7 + (int)(random.Next01() * 5f);
            Mesh leaf = _meshes.leaf;
            for (int i = 0; i < blades; i++)
            {
                float length = random.Range(0.8f, BladeLength) * item.scale;
                float width = length * random.Range(0.12f, 0.18f);
                float around = i * 360f / blades + random.Range(-12f, 12f);
                Quaternion rotation = Quaternion.Euler(0f, around, random.Range(20f, MaxTilt));
                Color color = Color.Lerp(_rosetteTint, _tint, random.Range(0f, 0.35f));
                Vector3 scale = new Vector3(width * 0.3f, length, width);
                Part(pivot, leaf, _plantMaterial, Vector3.zero, rotation, scale, color, "Leaf");
            }
        }

        void Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Quaternion rotation, Vector3 scale,
            Color color, string name)
        {
            GameObject partGo = new GameObject(name);
            partGo.transform.SetParent(parent, false);
            partGo.transform.localRotation = rotation;
            partGo.transform.localScale = scale;
            partGo.transform.localPosition = bottom + rotation * new Vector3(0f, -mesh.bounds.min.y * scale.y, 0f);
            partGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = partGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            _properties.SetColor("_BaseColor", color.linear);
            meshRenderer.SetPropertyBlock(_properties);
        }
    }
}
