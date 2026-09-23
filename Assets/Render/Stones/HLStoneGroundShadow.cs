using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stones
{
    // Cosmetic ground projection. Direction points toward the key light, in world space.
    public class HLStoneGroundShadow : MonoBehaviour
    {
        Mesh _mesh;
        Material _material;
        Bounds _bounds;
        Vector3 _directionToLight;

        Transform _disc;
        public Transform disc { get { return _disc; } }

        bool _visible = true;
        public bool visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
                if (_disc != null)
                {
                    _disc.gameObject.SetActive(value);
                }
            }
        }

        public void Configure(Bounds localBounds, Vector3 keyLightDirection, bool show)
        {
            _bounds = localBounds;
            _directionToLight = keyLightDirection;
            if (_disc == null)
            {
                CreateDisc();
            }
            visible = show;
            Refresh();
        }

        void CreateDisc()
        {
            _disc = new GameObject("HLGroundShadow").transform;
            _disc.SetParent(transform, false);
            _disc.gameObject.layer = gameObject.layer;

            int segments = 32;
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % segments + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            _mesh = new Mesh { name = "HLShadowDisc" };
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _disc.gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;

            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _material.name = "HLUltramarineShadow";
            _material.renderQueue = 2001;
            _material.SetColor("_BaseColor", ((Color)new Color32(43, 75, 143, 255)).linear);
            MeshRenderer renderer = _disc.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void Refresh()
        {
            if (_disc == null)
            {
                return;
            }

            Vector3 away = new Vector3(-_directionToLight.x, 0f, -_directionToLight.z);
            if (!float.IsFinite(away.sqrMagnitude) || away.sqrMagnitude < 1e-6f)
            {
                away = Vector3.forward;
            }
            away.Normalize();

            Vector3 scale = transform.lossyScale;
            float footprint = Mathf.Max(_bounds.size.x * Mathf.Abs(scale.x), _bounds.size.z * Mathf.Abs(scale.z));
            float width = Mathf.Max(0.05f, footprint * 0.55f);
            float length = Mathf.Max(width, _bounds.size.y * Mathf.Abs(scale.y) * 1.25f);
            Vector3 center = transform.TransformPoint(new Vector3(_bounds.center.x, _bounds.min.y, _bounds.center.z));
            _disc.position = center + away * length * 0.55f + Vector3.up * 0.012f;
            _disc.rotation = Quaternion.LookRotation(away, Vector3.up);

            // Compensate parent scale: the projection stays flat even when the model is scaled.
            _disc.localScale = Vector3.one;
            Vector3 inherited = _disc.lossyScale;
            _disc.localScale = new Vector3(
                width / Mathf.Max(0.0001f, Mathf.Abs(inherited.x)),
                0.001f / Mathf.Max(0.0001f, Mathf.Abs(inherited.y)),
                length / Mathf.Max(0.0001f, Mathf.Abs(inherited.z)));
        }

        void LateUpdate()
        {
            Refresh();
        }

        void OnEnable()
        {
            if (_disc != null)
            {
                _disc.gameObject.SetActive(_visible);
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
