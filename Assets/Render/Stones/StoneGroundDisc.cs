using UnityEngine;

namespace HealerLike.Render.Stones
{
    // A flat disc on the ground under a stone, either the bare earth or a cast shadow away from the key light
    public class StoneGroundDisc : MonoBehaviour
    {
        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");

        // The disc mesh is the unit circle; the bare earth used to have a wobbly edge averaging 0.87 of its radius
        static readonly float bareEdge = 0.87f;

        [SerializeField] MeshRenderer _renderer;
        [SerializeField] bool _isShadow;
        [SerializeField] Color _colour = new Color32(70, 111, 87, 255);

        MaterialPropertyBlock _block;
        Bounds _bounds;
        Vector3 _directionToLight;
        float _radius;
        bool _isShown;

        public bool isShadow { get { return _isShadow; } }

        // The key light turns cheap shadows off while it casts real ones, whatever the owner shows
        bool _isAllowed = true;
        public bool isAllowed
        {
            get
            {
                return _isAllowed;
            }
            set
            {
                _isAllowed = value;
                gameObject.SetActive(_isShown && _isAllowed);
            }
        }

        public Color colour
        {
            get
            {
                return _colour;
            }
            set
            {
                _colour = value;
                ApplyColour();
            }
        }

        // World radius of the bare earth, which covers the clump footprint
        public float radius
        {
            get
            {
                Vector3 scale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
                return _radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            }
        }

        public Vector3 center { get { return transform.position; } }

        // Bounds are in the parent's space, the light direction in world space
        public void Init(Bounds localBounds, Vector3 directionToLight)
        {
            _bounds = localBounds;
            _directionToLight = directionToLight;
            _radius = Mathf.Max(localBounds.extents.x, localBounds.extents.z) * 1.18f;
            ApplyColour();
            Refresh();
        }

        public void Show(bool show)
        {
            _isShown = show;
            gameObject.SetActive(_isShown && _isAllowed);
        }

        public void Refresh()
        {
            if (!_isShadow)
            {
                transform.localPosition = new Vector3(_bounds.center.x, _bounds.min.y + 0.006f, _bounds.center.z);
                transform.localRotation = Quaternion.identity;
                transform.localScale = new Vector3(_radius * bareEdge, 1f, _radius * bareEdge);
                return;
            }

            Vector3 away = new Vector3(-_directionToLight.x, 0f, -_directionToLight.z);
            if (!float.IsFinite(away.sqrMagnitude) || away.sqrMagnitude < 0.000001f)
            {
                away = Vector3.forward;
            }
            away.Normalize();

            Transform parent = transform.parent;
            Vector3 scale = parent.lossyScale;
            float footprint = Mathf.Max(_bounds.size.x * Mathf.Abs(scale.x), _bounds.size.z * Mathf.Abs(scale.z));
            float width = Mathf.Max(0.05f, footprint * 0.55f);
            float length = Mathf.Max(width, _bounds.size.y * Mathf.Abs(scale.y) * 1.25f);
            Vector3 center = parent.TransformPoint(new Vector3(_bounds.center.x, _bounds.min.y, _bounds.center.z));
            transform.position = center + away * length * 0.55f + Vector3.up * 0.012f;
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);

            // Compensate the parent scale so the projection stays flat on a scaled model
            transform.localScale = Vector3.one;
            Vector3 inherited = transform.lossyScale;
            transform.localScale = new Vector3(
                width / Mathf.Max(0.0001f, Mathf.Abs(inherited.x)),
                0.001f / Mathf.Max(0.0001f, Mathf.Abs(inherited.y)),
                length / Mathf.Max(0.0001f, Mathf.Abs(inherited.z)));
        }

        void ApplyColour()
        {
            if (_isShadow || _renderer == null)
            {
                return;
            }

            if (_block == null)
            {
                _block = new MaterialPropertyBlock();
            }

            Color colour = _colour;
            colour.a = 1f;
            _block.SetColor(baseColorId, colour.linear);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
