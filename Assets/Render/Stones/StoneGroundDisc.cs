using UnityEngine;

namespace HealerLike.Render.Stones
{
    // A flat cast shadow on the ground under a stone, away from the key light
    public class StoneGroundDisc : MonoBehaviour
    {
        [SerializeField] MeshRenderer _renderer;
        [SerializeField] bool _isShadow;

        Bounds _bounds;
        Vector3 _directionToLight;
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

        // Bounds are in the parent's space, the light direction in world space
        public void Init(Bounds localBounds, Vector3 directionToLight)
        {
            _bounds = localBounds;
            _directionToLight = directionToLight;
            Refresh();
        }

        public void Show(bool show)
        {
            _isShown = show;
            gameObject.SetActive(_isShown && _isAllowed);
        }

        public void Refresh()
        {
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
    }
}
