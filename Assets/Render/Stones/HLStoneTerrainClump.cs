using HealerLike.Render.Zones;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    public class HLStoneTerrainClump : MonoBehaviour
    {
        [FormerlySerializedAs("stoneMaterial")]
        [SerializeField] Material _stoneMaterial;
        [FormerlySerializedAs("groundShadowEnabled")]
        [SerializeField] bool _groundShadowEnabled = true;
        [FormerlySerializedAs("directionToKeyLight")]
        [SerializeField] Vector3 _directionToKeyLight = new Vector3(-1f, 2f, -1f);
        [SerializeField] StoneGroundDisc _groundDisc;
        [SerializeField] StoneGroundDisc _groundShadow;
        [SerializeField] MeshFilter _ochreFace;
        [SerializeField] HLTrampleZone _trample;

        HLStoneLife _life;
        StoneMeshCache _ownMeshes;
        Mesh _ochreMesh;

        readonly HLStoneAssembly _assembly = new HLStoneAssembly();
        public HLStoneAssembly assembly { get { return _assembly; } }

        public float bareGroundRadius { get { return _groundDisc != null ? _groundDisc.radius : 0f; } }

        public Vector3 bareGroundCenter
        {
            get
            {
                return _groundDisc != null ? _groundDisc.center : transform.position;
            }
        }

        public Color groundColour
        {
            get
            {
                return _groundDisc != null ? _groundDisc.colour : Color.clear;
            }
            set
            {
                if (_groundDisc != null)
                {
                    _groundDisc.colour = value;
                }
            }
        }

        public bool groundShadowEnabled
        {
            get
            {
                return _groundShadowEnabled;
            }
            set
            {
                _groundShadowEnabled = value;
                if (_groundShadow != null)
                {
                    _groundShadow.Show(value && isActiveAndEnabled);
                }
            }
        }

        // Without an effects owner the clump keeps its own stone meshes
        public void Init(uint seed, float cellSize, HLStoneEffects effects, HLZoneRegistry zones)
        {
            if (!float.IsFinite(cellSize) || cellSize <= 0f)
            {
                Debug.LogError($"[HLStoneTerrainClump] Cell size must be positive, not {cellSize}.");
                return;
            }

            ClearFace();
            if (effects != null)
            {
                _assembly.Init(effects.stoneMeshes);
            }
            else
            {
                if (_ownMeshes == null)
                {
                    _ownMeshes = new StoneMeshCache();
                }
                _assembly.Init(_ownMeshes);
            }

            HLStoneRandom random = new HLStoneRandom(HLStoneSeed.ForPart(seed, 401));
            int count = 3 + (int)(random.Next() % 3);
            int silhouette = (int)(seed % 3);
            for (int i = 0; i < count; i++)
            {
                // The random draws below keep their order: size, elongation, depth, palette, yaw.
                float size = 0.65f;
                if (i > 0)
                {
                    size = random.Range(0.27f, 0.42f);
                }

                float elongation;
                if (i > 0)
                {
                    elongation = random.Range(0.7f, 1.35f);
                }
                else if (silhouette == 0)
                {
                    elongation = 2.4f;
                }
                else if (silhouette == 1)
                {
                    elongation = 0.8f;
                }
                else
                {
                    elongation = 1.45f;
                }

                HLStonePart recipe = new HLStonePart();
                recipe.shape = HLStonePresets.Shape(size, elongation, random.Range(0.6f, 1f), 0.16f, 0);
                recipe.seedSalt = 501u + (uint)i;
                recipe.paletteIndex = (int)(random.Next() % 3);
                recipe.localEulerAngles = new Vector3(0f, random.Range(0f, 360f), 0f);
                if (i > 0)
                {
                    recipe.localPosition = new Vector3(Mathf.Cos(i * 2.4f) * 0.28f, 0f, Mathf.Sin(i * 2.4f) * 0.28f);
                }
                if (!_assembly.Add(transform, seed, recipe, _stoneMaterial))
                {
                    return;
                }

                HLStoneAssembly.Part part = _assembly.parts[i];
                Vector3 position = part.transform.localPosition;
                position.y = -part.lease.data.bounds.min.y;
                part.transform.localPosition = position;
            }

            _assembly.Fit(0.96f, random.Range(0.7f, 1.2f));
            foreach (HLStoneAssembly.Part part in _assembly.parts)
            {
                part.transform.localPosition *= cellSize;
                part.transform.localScale *= cellSize;
            }
            _assembly.RecalculateBounds();

            if (_groundShadow != null)
            {
                _groundShadow.Init(_assembly.localBounds, _directionToKeyLight);
                _groundShadow.Show(_groundShadowEnabled && isActiveAndEnabled);
            }
            if (_groundDisc != null)
            {
                _groundDisc.Init(_assembly.localBounds, _directionToKeyLight);
                _groundDisc.Show(isActiveAndEnabled);
            }
            if (_life == null)
            {
                _life = gameObject.AddComponent<HLStoneLife>();
            }
            _life.Init(effects, zones, seed, bareGroundRadius, true);

            // The bare disc may sit off the pivot, the flattened grass follows the disc
            if (_trample != null)
            {
                _trample.transform.position = bareGroundCenter;
                _trample.radius = HLTrampleZone.TrampleRadius(bareGroundRadius);
                _trample.Init(zones);
            }

            if (HLStoneLifeState.Ochre(seed))
            {
                CreateFace();
            }
        }

        void CreateFace()
        {
            if (_ochreFace == null)
            {
                return;
            }

            HLStoneAssembly.Part part = _assembly.parts[0];
            Mesh mesh = part.lease.mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int face = 0;
            for (int i = 3; i < vertices.Length; i += 3)
            {
                if (normals[i].x + normals[i].y * 0.5f > normals[face].x + normals[face].y * 0.5f)
                {
                    face = i;
                }
            }

            Vector3 lift = normals[face] * 0.002f;
            _ochreMesh = new Mesh { name = "HLOchreFacet" };
            _ochreMesh.vertices = new Vector3[]
            {
                vertices[face] + lift,
                vertices[face + 1] + lift,
                vertices[face + 2] + lift
            };
            _ochreMesh.triangles = new int[] { 0, 1, 2 };
            _ochreMesh.RecalculateNormals();
            _ochreMesh.RecalculateBounds();

            // The facet rides on the first part, which is rebuilt on every Init
            _ochreFace.sharedMesh = _ochreMesh;
            _ochreFace.transform.SetParent(part.transform, false);
            _ochreFace.gameObject.SetActive(true);
            MeshRenderer renderer = _ochreFace.GetComponent<MeshRenderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetVector("_BaseColor", HLStoneAssembly.Palette[3].linear);
            renderer.SetPropertyBlock(block);
        }

        void ClearFace()
        {
            if (_ochreFace != null)
            {
                _ochreFace.gameObject.SetActive(false);
                _ochreFace.transform.SetParent(transform, false);
                _ochreFace.sharedMesh = null;
            }
            DestroyFaceMesh();
        }

        void DestroyFaceMesh()
        {
            if (_ochreMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_ochreMesh);
                }
                else
                {
                    DestroyImmediate(_ochreMesh);
                }
                _ochreMesh = null;
            }
        }

        void SetVisible(bool value)
        {
            foreach (HLStoneAssembly.Part part in _assembly.parts)
            {
                if (part.transform != null)
                {
                    part.transform.gameObject.SetActive(value);
                }
            }
            if (_groundShadow != null)
            {
                _groundShadow.Show(value && _groundShadowEnabled);
            }
            if (_groundDisc != null)
            {
                _groundDisc.Show(value);
            }
            if (_life != null)
            {
                _life.enabled = value;
            }
        }

        void OnEnable()
        {
            SetVisible(true);
        }

        void OnDisable()
        {
            SetVisible(false);
        }

        void OnDestroy()
        {
            DestroyFaceMesh();
            _assembly.Dispose();
            if (_ownMeshes != null)
            {
                _ownMeshes.Clear();
            }
        }
    }
}
