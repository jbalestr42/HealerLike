using System.Collections.Generic;
using HealerLike.Render.Stage;
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
        StageKeyLight _keyLight;
        bool _isShown;

        public bool isShadow { get { return _isShadow; } }

        // Bounds are in the parent's space, the light direction in world space; without a key light the shadow
        // always shows when its owner shows it
        public void Init(Bounds localBounds, Vector3 directionToLight, StageKeyLight keyLight)
        {
            _bounds = localBounds;
            _directionToLight = directionToLight;
            _keyLight = keyLight;
            Refresh();
        }

        public void Show(bool show)
        {
            _isShown = show;
            UpdateVisibility();
        }

        public void Refresh()
        {
            UpdateVisibility();
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

        // Real shadows from the key light take over from a cast shadow, whatever its owner shows
        void UpdateVisibility()
        {
            bool isReplaced = _isShadow && _keyLight != null && _keyLight.realShadows;
            bool isVisible = _isShown && !isReplaced;
            if (gameObject.activeSelf != isVisible)
            {
                gameObject.SetActive(isVisible);
            }
        }

        // The parts' box in the given space, the space Init takes its bounds in
        public static Bounds Measure(Transform space, IReadOnlyList<Transform> partTransforms)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool isEmpty = true;
            foreach (Transform part in partTransforms)
            {
                Bounds world = part.GetComponent<Renderer>().bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3(CornerSign(corner, 1), CornerSign(corner, 2), CornerSign(corner, 4));
                    Vector3 point = space.InverseTransformPoint(world.center + Vector3.Scale(world.extents, sign));
                    if (isEmpty)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        isEmpty = false;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }
            return bounds;
        }

        // -1 or 1 along one axis of a box corner, the axis picked by its bit
        static float CornerSign(int corner, int bit)
        {
            if ((corner & bit) == 0)
            {
                return -1f;
            }
            return 1f;
        }
    }
}
