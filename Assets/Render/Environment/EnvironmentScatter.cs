using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stage;
using HealerLike.Render.Stones;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Environment
{
    // Seeded ring of stones and plants around the grid, never inside it, built once.
    public class EnvironmentScatter : MonoBehaviour
    {
        public static readonly Color[] Greens =
        {
            new Color32(46, 125, 79, 255),
            new Color32(79, 168, 79, 255),
            new Color32(155, 210, 74, 255),
            new Color32(127, 201, 63, 255)
        };

        static readonly int fogEndId = Shader.PropertyToID("_HLFogEnd");
        static readonly int lookAppliedId = Shader.PropertyToID("_HLLookApplied");

        struct Motion
        {
            public Transform pivot;
            public Transform anchor;
            public Quaternion rest;
            public float frequency;
            public float phase;
            public float amplitude;
            public float uncurl;
        }

        [FormerlySerializedAs("plantMaterial")]
        [SerializeField] Material _plantMaterial;
        [FormerlySerializedAs("stoneMaterial")]
        [SerializeField] Material _stoneMaterial;
        [FormerlySerializedAs("surfaceY")]
        [SerializeField] float _surfaceY = 0.5f;
        [FormerlySerializedAs("viewCamera")]
        [SerializeField] Camera _viewCamera;
        [FormerlySerializedAs("gust")]
        [SerializeField] EnvironmentGust _gust;
        [FormerlySerializedAs("farDistance")]
        [SerializeField] float _farDistance = 60f;

        readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        readonly List<Motion> _swaying = new List<Motion>();
        double _builtAt;
        uint _colourSeed;
        MaterialPropertyBlock _properties;
        PrimitiveMeshes _meshes;

        [FormerlySerializedAs("settings")]
        [SerializeField] EnvironmentSettings _settings = EnvironmentSettings.Default;
        public EnvironmentSettings settings { get { return _settings; } set { _settings = value; } }

        List<EnvironmentItem> _items = new List<EnvironmentItem>();
        public IReadOnlyList<EnvironmentItem> items { get { return _items; } }

        int _plantCount;
        public int swayingCount { get { return _plantCount; } }

        Transform _root;
        public Transform root { get { return _root; } }

        public void Init(Rect board, float cellSize, float surfaceY, Camera viewCamera, EnvironmentGust gust, float fogEnd,
            RenderManager manager)
        {
            if (manager == null || manager.meshes == null)
            {
                Debug.LogError("[EnvironmentScatter] Init needs the render manager and its primitive meshes.");
                return;
            }

            _meshes = manager.meshes;
            _surfaceY = surfaceY;
            ConfigureMotion(viewCamera, gust, fogEnd);
            Build(board, cellSize);
        }

        void Update()
        {
            if (!_viewCamera)
            {
                _viewCamera = Camera.main;
            }

            bool isLookApplied = Shader.GetGlobalFloat(lookAppliedId) > 0.5f;
            float distance = isLookApplied ? Shader.GetGlobalFloat(fogEndId) : _farDistance;
            Vector3 cameraPosition = _viewCamera ? _viewCamera.transform.position : Vector3.zero;
            Animate(Time.timeAsDouble, cameraPosition, _viewCamera ? distance : float.MaxValue);
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

        public static Rect GridRect(GridManager grid)
        {
            Vector3 center = grid.transform.position;
            float width = grid.width * grid.size;
            float height = grid.height * grid.size;
            return new Rect(center.x - width * 0.5f, center.z - height * 0.5f, width, height);
        }

        public void ConfigureMotion(Camera viewCamera, EnvironmentGust gust, float fogEnd)
        {
            _viewCamera = viewCamera;
            _gust = gust;
            _farDistance = Mathf.Max(1f, fogEnd);
        }

        // Sampled from the absolute clock, so a plant coming back into range needs no catch-up on missed frames
        public void Animate(double time, Vector3 cameraPosition, float distance)
        {
            Vector3 wind = _gust ? _gust.Sample(time) : Vector3.zero;
            float age = Mathf.Max(0f, (float)(time - _builtAt));
            float settle = 0f;
            if (age < 1.2f)
            {
                settle = Mathf.Sin(age * 12f) * Mathf.Exp(-age * 5f) * 3f;
            }

            for (int i = 0; i < _swaying.Count; i++)
            {
                Motion motion = _swaying[i];
                if (!motion.pivot || (motion.anchor.position - cameraPosition).sqrMagnitude > distance * distance)
                {
                    continue;
                }

                double cycle = time * motion.frequency * 2 * System.Math.PI % (2 * System.Math.PI);
                float wave = Mathf.Sin((float)cycle + motion.phase);
                Vector3 localWind = motion.pivot.parent.InverseTransformDirection(wind);
                Quaternion push = Quaternion.Euler(localWind.z * motion.amplitude * 2f, 0f,
                    -localWind.x * motion.amplitude * 2f - wind.magnitude * motion.uncurl);
                float swing = wave * motion.amplitude;
                Quaternion sway = Quaternion.Euler(swing * 0.35f, 0f, swing + settle);
                motion.pivot.localRotation = push * motion.rest * sway;
            }
        }

        public void Build(Rect gridRect, float cellSize)
        {
            if (_meshes == null)
            {
                Debug.LogError("[EnvironmentScatter] Build needs the primitive meshes.");
                return;
            }

            Clear();
            _builtAt = Time.timeAsDouble;
            _items = EnvironmentLayout.Generate(_settings, gridRect, cellSize, _surfaceY);
            _root = new GameObject("EnvironmentItems").transform;
            _root.SetParent(transform, false);
            _properties = new MaterialPropertyBlock();
            foreach (EnvironmentItem item in _items)
            {
                Spawn(item);
            }
        }

        public void Clear()
        {
            _swaying.Clear();
            _plantCount = 0;
            if (_root)
            {
                _root.gameObject.SetActive(false);
                Destroy(_root.gameObject);
            }

            _root = null;
            foreach (Mesh mesh in _ownedMeshes)
            {
                Destroy(mesh);
            }

            _ownedMeshes.Clear();
        }

        public static Color VaryColor(Color colour, uint seed)
        {
            StoneRandom random = new StoneRandom(seed);
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            float hue = Mathf.Repeat(h + random.Range(-6f, 6f) / 360f, 1f);
            float value = Mathf.Clamp01(v * random.Range(0.92f, 1.08f));
            return Color.HSVToRGB(hue, s, value);
        }

        void Spawn(EnvironmentItem item)
        {
            Transform pivot = new GameObject(item.kind.ToString()).transform;
            pivot.SetParent(_root, false);
            pivot.position = item.position;
            pivot.rotation = Quaternion.Euler(0f, item.yaw, 0f);
            StoneRandom random = new StoneRandom(item.seed);
            float s = item.scale;
            _colourSeed = item.seed;
            switch (item.kind)
            {
                case EnvironmentKind.Boulder:
                    Stone(pivot, item.seed, StonePresets.Boulder, Vector3.zero, s, item.paletteIndex);
                    break;
                case EnvironmentKind.Monolith:
                    int palette = item.paletteIndex % 2 == 0 ? 1 : 2;
                    Stone(pivot, item.seed, StonePresets.Monolith, Vector3.zero, s, palette);
                    break;
                case EnvironmentKind.Cairn:
                    SpawnCairn(pivot, item, random, s);
                    break;
                case EnvironmentKind.MushroomTree:
                    SpawnMushroomTree(pivot, item, random, s);
                    break;
                case EnvironmentKind.SpiralFern:
                    SpawnSpiralFern(pivot, item, random, s);
                    break;
                case EnvironmentKind.BladeRosette:
                    SpawnBladeRosette(pivot, item, random, s);
                    break;
                case EnvironmentKind.SphereCluster:
                    SpawnSphereCluster(pivot, item, random, s);
                    break;
            }
        }

        // Two to four stones shrinking upward, each resting on the one below
        void SpawnCairn(Transform pivot, EnvironmentItem item, StoneRandom random, float s)
        {
            int layers = 2 + (int)(random.Next01() * 3f);
            float y = 0f;
            for (int i = 0; i < layers; i++)
            {
                float k = s * (1f - i * 0.2f);
                Vector3 bottom = new Vector3(random.Range(-0.08f, 0.08f) * s, y, random.Range(-0.08f, 0.08f) * s);
                uint seed = StoneSeed.ForPart(item.seed, (uint)i + 1);
                Vector3 part = Stone(pivot, seed, StonePresets.Cairn, bottom, k, (item.paletteIndex + i) & 3);
                y += part.y * 0.8f;
            }
        }

        void SpawnMushroomTree(Transform pivot, EnvironmentItem item, StoneRandom random, float s)
        {
            float stem = random.Range(2.2f, 4.2f) * s;
            Vector3 stemScale = new Vector3(0.22f * s, stem, 0.22f * s);
            Color stemColor = new Color(0.65f, 0.82f, 0.62f);
            Part(pivot, _meshes.capsule, Vector3.zero, Quaternion.identity, stemScale, stemColor);

            Transform cap = new GameObject("NoddingCap").transform;
            cap.SetParent(pivot, false);
            cap.localPosition = Vector3.up * (stem * 0.92f);
            Vector3 underScale = new Vector3(1.5f, 0.14f, 1.5f) * s;
            Part(cap, _meshes.sphere, Vector3.down * 0.08f * s, Quaternion.identity, underScale, Greens[0]);

            bool isCone = random.Next01() < 0.4f;
            Mesh top = isCone ? _meshes.cone : _meshes.sphere;
            Vector3 topScale = isCone ? new Vector3(1.4f, 0.7f, 1.4f) * s : new Vector3(1.6f, 0.45f, 1.6f) * s;
            Color topColor = random.Next01() < 0.5f ? new Color(0.44f, 0.74f, 0.61f) : new Color(0.64f, 0.78f, 0.65f);
            Part(cap, top, Vector3.zero, Quaternion.identity, topScale, topColor);

            Sway(pivot, item.seed, Mathf.Clamp(stem * 0.45f, 0.6f, 3f));
            AddMotion(cap, pivot, item.seed + 1, 1.4f, 0f);
        }

        void SpawnSpiralFern(Transform pivot, EnvironmentItem item, StoneRandom random, float s)
        {
            int fronds = 3 + (int)(random.Next01() * 3f);
            for (int f = 0; f < fronds; f++)
            {
                Transform frond = new GameObject("Frond").transform;
                frond.SetParent(pivot, false);
                frond.localRotation = Quaternion.Euler(0f, f * 360f / fronds + random.Range(-15f, 15f), 0f);
                Transform linkParent = frond;
                float length = 0.3f * s;
                for (int i = 0; i < 9; i++)
                {
                    Transform joint = new GameObject("CurlJoint").transform;
                    joint.SetParent(linkParent, false);
                    joint.localPosition = i == 0 ? Vector3.zero : Vector3.up * length / 0.88f;
                    float curl = i == 0 ? random.Range(10f, 25f) : 22f + i * 5f;
                    joint.localRotation = Quaternion.Euler(0f, 0f, -curl);
                    Vector3 scale = new Vector3(length * 0.9f, length * 1.15f, length * 0.9f);
                    Color color = Color.Lerp(Greens[1], Greens[2], i / 8f);
                    Part(joint, _meshes.sphere, Vector3.zero, Quaternion.identity, scale, color);
                    // A positive correction opens each negative curl angle during a gust
                    AddMotion(joint, pivot, item.seed + (uint)(f * 16 + i), 0.12f * s, -3f);
                    linkParent = joint;
                    length *= 0.88f;
                }
            }

            Sway(pivot, item.seed, 1.6f * s);
        }

        void SpawnBladeRosette(Transform pivot, EnvironmentItem item, StoneRandom random, float s)
        {
            int blades = 7 + (int)(random.Next01() * 5f);
            for (int i = 0; i < blades; i++)
            {
                float around = i * 360f / blades + random.Range(-10f, 10f);
                Quaternion rotation = Quaternion.Euler(0f, around, random.Range(15f, 45f));
                Vector3 scale = new Vector3(0.32f * s, random.Range(0.8f, 1.6f) * s, 0.065f * s);
                Color color = Color.Lerp(Greens[0], Greens[2], random.Next01());
                Part(pivot, _meshes.cone, Vector3.zero, rotation, scale, color);
            }

            Sway(pivot, item.seed, 0.45f * s);
        }

        void SpawnSphereCluster(Transform pivot, EnvironmentItem item, StoneRandom random, float s)
        {
            int stems = 3 + (int)(random.Next01() * 3f);
            for (int i = 0; i < stems; i++)
            {
                float around = i * 360f / stems + random.Range(-20f, 20f);
                Quaternion tilt = Quaternion.Euler(0f, around, random.Range(0f, 15f));
                float h = random.Range(0.6f, 1.6f) * s;
                float d = random.Range(0.3f, 0.55f) * s;
                Vector3 stemScale = new Vector3(0.05f * s, h, 0.05f * s);
                Part(pivot, _meshes.cylinder, Vector3.zero, tilt, stemScale, Greens[1]);
                Vector3 bottom = tilt * Vector3.up * h - Vector3.up * (d * 0.2f);
                Color color = i % 2 == 0 ? Greens[2] : Greens[3];
                Part(pivot, _meshes.sphere, bottom, Quaternion.identity, Vector3.one * d, color);
            }

            Sway(pivot, item.seed, 1.2f * s);
        }

        void Sway(Transform pivot, uint seed, float degrees)
        {
            _plantCount++;
            AddMotion(pivot, pivot, seed, degrees, 0f);
        }

        void AddMotion(Transform pivot, Transform anchor, uint seed, float degrees, float uncurl)
        {
            StoneRandom random = new StoneRandom(seed);
            _swaying.Add(new Motion
            {
                pivot = pivot,
                anchor = anchor,
                rest = pivot.localRotation,
                frequency = random.Range(0.2f, 0.4f),
                phase = random.Range(0f, Mathf.PI * 2f),
                amplitude = degrees,
                uncurl = uncurl
            });
        }

        // Places the part so the lowest point of its mesh sits on bottom, along the part's own up axis.
        // Returns its world size.
        Vector3 Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Quaternion rotation,
            Vector3 scale, Color color, string name)
        {
            GameObject partGo = new GameObject(name);
            partGo.transform.SetParent(parent, false);
            partGo.transform.localRotation = rotation;
            partGo.transform.localScale = scale;
            partGo.transform.localPosition = bottom + rotation * new Vector3(0f, -mesh.bounds.min.y * scale.y, 0f);
            partGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = partGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            _properties.SetColor("_BaseColor", VaryColor(color, _colourSeed).linear);
            meshRenderer.SetPropertyBlock(_properties);
            return Vector3.Scale(mesh.bounds.size, scale);
        }

        void Part(Transform parent, Mesh mesh, Vector3 bottom, Quaternion rotation, Vector3 scale, Color color)
        {
            Part(parent, mesh, _plantMaterial, bottom, rotation, scale, color, mesh.name);
        }

        Vector3 Stone(Transform parent, uint seed, StoneSettings shape, Vector3 bottom, float scale, int palette)
        {
            Mesh mesh = StoneMesh.CreateMesh(seed, shape);
            mesh.name = "EnvironmentStone";
            _ownedMeshes.Add(mesh);
            // Sink each stone a little into the ground so it reads as rooted
            Vector3 sunkBottom = bottom - Vector3.up * (mesh.bounds.size.y * scale * 0.12f);
            Color color = StoneAssembly.Palette[Mathf.Clamp(palette, 0, 3)];
            Vector3 size = Vector3.one * scale;
            return Part(parent, mesh, _stoneMaterial, sunkBottom, Quaternion.identity, size, color, "Stone");
        }
    }
}
