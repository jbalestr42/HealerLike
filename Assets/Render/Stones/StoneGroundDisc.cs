using System.Collections.Generic;
using HealerLike.Render.Creatures;
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
        readonly BorrowedMeshCopies _meshCopies = new BorrowedMeshCopies();
        MeshFilter _filter;
        Mesh _sourceMesh;
        Mesh _meshCopy;

        public bool isShadow { get { return _isShadow; } }

        // Bounds are in the parent's space, the light direction in world space; without a key light the shadow
        // always shows when its owner shows it
        public void Init(Bounds localBounds, Vector3 directionToLight, StageKeyLight keyLight)
        {
            CopyMesh();
            _bounds = localBounds;
            _directionToLight = directionToLight;
            _keyLight = keyLight;
            Refresh();
        }

        // The prefab disc is another active descendant scanned by the legacy selection outline.
        void CopyMesh()
        {
            _filter = GetComponent<MeshFilter>();
            if (!_filter)
            {
                return;
            }

            if (_filter.sharedMesh != _meshCopy)
            {
                _sourceMesh = _filter.sharedMesh;
            }

            _meshCopies.Dispose();
            _meshCopy = _meshCopies.Get(_sourceMesh);
            _filter.sharedMesh = _meshCopy;
        }

        void OnDestroy()
        {
            if (_filter && _filter.sharedMesh == _meshCopy)
            {
                _filter.sharedMesh = _sourceMesh;
            }

            _meshCopies.Dispose();
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
                    Vector3 point = space.InverseTransformPoint(RenderMath.Corner(world, corner));
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
    }
}
