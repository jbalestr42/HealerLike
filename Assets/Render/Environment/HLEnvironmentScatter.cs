using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    /// <summary>Procedural ring of stones and plants around the grid, never inside it. Built once from the seed.</summary>
    [DisallowMultipleComponent]
    public sealed class HLEnvironmentScatter : MonoBehaviour
    {
        public static readonly Color[] Greens = { new Color32(46,125,79,255), new Color32(79,168,79,255), new Color32(155,210,74,255), new Color32(127,201,63,255) };
        [SerializeField] GridManager grid;
        [SerializeField] Material plantMaterial;
        [SerializeField] Material stoneMaterial;
        [SerializeField] float surfaceY = .5f;
        [SerializeField] HLEnvironmentSettings settings = HLEnvironmentSettings.Default;
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        readonly List<(Transform pivot, Quaternion rest, HLIdleDefinition idle)> swaying = new List<(Transform, Quaternion, HLIdleDefinition)>();
        List<HLEnvironmentItem> items = new List<HLEnvironmentItem>();
        Transform root;
        MaterialPropertyBlock properties;

        public IReadOnlyList<HLEnvironmentItem> Items => items;
        public int SwayingCount => swaying.Count;
        public Transform Root => root;
        public HLEnvironmentSettings Settings { get => settings; set => settings = value; }

        public static Rect GridRect(GridManager g)
        {
            Vector3 c = g.transform.position; float w = g.width * g.size, h = g.height * g.size;
            return new Rect(c.x - w * .5f, c.z - h * .5f, w, h);
        }

        void Start() { if (grid && grid.width > 0 && grid.height > 0 && grid.size > 0) Build(GridRect(grid), grid.size); }

        void Update()
        {
            float time = Time.time;
            foreach (var (pivot, rest, idle) in swaying)
                if (pivot) pivot.localRotation = rest * HLIdleMotion.Evaluate(idle, time).sway;
        }

        public void Build(Rect gridRect, float cellSize)
        {
            Clear();
            items = HLEnvironmentLayout.Generate(settings, gridRect, cellSize, surfaceY);
            root = new GameObject("HLEnvironmentItems").transform; root.SetParent(transform, false);
            properties = new MaterialPropertyBlock();
            foreach (var item in items) Spawn(item);
        }

        public void Clear()
        {
            swaying.Clear();
            if (root) Dispose(root.gameObject); root = null;
            foreach (var mesh in ownedMeshes) Dispose(mesh);
            ownedMeshes.Clear();
        }
        void OnDestroy() => Clear();
        static void Dispose(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }

        void Spawn(in HLEnvironmentItem item)
        {
            var pivot = new GameObject(item.Kind.ToString()).transform; pivot.SetParent(root, false);
            pivot.localPosition = item.Position; pivot.localRotation = Quaternion.Euler(0, item.Yaw, 0);
            var random = new HLStoneRandom(item.Seed);
            float s = item.Scale;
            switch (item.Kind)
            {
                case HLEnvironmentKind.Boulder: Stone(pivot, item.Seed, HLStonePresets.Boulder, Vector3.zero, s, item.PaletteIndex); break;
                case HLEnvironmentKind.Monolith: Stone(pivot, item.Seed, HLStonePresets.Monolith, Vector3.zero, s, item.PaletteIndex % 2 == 0 ? 1 : 2); break;
                case HLEnvironmentKind.Cairn:
                {
                    // Stacked cairn: two to four stones shrinking upward, each resting on the one below.
                    int layers = 2 + (int)(random.Next01() * 3); float y = 0;
                    for (int i = 0; i < layers; i++)
                    {
                        float k = s * (1 - i * .2f);
                        var part = Stone(pivot, HLStoneSeed.ForPart(item.Seed, (uint)i + 1), HLStonePresets.Cairn, new Vector3(random.Range(-.08f, .08f) * s, y, random.Range(-.08f, .08f) * s), k, (item.PaletteIndex + i) & 3);
                        y += part.y * .8f;
                    }
                    break;
                }
                case HLEnvironmentKind.MushroomTree:
                {
                    float stem = random.Range(2.2f, 4.2f) * s;
                    Part(pivot, HLPrimitive.Capsule, Vector3.zero, Quaternion.identity, new Vector3(.22f * s, stem, .22f * s), Greens[1]);
                    bool cone = random.Next01() < .4f;
                    Part(pivot, cone ? HLPrimitive.Cone : HLPrimitive.Sphere, Vector3.up * (stem * .92f), Quaternion.identity,
                        cone ? new Vector3(1.4f, .7f, 1.4f) * s : new Vector3(1.6f, .45f, 1.6f) * s, random.Next01() < .5f ? Greens[2] : Greens[3]);
                    Sway(pivot, item.Seed, 2.5f);
                    break;
                }
                case HLEnvironmentKind.SpiralFern:
                {
                    int fronds = 3 + (int)(random.Next01() * 3);
                    for (int f = 0; f < fronds; f++)
                    {
                        var frond = new GameObject("Frond").transform; frond.SetParent(pivot, false);
                        frond.localRotation = Quaternion.Euler(0, f * 360f / fronds + random.Range(-15, 15), 0);
                        Vector3 p = Vector3.zero; float theta = random.Range(10, 25), length = .3f * s;
                        for (int i = 0; i < 7; i++)
                        {
                            var d = new Vector3(Mathf.Sin(theta * Mathf.Deg2Rad), Mathf.Cos(theta * Mathf.Deg2Rad), 0);
                            Part(frond, HLPrimitive.CylinderSegment, p, Quaternion.FromToRotation(Vector3.up, d), new Vector3(.07f * s, length, .07f * s), Color.Lerp(Greens[1], Greens[2], i / 6f));
                            p += d * length; theta += 22 + i * 7; length *= .88f;
                        }
                        Part(frond, HLPrimitive.Torus, p, Quaternion.Euler(0, 0, 90), new Vector3(.22f, .22f, .22f) * s, Greens[2]);
                    }
                    Sway(pivot, item.Seed, 4f);
                    break;
                }
                case HLEnvironmentKind.BladeRosette:
                {
                    int blades = 7 + (int)(random.Next01() * 5);
                    for (int i = 0; i < blades; i++)
                        Part(pivot, HLPrimitive.Cone, Vector3.zero, Quaternion.Euler(0, i * 360f / blades + random.Range(-10, 10), random.Range(15, 45)),
                            new Vector3(.14f * s, random.Range(.6f, 1.2f) * s, .14f * s), Color.Lerp(Greens[0], Greens[2], random.Next01()));
                    break;
                }
                case HLEnvironmentKind.SphereCluster:
                {
                    int stems = 3 + (int)(random.Next01() * 3);
                    for (int i = 0; i < stems; i++)
                    {
                        var tilt = Quaternion.Euler(0, i * 360f / stems + random.Range(-20, 20), random.Range(0, 15));
                        float h = random.Range(.6f, 1.6f) * s, d = random.Range(.3f, .55f) * s;
                        Part(pivot, HLPrimitive.CylinderSegment, Vector3.zero, tilt, new Vector3(.05f * s, h, .05f * s), Greens[1]);
                        Part(pivot, HLPrimitive.Sphere, tilt * Vector3.up * h - Vector3.up * (d * .2f), Quaternion.identity, Vector3.one * d, i % 2 == 0 ? Greens[2] : Greens[3]);
                    }
                    Sway(pivot, item.Seed, 3f);
                    break;
                }
            }
        }

        void Sway(Transform pivot, uint seed, float degrees)
        {
            var idle = new HLIdleDefinition { swayDegrees = degrees, swayFrequency = .1f, breathAmount = 0, breathFrequency = .2f, seed = (int)(seed & 0x7fffffff) };
            swaying.Add((pivot, pivot.localRotation, idle));
        }

        // Places the part so the lowest point of its mesh sits on bottom, along the part's own up axis. Returns its world size.
        Vector3 Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Quaternion rotation, Vector3 scale, Color color, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localRotation = rotation; go.transform.localScale = scale;
            go.transform.localPosition = bottom + rotation * new Vector3(0, -mesh.bounds.min.y * scale.y, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            properties.SetColor("_BaseColor", color.linear); renderer.SetPropertyBlock(properties);
            return Vector3.Scale(mesh.bounds.size, scale);
        }
        void Part(Transform parent, HLPrimitive primitive, Vector3 bottom, Quaternion rotation, Vector3 scale, Color color)
            => Part(parent, HLPrimitiveMeshes.Get(primitive), plantMaterial, bottom, rotation, scale, color, primitive.ToString());

        Vector3 Stone(Transform parent, uint seed, HLStoneSettings shape, Vector3 bottom, float scale, int palette)
        {
            var mesh = HLStoneMesh.CreateMesh(seed, shape); mesh.name = "HLEnvironmentStone"; ownedMeshes.Add(mesh);
            // Sink each stone a little into the ground so it reads as rooted.
            return Part(parent, mesh, stoneMaterial, bottom - Vector3.up * (mesh.bounds.size.y * scale * .12f), Quaternion.identity,
                Vector3.one * scale, HLStoneAssembly.Palette[Mathf.Clamp(palette, 0, 3)], "Stone");
        }
    }
}
