using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Environment
{
    // A row of tall silhouettes past the far edge, each standing so its mid-height sits in the last visible fog band.
    public class EnvironmentRidge : AEnvironmentSpawner
    {
        public static readonly uint Salt = 0x52494447u;
        public static readonly float SpreadX = 14f;
        public static readonly float GridClearance = 3f;
        // The cap's bottom sits this far below the stem top, in cap thicknesses; the rest stands above it
        static readonly float capSink = 0.7f;

        [SerializeField] int _seed = EnvironmentSettings.DefaultSeed;
        [SerializeField] int _fogBands = 6;
        [SerializeField] Color _stoneColor = new Color32(168, 184, 172, 255);
        [SerializeField] Color _stemColor = new Color32(184, 200, 180, 255);
        [SerializeField] Color _capColor = new Color32(156, 180, 162, 255);

        Camera _stageCamera;
        Rect _grid;
        float _groundY;
        float _fogStart;
        float _fogEnd;

        List<RidgeItem> _items = new List<RidgeItem>();
        public IReadOnlyList<RidgeItem> items { get { return _items; } }

        public void Init(Camera stageCamera, Rect board, float surfaceY, float fogStart, float fogEnd,
                         PrimitiveMeshes meshes)
        {
            if (meshes == null)
            {
                Debug.LogError("[EnvironmentRidge] Init needs the primitive meshes.");
                return;
            }

            _meshes = meshes;
            _stageCamera = stageCamera;
            _grid = board;
            _groundY = surfaceY;
            _fogStart = fogStart;
            _fogEnd = fogEnd;
            Build();
        }

        // Camera distances of the last visible fog band, [min, max); zero and a log when the fog is not valid
        public static Vector2 LastBand(float fogStart, float fogEnd, int fogBands)
        {
            if (fogBands < 1 || !(fogStart >= 0f) || !(fogEnd > fogStart) || !float.IsFinite(fogEnd))
            {
                Debug.LogError($"[EnvironmentRidge] Rejected fog from {fogStart} to {fogEnd} in {fogBands} bands.");
                return Vector2.zero;
            }

            return new Vector2(fogStart + (fogBands - 1f) / fogBands * (fogEnd - fogStart), fogEnd);
        }

        // Eight to twelve monoliths and eight to twelve mushroom stems across x in [-SpreadX, SpreadX],
        // past the far (+z) edge.
        // Items the band cannot reach are clamped to z >= grid.yMax + GridClearance.
        // Returns no items and logs when an input is not valid.
        public static List<RidgeItem> Layout(Vector3 cameraPosition, float fogStart, float fogEnd, int fogBands,
            Rect grid, float groundY, int seed)
        {
            Vector2 band = LastBand(fogStart, fogEnd, fogBands);
            if (band.y <= 0f)
            {
                return new List<RidgeItem>();
            }

            if (!(grid.width > 0f) || !(grid.height > 0f) || !float.IsFinite(groundY))
            {
                Debug.LogError($"[EnvironmentRidge] Rejected grid {grid} with ground {groundY}.");
                return new List<RidgeItem>();
            }

            uint baseSeed = SeededRandom.ForPart((uint)seed, Salt);
            SeededRandom random = new SeededRandom(baseSeed);
            int monoliths = 8 + (int)(random.Next01() * 5f);
            int mushrooms = 8 + (int)(random.Next01() * 5f);
            int total = monoliths + mushrooms;

            // Deal the kinds into x slots with a seeded shuffle so the row alternates irregularly
            RidgeKind[] kinds = new RidgeKind[total];
            for (int i = 0; i < total; i++)
            {
                kinds[i] = i < monoliths ? RidgeKind.Monolith : RidgeKind.Mushroom;
            }

            for (int i = total - 1; i > 0; i--)
            {
                int j = (int)(random.Next01() * (i + 1));
                RidgeKind swap = kinds[i];
                kinds[i] = kinds[j];
                kinds[j] = swap;
            }

            float spacing = 2f * SpreadX / total;
            float depth = band.y - band.x;
            List<RidgeItem> result = new List<RidgeItem>(total);
            for (int i = 0; i < total; i++)
            {
                RidgeItem item = new RidgeItem
                {
                    kind = kinds[i],
                    yaw = random.Range(0f, 360f),
                    seed = SeededRandom.ForPart(baseSeed, (uint)i + 1)
                };
                if (item.kind == RidgeKind.Monolith)
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

        void Build(Vector3 cameraPosition)
        {
            _items = Layout(cameraPosition, _fogStart, _fogEnd, _fogBands, _grid, _groundY, _seed);
            CreateRoot("RidgeItems");
            foreach (RidgeItem item in _items)
            {
                Spawn(item);
            }
        }

        void Spawn(RidgeItem item)
        {
            Transform pivot = new GameObject(item.kind.ToString()).transform;
            pivot.SetParent(root, true);
            pivot.position = item.position;
            pivot.rotation = Quaternion.Euler(0f, item.yaw, 0f);
            if (item.kind == RidgeKind.Monolith)
            {
                Mesh mesh = CreateStone(item.seed, StonePresets.Monolith, "RidgeStone");
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

        // Far silhouettes neither cast nor take shadows
        void Part(Transform parent, Mesh mesh, Material material, Vector3 bottom, Vector3 scale, Color color)
        {
            MeshRenderer meshRenderer = Part(parent, mesh, material, bottom, Quaternion.identity, scale, color,
                                             mesh.name);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }
    }
}
