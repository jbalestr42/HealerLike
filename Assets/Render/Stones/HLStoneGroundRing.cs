using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    public class HLStoneGroundRing : MonoBehaviour
    {
        [FormerlySerializedAs("groundColour")]
        [SerializeField] Color _groundColour = new Color32(70, 111, 87, 255);

        Transform _disc;
        Mesh _mesh;
        Material _material;
        float _radius;

        public Color groundColour
        {
            get
            {
                return _groundColour;
            }
            set
            {
                _groundColour = value;
                if (_material != null)
                {
                    ApplyColour();
                }
            }
        }

        public float radius
        {
            get
            {
                return _radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            }
        }

        public Vector3 center { get { return _disc != null ? _disc.position : transform.position; } }

        public void Configure(Bounds bounds)
        {
            _radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.18f;
            if (_disc == null)
            {
                CreateDisc();
            }
            ApplyColour();
            _disc.localPosition = new Vector3(bounds.center.x, bounds.min.y + 0.006f, bounds.center.z);
            _disc.localScale = new Vector3(_radius, 1f, _radius);
        }

        void CreateDisc()
        {
            _disc = new GameObject("HLBareGround").transform;
            _disc.SetParent(transform, false);
            _disc.gameObject.layer = gameObject.layer;

            int segments = 32;
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float edge = 0.87f + 0.08f * Mathf.Sin(angle * 5f) + 0.05f * Mathf.Cos(angle * 9f);
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * edge, 0f, Mathf.Sin(angle) * edge);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % segments + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            _mesh = new Mesh { name = "HLBareGroundDisc" };
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _disc.gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;

            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "HLBareGroundMaterial" };
            MeshRenderer renderer = _disc.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void ApplyColour()
        {
            Color colour = _groundColour;
            colour.a = 1f;
            _material.SetColor("_BaseColor", colour.linear);
        }

        void OnEnable()
        {
            if (_disc != null)
            {
                _disc.gameObject.SetActive(true);
            }
        }

        void OnDisable()
        {
            if (_disc != null)
            {
                _disc.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (_disc != null)
            {
                HLStoneMeshCache.DestroyOwned(_disc.gameObject);
            }
            HLStoneMeshCache.DestroyOwned(_mesh);
            HLStoneMeshCache.DestroyOwned(_material);
        }
    }
}
