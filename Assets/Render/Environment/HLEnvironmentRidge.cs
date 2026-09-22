using System;
using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Environment
{
    public enum HLRidgeKind { Monolith, Mushroom }

    /// <summary>Height is the full standing height; CapDiameter is zero for monoliths.</summary>
    public struct HLRidgeItem
    {
        public HLRidgeKind Kind;
        public Vector3 Position;
        public float Height, Width, CapDiameter, CapThickness, Yaw;
        public uint Seed;
    }

    /// <summary>A row of tall silhouettes past the far edge, each standing so its mid-height sits in the last visible fog band.</summary>
    [DisallowMultipleComponent]
    public sealed class HLEnvironmentRidge : MonoBehaviour
    {
        public const uint Salt = 0x52494447u;
        public const float SpreadX = 14, GridClearance = 3;
        // The cap's bottom sits this far below the stem top, in cap thicknesses; the rest stands above it.
        const float CapSink = .7f;

        [SerializeField] Camera stageCamera;
        [SerializeField] Material stoneMaterial;
        [SerializeField] Material plantMaterial;
        [SerializeField] Rect grid = new Rect(-8, -8, 16, 16);
        [SerializeField] float groundY = .5f;
        [SerializeField] int seed = 1707;
        [SerializeField] float fogStart = 43.837f;
        [SerializeField] float fogEnd = 50.356f;
        [SerializeField] int fogBands = 6;
        [SerializeField] Color stoneColor = new Color32(168, 184, 172, 255);
        [SerializeField] Color stemColor = new Color32(184, 200, 180, 255);
        [SerializeField] Color capColor = new Color32(156, 180, 162, 255);
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        List<HLRidgeItem> items = new List<HLRidgeItem>();
        Transform root;
        bool retained;
        MaterialPropertyBlock properties;

        public IReadOnlyList<HLRidgeItem> Items => items;
        public Transform Root => root;

        public void Configure(Camera stage, Material stones, Material plants, Rect gridRect, float surfaceY, float start, float end, int bands, int layoutSeed)
        {
            stageCamera = stage; stoneMaterial = stones; plantMaterial = plants; grid = gridRect; groundY = surfaceY;
            fogStart = start; fogEnd = end; fogBands = bands; seed = layoutSeed;
        }

        /// <summary>Camera distances of the last visible fog band, [min, max).</summary>
        public static Vector2 LastBand(float fogStart, float fogEnd, int fogBands)
        {
            if (fogBands < 1 || !(fogStart >= 0) || !(fogEnd > fogStart) || float.IsInfinity(fogEnd)) throw new ArgumentOutOfRangeException(nameof(fogBands));
            return new Vector2(fogStart + (fogBands - 1f) / fogBands * (fogEnd - fogStart), fogEnd);
        }

        public static Vector3 MidHeight(in HLRidgeItem item) => item.Position + Vector3.up * (item.Height * .5f);

        /// <summary>Eight to twelve monoliths and eight to twelve mushroom stems across x in [-SpreadX, SpreadX], beyond the grid's far (+z) edge.
        /// Items the band cannot reach are clamped to z >= grid.yMax + GridClearance.</summary>
        public static List<HLRidgeItem> Layout(Vector3 cameraPosition, float fogStart, float fogEnd, int fogBands, Rect grid, float groundY, int seed)
        {
            var band = LastBand(fogStart, fogEnd, fogBands);
            if (!(grid.width > 0) || !(grid.height > 0) || float.IsNaN(groundY) || float.IsInfinity(groundY)) throw new ArgumentOutOfRangeException(nameof(grid));
            uint baseSeed = HLStoneSeed.ForPart((uint)seed, Salt);
            var random = new HLStoneRandom(baseSeed);
            int monoliths = 8 + (int)(random.Next01() * 5), mushrooms = 8 + (int)(random.Next01() * 5), total = monoliths + mushrooms;
            // Deal the kinds into x slots with a seeded shuffle so the row alternates irregularly.
            var kinds = new HLRidgeKind[total];
            for (int i = 0; i < total; i++) kinds[i] = i < monoliths ? HLRidgeKind.Monolith : HLRidgeKind.Mushroom;
            for (int i = total - 1; i > 0; i--) { int j = (int)(random.Next01() * (i + 1)); var t = kinds[i]; kinds[i] = kinds[j]; kinds[j] = t; }
            float spacing = 2 * SpreadX / total, depth = band.y - band.x;
            var result = new List<HLRidgeItem>(total);
            for (int i = 0; i < total; i++)
            {
                var item = new HLRidgeItem { Kind = kinds[i], Yaw = random.Range(0, 360), Seed = HLStoneSeed.ForPart(baseSeed, (uint)i + 1) };
                if (item.Kind == HLRidgeKind.Monolith) { item.Height = random.Range(6, 12); item.Width = item.Height * random.Range(.18f, .26f); }
                else
                {
                    float stem = random.Range(7, 14);
                    item.Width = random.Range(.35f, .6f); item.CapDiameter = random.Range(2.5f, 5); item.CapThickness = item.CapDiameter * random.Range(.25f, .35f);
                    item.Height = stem + (1 - CapSink) * item.CapThickness;
                }
                float x = Mathf.Clamp(-SpreadX + (i + .5f) * spacing + random.Range(-.35f, .35f) * spacing, -SpreadX, SpreadX);
                float distance = random.Range(band.x + .2f * depth, band.y - .2f * depth);
                float dx = x - cameraPosition.x, dy = groundY + item.Height * .5f - cameraPosition.y, reach = distance * distance - dx * dx - dy * dy;
                float z = cameraPosition.z + Mathf.Sqrt(Mathf.Max(0, reach));
                item.Position = new Vector3(x, groundY, Mathf.Max(z, grid.yMax + GridClearance));
                result.Add(item);
            }
            return result;
        }

        void Start() => Build();

        public void Build()
        {
            if (stageCamera) Build(stageCamera.transform.position);
        }

        public void Build(Vector3 cameraPosition)
        {
            Clear();
            HLPrimitiveMeshes.Retain(); retained=true;
            items = Layout(cameraPosition, fogStart, fogEnd, fogBands, grid, groundY, seed);
            root = new GameObject("HLRidgeItems").transform; root.SetParent(transform, false);
            properties = new MaterialPropertyBlock();
            foreach (var item in items) Spawn(item);
        }

        public void Clear()
        {
            if (root) Dispose(root.gameObject); root = null;
            foreach (var mesh in ownedMeshes) Dispose(mesh);
            ownedMeshes.Clear();
            if(retained) { HLPrimitiveMeshes.Release(); retained=false; }
        }
        void OnEnable() { if(root) root.gameObject.SetActive(true); }
        void OnDisable() { if(root) root.gameObject.SetActive(false); }
        void OnDestroy() => Clear();
        static void Dispose(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }

        void Spawn(in HLRidgeItem item)
        {
            var pivot = new GameObject(item.Kind.ToString()).transform; pivot.SetParent(root, true);
            pivot.position = item.Position; pivot.rotation = Quaternion.Euler(0, item.Yaw, 0);
            if (item.Kind == HLRidgeKind.Monolith)
            {
                var mesh = HLStoneMesh.CreateMesh(item.Seed, HLStonePresets.Monolith); mesh.name = "HLRidgeStone"; ownedMeshes.Add(mesh);
                var size = mesh.bounds.size;
                var scale = new Vector3(item.Width / Mathf.Max(size.x, 1e-4f), item.Height / Mathf.Max(size.y, 1e-4f), item.Width / Mathf.Max(size.x, 1e-4f));
                Part(pivot, mesh, stoneMaterial, Vector3.zero, scale, stoneColor);
                return;
            }
            float stem = item.Height - (1 - CapSink) * item.CapThickness;
            Part(pivot, HLPrimitiveMeshes.Get(HLPrimitive.Capsule), plantMaterial, Vector3.zero, new Vector3(item.Width, stem, item.Width), stemColor);
            Part(pivot, HLPrimitiveMeshes.Get(HLPrimitive.Sphere), plantMaterial, Vector3.up * (stem - CapSink * item.CapThickness),
                new Vector3(item.CapDiameter, item.CapThickness, item.CapDiameter), capColor);
        }

        void Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Vector3 scale, Color color)
        {
            var go = new GameObject(mesh.name); go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.transform.localPosition = bottom + new Vector3(0, -mesh.bounds.min.y * scale.y, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            properties.SetColor("_BaseColor", color.linear); renderer.SetPropertyBlock(properties);
        }
    }
}
