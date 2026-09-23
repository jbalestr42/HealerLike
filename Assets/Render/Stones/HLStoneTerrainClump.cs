using System;
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

        HLStoneGroundShadow _groundShadow;
        HLStoneGroundRing _groundRing;
        HLStoneLife _life;
        Mesh _ochreMesh;
        GameObject _ochreFace;

        readonly HLStoneAssembly _assembly = new HLStoneAssembly();
        public HLStoneAssembly assembly { get { return _assembly; } }

        public float bareGroundRadius { get { return _groundRing != null ? _groundRing.radius : 0f; } }

        public Vector3 bareGroundCenter
        {
            get
            {
                return _groundRing != null ? _groundRing.center : transform.position;
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
                    _groundShadow.visible = value;
                }
            }
        }

        public void Initialize(uint seed, float cellSize)
        {
            if (!float.IsFinite(cellSize) || cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            }

            ClearFace();
            _assembly.Dispose();
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
                _assembly.Add(transform, seed, recipe, _stoneMaterial);

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

            if (_groundShadow == null)
            {
                _groundShadow = gameObject.AddComponent<HLStoneGroundShadow>();
            }
            _groundShadow.Configure(_assembly.localBounds, _directionToKeyLight, _groundShadowEnabled);
            if (_groundRing == null)
            {
                _groundRing = gameObject.AddComponent<HLStoneGroundRing>();
            }
            _groundRing.Configure(_assembly.localBounds);
            if (_life == null)
            {
                _life = gameObject.AddComponent<HLStoneLife>();
            }
            _life.Configure(null, seed, bareGroundRadius, true);
            if (HLStoneLifeState.Ochre(seed))
            {
                CreateFace();
            }
        }

        void CreateFace()
        {
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

            _ochreFace = new GameObject("HLOchreFace");
            _ochreFace.layer = gameObject.layer;
            _ochreFace.transform.SetParent(part.transform, false);
            _ochreFace.AddComponent<MeshFilter>().sharedMesh = _ochreMesh;
            MeshRenderer renderer = _ochreFace.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _stoneMaterial;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetVector("_BaseColor", HLStoneAssembly.Palette[3].linear);
            renderer.SetPropertyBlock(block);
        }

        void ClearFace()
        {
            HLStoneMeshCache.DestroyOwned(_ochreFace);
            HLStoneMeshCache.DestroyOwned(_ochreMesh);
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
                _groundShadow.enabled = value;
            }
            if (_groundRing != null)
            {
                _groundRing.enabled = value;
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
            ClearFace();
            _assembly.Dispose();
        }
    }
}
