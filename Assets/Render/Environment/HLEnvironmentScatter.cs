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
        struct Motion
        {
            public Transform pivot, anchor;
            public Quaternion rest;
            public float frequency, phase, amplitude, uncurl;
        }
        readonly List<Motion> swaying = new List<Motion>();
        int plantCount;
        double builtAt;
        uint colourSeed;
        [SerializeField] Camera viewCamera;
        [SerializeField] HLEnvironmentGust gust;
        [SerializeField] float farDistance = 60;
        static readonly int FogEnd = Shader.PropertyToID("_HLFogEnd");
        static readonly int LookApplied = Shader.PropertyToID("_HLLookApplied");
        List<HLEnvironmentItem> items = new List<HLEnvironmentItem>();
        Transform root;
        MaterialPropertyBlock properties;

        public IReadOnlyList<HLEnvironmentItem> Items => items;
        public int SwayingCount => plantCount;
        public Transform Root => root;
        public HLEnvironmentSettings Settings { get => settings; set => settings = value; }

        public static Rect GridRect(GridManager g)
        {
            Vector3 c = g.transform.position; float w = g.width * g.size, h = g.height * g.size;
            return new Rect(c.x - w * .5f, c.z - h * .5f, w, h);
        }

        void Start() { if (grid && grid.width > 0 && grid.height > 0 && grid.size > 0) Build(GridRect(grid), grid.size); }

        public void ConfigureMotion(Camera camera, HLEnvironmentGust source, float fogEnd)
        { viewCamera = camera; gust = source; farDistance = Mathf.Max(1, fogEnd); }

        void Update()
        {
            if (!viewCamera) viewCamera = Camera.main;
            float distance = Shader.GetGlobalFloat(LookApplied) > .5f ? Shader.GetGlobalFloat(FogEnd) : farDistance;
            Animate(Time.timeAsDouble, viewCamera ? viewCamera.transform.position : Vector3.zero, viewCamera ? distance : float.MaxValue);
        }

        /// <summary>Absolute clock sampling permits LOD re-entry without integrating missed frames.</summary>
        public void Animate(double time, Vector3 cameraPosition, float distance)
        {
            Vector3 wind = gust ? gust.Sample(time) : Vector3.zero;
            float age = Mathf.Max(0, (float)(time - builtAt));
            float settle = age < 1.2f ? Mathf.Sin(age * 12) * Mathf.Exp(-age * 5) * 3 : 0;
            for (int i = 0; i < swaying.Count; i++)
            {
                var m = swaying[i];
                if (!m.pivot || (m.anchor.position - cameraPosition).sqrMagnitude > distance * distance) continue;
                float wave = Mathf.Sin((float)(time * m.frequency * 2 * System.Math.PI % (2 * System.Math.PI)) + m.phase);
                Vector3 localWind = m.pivot.parent.InverseTransformDirection(wind);
                m.pivot.localRotation = Quaternion.Euler(localWind.z * m.amplitude * 2, 0,
                    -localWind.x * m.amplitude * 2 - wind.magnitude * m.uncurl) * m.rest *
                    Quaternion.Euler(wave * m.amplitude * .35f, 0, wave * m.amplitude + settle);
            }
        }

        public void Build(Rect gridRect, float cellSize)
        {
            Clear();
            builtAt = Time.timeAsDouble;
            items = HLEnvironmentLayout.Generate(settings, gridRect, cellSize, surfaceY);
            HLPrimitiveMeshes.Retain(); retained = true;
            root = new GameObject("HLEnvironmentItems").transform; root.SetParent(transform, false);
            properties = new MaterialPropertyBlock();
            foreach (var item in items) Spawn(item);
        }

        bool retained;
        public void Clear()
        {
            swaying.Clear(); plantCount = 0;
            if (root) { root.gameObject.SetActive(false); Dispose(root.gameObject); } root = null;
            foreach (var mesh in ownedMeshes) Dispose(mesh);
            ownedMeshes.Clear();
            if (retained) { HLPrimitiveMeshes.Release(); retained = false; }
        }
        void OnDestroy() => Clear();
        static void Dispose(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }

        void Spawn(in HLEnvironmentItem item)
        {
            var pivot = new GameObject(item.Kind.ToString()).transform; pivot.SetParent(root, false);
            pivot.localPosition = item.Position; pivot.localRotation = Quaternion.Euler(0, item.Yaw, 0);
            var random = new HLStoneRandom(item.Seed);
            float s = item.Scale; colourSeed = item.Seed;
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
                    Part(pivot, HLPrimitive.Capsule, Vector3.zero, Quaternion.identity, new Vector3(.22f * s, stem, .22f * s), new Color(.65f, .82f, .62f));
                    var cap = new GameObject("NoddingCap").transform; cap.SetParent(pivot, false); cap.localPosition = Vector3.up * (stem * .92f);
                    Part(cap, HLPrimitive.Sphere, Vector3.down * .08f * s, Quaternion.identity, new Vector3(1.5f, .14f, 1.5f) * s, Greens[0]);
                    bool cone = random.Next01() < .4f;
                    Part(cap, cone ? HLPrimitive.Cone : HLPrimitive.Sphere, Vector3.zero, Quaternion.identity,
                        cone ? new Vector3(1.4f, .7f, 1.4f) * s : new Vector3(1.6f, .45f, 1.6f) * s, random.Next01() < .5f ? new Color(.44f, .74f, .61f) : new Color(.64f, .78f, .65f));
                    Sway(pivot, item.Seed, Mathf.Clamp(stem * .45f, .6f, 3));
                    AddMotion(cap, pivot, item.Seed + 1, 1.4f, 0);
                    break;
                }
                case HLEnvironmentKind.SpiralFern:
                {
                    int fronds = 3 + (int)(random.Next01() * 3);
                    for (int f = 0; f < fronds; f++)
                    {
                        var frond = new GameObject("Frond").transform; frond.SetParent(pivot, false);
                        frond.localRotation = Quaternion.Euler(0, f * 360f / fronds + random.Range(-15, 15), 0);
                        Transform linkParent = frond;
                        float length = .3f * s;
                        for (int i = 0; i < 9; i++)
                        {
                            var joint = new GameObject("HLCurlJoint").transform; joint.SetParent(linkParent, false);
                            joint.localPosition = i == 0 ? Vector3.zero : Vector3.up * length / .88f;
                            joint.localRotation = Quaternion.Euler(0, 0, -(i == 0 ? random.Range(10, 25) : 22 + i * 5));
                            Part(joint, HLPrimitive.Sphere, Vector3.zero, Quaternion.identity,
                                new Vector3(length * .9f, length * 1.15f, length * .9f), Color.Lerp(Greens[1], Greens[2], i / 8f));
                            // Positive correction opens each negative curl angle during a gust.
                            AddMotion(joint, pivot, item.Seed + (uint)(f * 16 + i), .12f * s, -3);
                            linkParent = joint; length *= .88f;
                        }
                    }
                    Sway(pivot, item.Seed, 1.6f * s);
                    break;
                }
                case HLEnvironmentKind.BladeRosette:
                {
                    int blades = 7 + (int)(random.Next01() * 5);
                    for (int i = 0; i < blades; i++)
                        Part(pivot, HLPrimitive.Cone, Vector3.zero, Quaternion.Euler(0, i * 360f / blades + random.Range(-10, 10), random.Range(15, 45)),
                            new Vector3(.32f * s, random.Range(.8f, 1.6f) * s, .065f * s), Color.Lerp(Greens[0], Greens[2], random.Next01()));
                    Sway(pivot, item.Seed, .45f * s);
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
                    Sway(pivot, item.Seed, 1.2f * s);
                    break;
                }
            }
        }

        void Sway(Transform pivot, uint seed, float degrees)
        {
            plantCount++;
            AddMotion(pivot, pivot, seed, degrees, 0);
        }
        void AddMotion(Transform pivot, Transform anchor, uint seed, float degrees, float uncurl)
        {
            var random = new HLStoneRandom(seed);
            swaying.Add(new Motion { pivot = pivot, anchor = anchor, rest = pivot.localRotation,
                frequency = random.Range(.2f, .4f), phase = random.Range(0, Mathf.PI * 2), amplitude = degrees, uncurl = uncurl });
        }
        public static Color VaryColor(Color colour, uint seed)
        {
            var random = new HLStoneRandom(seed);
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            return Color.HSVToRGB(Mathf.Repeat(h + random.Range(-6f, 6f) / 360, 1), s, Mathf.Clamp01(v * random.Range(.92f, 1.08f)));
        }

        // Places the part so the lowest point of its mesh sits on bottom, along the part's own up axis. Returns its world size.
        Vector3 Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Quaternion rotation, Vector3 scale, Color color, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localRotation = rotation; go.transform.localScale = scale;
            go.transform.localPosition = bottom + rotation * new Vector3(0, -mesh.bounds.min.y * scale.y, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            properties.SetColor("_BaseColor", VaryColor(color, colourSeed).linear); renderer.SetPropertyBlock(properties);
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
