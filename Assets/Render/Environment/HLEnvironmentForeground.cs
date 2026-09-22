using System;
using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Environment
{
    public enum HLForegroundKind { Boulder, Rosette }

    /// <summary>Scale is the boulder radius or the rosette height; Radius is the ground footprint either way.</summary>
    public struct HLForegroundItem
    {
        public HLForegroundKind Kind;
        public Vector3 Position;
        public float Scale, Radius, Yaw;
        public uint Seed;
    }

    /// <summary>Large dark shapes cropped by the bottom corners of the frame, in front of everything else.</summary>
    [DisallowMultipleComponent]
    public sealed class HLEnvironmentForeground : MonoBehaviour
    {
        public const uint Salt = 0x464F5245u;
        // Longest rosette blade over rosette height, and the widest blade tilt: bounds the rosette footprint.
        public const float BladeLength = 1.15f, MaxTilt = 50;

        [SerializeField] Camera stageCamera;
        [SerializeField] Material stoneMaterial;
        [SerializeField] Material plantMaterial;
        [SerializeField] float groundY = .5f;
        [SerializeField] int seed = 1707;
        [Tooltip("Frame aspect the corners are computed for; zero or less uses the camera's.")] [SerializeField] float aspect = 9f / 16f;
        [SerializeField] Color tint = new Color32(43, 75, 143, 255);
        [SerializeField] Color rosetteTint = new Color32(58, 90, 154, 255);
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        List<HLForegroundItem> items = new List<HLForegroundItem>();
        Transform root;
        MaterialPropertyBlock properties;

        public IReadOnlyList<HLForegroundItem> Items => items;
        public Transform Root => root;

        public void Configure(Camera stage, Material stones, Material plants, float surfaceY, int layoutSeed)
        {
            stageCamera = stage; stoneMaterial = stones; plantMaterial = plants; groundY = surfaceY; seed = layoutSeed;
        }

        /// <summary>Ground point seen through the given viewport point (0..1, origin bottom-left).</summary>
        public static Vector3 GroundHit(Vector3 cameraPosition, Quaternion cameraRotation, float verticalFov, float aspect, Vector2 viewport, float groundY)
        {
            float tan = Mathf.Tan(verticalFov * .5f * Mathf.Deg2Rad);
            var direction = cameraRotation * new Vector3((2 * viewport.x - 1) * tan * aspect, (2 * viewport.y - 1) * tan, 1);
            float along = (groundY - cameraPosition.y) / direction.y;
            if (!(direction.y < 0) || !(along > 0) || float.IsInfinity(along)) throw new ArgumentException("The viewport ray does not hit the ground.");
            return cameraPosition + direction * along;
        }

        /// <summary>Viewport coordinates (0..1 inside the frame) of a world point in front of the camera.</summary>
        public static Vector2 ToViewport(Vector3 world, Vector3 cameraPosition, Quaternion cameraRotation, float verticalFov, float aspect)
        {
            var local = Quaternion.Inverse(cameraRotation) * (world - cameraPosition);
            float tan = Mathf.Tan(verticalFov * .5f * Mathf.Deg2Rad);
            return new Vector2(.5f + .5f * local.x / (local.z * tan * aspect), .5f + .5f * local.y / (local.z * tan));
        }

        /// <summary>Two to three boulders and one to two rosettes per bottom corner, centred on or just past the frame edge.</summary>
        public static List<HLForegroundItem> Layout(Vector3 cameraPosition, Quaternion cameraRotation, float verticalFov, float aspect, float groundY, int seed)
        {
            if (!(verticalFov > 1) || !(verticalFov < 179) || !(aspect > 0) || float.IsInfinity(aspect) || float.IsNaN(groundY) || float.IsInfinity(groundY))
                throw new ArgumentOutOfRangeException(nameof(verticalFov));
            var result = new List<HLForegroundItem>();
            uint baseSeed = HLStoneSeed.ForPart((uint)seed, Salt);
            var random = new HLStoneRandom(baseSeed);
            for (int side = 0; side < 2; side++)
            {
                float outward = side == 0 ? -1 : 1;
                var corner = GroundHit(cameraPosition, cameraRotation, verticalFov, aspect, new Vector2(side, 0), groundY);
                // Offsets: positive x is outward past the side edge, negative z is down past the bottom edge.
                int boulders = 2 + (int)(random.Next01() * 2), rosettes = 1 + (int)(random.Next01() * 2);
                for (int n = 0; n < boulders + rosettes; n++)
                {
                    bool boulder = n < boulders;
                    float scale = boulder ? random.Range(2, 3.5f) : random.Range(3, 5);
                    float radius = boulder ? scale : scale * BladeLength * Mathf.Sin(MaxTilt * Mathf.Deg2Rad);
                    Vector3 position = corner;
                    for (int attempt = 0; attempt < 16; attempt++)
                    {
                        float dx = boulder ? random.Range(-1, 2.5f) : random.Range(0, 3), dz = boulder ? random.Range(-2.5f, .5f) : random.Range(-2.5f, 0);
                        position = new Vector3(corner.x + outward * dx, groundY, corner.z + dz);
                        bool clear = true;
                        foreach (var other in result)
                            if (other.Kind == HLForegroundKind.Boulder && boulder && (other.Position - position).sqrMagnitude < Sq(.8f * (other.Radius + radius))) clear = false;
                        if (clear) break;
                    }
                    result.Add(new HLForegroundItem {
                        Kind = boulder ? HLForegroundKind.Boulder : HLForegroundKind.Rosette, Position = position, Scale = scale, Radius = radius,
                        Yaw = random.Range(0, 360), Seed = HLStoneSeed.ForPart(baseSeed, (uint)(side * 256 + n + 1)) });
                }
            }
            return result;
        }
        static float Sq(float v) => v * v;

        void Start() => Build();

        public void Build()
        {
            if (!stageCamera) return;
            Build(stageCamera.transform.position, stageCamera.transform.rotation, stageCamera.fieldOfView, aspect > 0 ? aspect : stageCamera.aspect);
        }

        public void Build(Vector3 cameraPosition, Quaternion cameraRotation, float verticalFov, float frameAspect)
        {
            Clear();
            items = Layout(cameraPosition, cameraRotation, verticalFov, frameAspect, groundY, seed);
            root = new GameObject("HLForegroundItems").transform; root.SetParent(transform, false);
            properties = new MaterialPropertyBlock();
            foreach (var item in items) Spawn(item);
        }

        public void Clear()
        {
            if (root) Dispose(root.gameObject); root = null;
            foreach (var mesh in ownedMeshes) Dispose(mesh);
            ownedMeshes.Clear();
        }
        void OnDestroy() => Clear();
        static void Dispose(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }

        void Spawn(in HLForegroundItem item)
        {
            var pivot = new GameObject(item.Kind.ToString()).transform; pivot.SetParent(root, true);
            pivot.position = item.Position; pivot.rotation = Quaternion.Euler(0, item.Yaw, 0);
            var random = new HLStoneRandom(item.Seed);
            if (item.Kind == HLForegroundKind.Boulder)
            {
                var mesh = HLStoneMesh.CreateMesh(item.Seed, HLStonePresets.Boulder); mesh.name = "HLForegroundStone"; ownedMeshes.Add(mesh);
                float k = item.Scale / Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z, 1e-4f);
                // Sink a quarter of the height so the boulder reads as half buried.
                Part(pivot, mesh, stoneMaterial, -Vector3.up * (mesh.bounds.size.y * k * .25f), Quaternion.identity, Vector3.one * k, tint, "Stone");
                return;
            }
            // Rosette: long flattened blades fanned from one root, each tilted outward, thin across its tilt plane.
            int blades = 7 + (int)(random.Next01() * 5);
            var cone = HLPrimitiveMeshes.Get(HLPrimitive.Cone);
            for (int i = 0; i < blades; i++)
            {
                float length = random.Range(.8f, BladeLength) * item.Scale, width = length * random.Range(.12f, .18f);
                var rotation = Quaternion.Euler(0, i * 360f / blades + random.Range(-12, 12), random.Range(20, MaxTilt));
                var color = Color.Lerp(rosetteTint, tint, random.Range(0, .35f));
                Part(pivot, cone, plantMaterial, Vector3.zero, rotation, new Vector3(width * .3f, length, width), color, "Blade");
            }
        }

        void Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Quaternion rotation, Vector3 scale, Color color, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localRotation = rotation; go.transform.localScale = scale;
            go.transform.localPosition = bottom + rotation * new Vector3(0, -mesh.bounds.min.y * scale.y, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            properties.SetColor("_BaseColor", color.linear); renderer.SetPropertyBlock(properties);
        }
    }
}
