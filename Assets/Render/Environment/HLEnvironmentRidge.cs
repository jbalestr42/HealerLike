using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grass;
using HealerLike.Render.Stage;
using HealerLike.Render.Stones;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace HealerLike.Render.Environment
{
    // A row of tall silhouettes past the far edge, each standing so its mid-height sits in the last visible fog band.
    public class HLEnvironmentRidge : MonoBehaviour
    {
        public static readonly uint Salt = 0x52494447u;
        public static readonly float SpreadX = 14f;
        public static readonly float GridClearance = 3f;
        // The cap's bottom sits this far below the stem top, in cap thicknesses; the rest stands above it
        static readonly float capSink = 0.7f;

        [FormerlySerializedAs("stageCamera")]
        [SerializeField] Camera _stageCamera;
        [FormerlySerializedAs("stoneMaterial")]
        [SerializeField] Material _stoneMaterial;
        [FormerlySerializedAs("plantMaterial")]
        [SerializeField] Material _plantMaterial;
        [FormerlySerializedAs("grid")]
        [SerializeField] Rect _grid = new Rect(-8f, -8f, 16f, 16f);
        [FormerlySerializedAs("groundY")]
        [SerializeField] float _groundY = 0.5f;
        [FormerlySerializedAs("seed")]
        [SerializeField] int _seed = 1707;
        [FormerlySerializedAs("fogStart")]
        [SerializeField] float _fogStart = 43.837f;
        [FormerlySerializedAs("fogEnd")]
        [SerializeField] float _fogEnd = 50.356f;
        [FormerlySerializedAs("fogBands")]
        [SerializeField] int _fogBands = 6;
        [FormerlySerializedAs("stoneColor")]
        [SerializeField] Color _stoneColor = new Color32(168, 184, 172, 255);
        [FormerlySerializedAs("stemColor")]
        [SerializeField] Color _stemColor = new Color32(184, 200, 180, 255);
        [FormerlySerializedAs("capColor")]
        [SerializeField] Color _capColor = new Color32(156, 180, 162, 255);

        readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        MaterialPropertyBlock _properties;
        RenderManager _manager;
        HLPrimitiveMeshes _meshes;

        // Runtime meshes for the stage scene, removed in D2
        HLPrimitiveMeshes _stageMeshes;

        List<HLRidgeItem> _items = new List<HLRidgeItem>();
        public IReadOnlyList<HLRidgeItem> items { get { return _items; } }

        Transform _root;
        public Transform root { get { return _root; } }

        public void Init(Camera stageCamera, Rect board, float surfaceY, float fogStart, float fogEnd, RenderManager manager)
        {
            if (manager == null || manager.meshes == null)
            {
                Debug.LogError("[HLEnvironmentRidge] Init needs the render manager and its primitive meshes.");
                return;
            }

            _manager = manager;
            _meshes = manager.meshes;
            _stageCamera = stageCamera;
            _grid = board;
            _groundY = surfaceY;
            _fogStart = fogStart;
            _fogEnd = fogEnd;
            Build();
        }

        // The stage scene builds from its own camera until the render manager attaches it, removed in D2
        void Start()
        {
            if (_manager == null)
            {
                Build();
            }
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
            if (_stageMeshes != null)
            {
                StageSceneMeshes.Release(_stageMeshes);
                _stageMeshes = null;
            }
        }

        // The stage builder wires the scene instance, removed in D2
        public void Configure(Camera stage, Material stones, Material plants, Rect gridRect, float surfaceY,
            float start, float end, int bands, int layoutSeed)
        {
            _stageCamera = stage;
            _stoneMaterial = stones;
            _plantMaterial = plants;
            _grid = gridRect;
            _groundY = surfaceY;
            _fogStart = start;
            _fogEnd = end;
            _fogBands = bands;
            _seed = layoutSeed;
        }

        // Camera distances of the last visible fog band, [min, max); zero and a log when the fog is not valid
        public static Vector2 LastBand(float fogStart, float fogEnd, int fogBands)
        {
            if (fogBands < 1 || !(fogStart >= 0f) || !(fogEnd > fogStart) || !float.IsFinite(fogEnd))
            {
                Debug.LogError($"[HLEnvironmentRidge] Rejected fog from {fogStart} to {fogEnd} in {fogBands} bands.");
                return Vector2.zero;
            }

            return new Vector2(fogStart + (fogBands - 1f) / fogBands * (fogEnd - fogStart), fogEnd);
        }

        public static Vector3 MidHeight(HLRidgeItem item)
        {
            return item.position + Vector3.up * (item.height * 0.5f);
        }

        // Eight to twelve monoliths and eight to twelve mushroom stems across x in [-SpreadX, SpreadX],
        // past the far (+z) edge.
        // Items the band cannot reach are clamped to z >= grid.yMax + GridClearance.
        // Returns no items and logs when an input is not valid.
        public static List<HLRidgeItem> Layout(Vector3 cameraPosition, float fogStart, float fogEnd, int fogBands,
            Rect grid, float groundY, int seed)
        {
            Vector2 band = LastBand(fogStart, fogEnd, fogBands);
            if (band.y <= 0f)
            {
                return new List<HLRidgeItem>();
            }

            if (!(grid.width > 0f) || !(grid.height > 0f) || !float.IsFinite(groundY))
            {
                Debug.LogError($"[HLEnvironmentRidge] Rejected grid {grid} with ground {groundY}.");
                return new List<HLRidgeItem>();
            }

            uint baseSeed = HLStoneSeed.ForPart((uint)seed, Salt);
            HLStoneRandom random = new HLStoneRandom(baseSeed);
            int monoliths = 8 + (int)(random.Next01() * 5f);
            int mushrooms = 8 + (int)(random.Next01() * 5f);
            int total = monoliths + mushrooms;

            // Deal the kinds into x slots with a seeded shuffle so the row alternates irregularly
            HLRidgeKind[] kinds = new HLRidgeKind[total];
            for (int i = 0; i < total; i++)
            {
                kinds[i] = i < monoliths ? HLRidgeKind.Monolith : HLRidgeKind.Mushroom;
            }

            for (int i = total - 1; i > 0; i--)
            {
                int j = (int)(random.Next01() * (i + 1));
                HLRidgeKind swap = kinds[i];
                kinds[i] = kinds[j];
                kinds[j] = swap;
            }

            float spacing = 2f * SpreadX / total;
            float depth = band.y - band.x;
            List<HLRidgeItem> result = new List<HLRidgeItem>(total);
            for (int i = 0; i < total; i++)
            {
                HLRidgeItem item = new HLRidgeItem
                {
                    kind = kinds[i],
                    yaw = random.Range(0f, 360f),
                    seed = HLStoneSeed.ForPart(baseSeed, (uint)i + 1)
                };
                if (item.kind == HLRidgeKind.Monolith)
                {
                    item.height = random.Range(6f, 12f);
                    item.width = item.height * random.Range(0.18f, 0.26f);
                }
                else
                {
                    float stem = random.Range(7f, 14f);
                    item.width = random.Range(0.35f, 0.6f);
                    item.capDiameter = random.Range(2.5f, 5f);
                    item.capThickness = item.capDiameter * random.Range(0.25f, 0.35f);
                    item.height = stem + (1f - capSink) * item.capThickness;
                }

                float slot = -SpreadX + (i + 0.5f) * spacing + random.Range(-0.35f, 0.35f) * spacing;
                float x = Mathf.Clamp(slot, -SpreadX, SpreadX);
                float distance = random.Range(band.x + 0.2f * depth, band.y - 0.2f * depth);
                float dx = x - cameraPosition.x;
                float dy = groundY + item.height * 0.5f - cameraPosition.y;
                float reach = distance * distance - dx * dx - dy * dy;
                float z = cameraPosition.z + Mathf.Sqrt(Mathf.Max(0f, reach));
                item.position = new Vector3(x, groundY, Mathf.Max(z, grid.yMax + GridClearance));
                result.Add(item);
            }

            return result;
        }

        public void Build()
        {
            if (_stageCamera)
            {
                Build(_stageCamera.transform.position);
            }
        }

        public void Build(Vector3 cameraPosition)
        {
            if (_manager == null && _meshes == null)
            {
                _stageMeshes = StageSceneMeshes.Create();
                _meshes = _stageMeshes;
            }

            Clear();
            _items = Layout(cameraPosition, _fogStart, _fogEnd, _fogBands, _grid, _groundY, _seed);
            _root = new GameObject("HLRidgeItems").transform;
            _root.SetParent(transform, false);
            _properties = new MaterialPropertyBlock();
            foreach (HLRidgeItem item in _items)
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

        void Spawn(HLRidgeItem item)
        {
            Transform pivot = new GameObject(item.kind.ToString()).transform;
            pivot.SetParent(_root, true);
            pivot.position = item.position;
            pivot.rotation = Quaternion.Euler(0f, item.yaw, 0f);
            if (item.kind == HLRidgeKind.Monolith)
            {
                Mesh mesh = HLStoneMesh.CreateMesh(item.seed, HLStonePresets.Monolith);
                mesh.name = "HLRidgeStone";
                _ownedMeshes.Add(mesh);
                Vector3 size = mesh.bounds.size;
                float across = item.width / Mathf.Max(size.x, 0.0001f);
                Vector3 scale = new Vector3(across, item.height / Mathf.Max(size.y, 0.0001f), across);
                Part(pivot, mesh, _stoneMaterial, Vector3.zero, scale, _stoneColor);
                return;
            }

            float stem = item.height - (1f - capSink) * item.capThickness;
            Mesh capsule = _meshes.capsule;
            Part(pivot, capsule, _plantMaterial, Vector3.zero, new Vector3(item.width, stem, item.width), _stemColor);
            Mesh sphere = _meshes.sphere;
            Vector3 capBottom = Vector3.up * (stem - capSink * item.capThickness);
            Vector3 capScale = new Vector3(item.capDiameter, item.capThickness, item.capDiameter);
            Part(pivot, sphere, _plantMaterial, capBottom, capScale, _capColor);
        }

        void Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Vector3 scale, Color color)
        {
            GameObject partGo = new GameObject(mesh.name);
            partGo.transform.SetParent(parent, false);
            partGo.transform.localScale = scale;
            partGo.transform.localPosition = bottom + new Vector3(0f, -mesh.bounds.min.y * scale.y, 0f);
            partGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = partGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            _properties.SetColor("_BaseColor", color.linear);
            meshRenderer.SetPropertyBlock(_properties);
        }
    }
}
