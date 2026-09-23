using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HealerLike.Render.Environment;

namespace HealerLike.Render.Stage
{
    // Moves the camera in on the fight when a wave starts and back to the overview when the round ends
    public class HLBattleFocus : MonoBehaviour
    {
        [SerializeField] Button _toggle;
        [SerializeField] Text _label;
        [SerializeField] float _pitch = 52f;

        readonly Dictionary<Transform, Renderer[]> _bodies = new Dictionary<Transform, Renderer[]>();
        readonly List<Transform> _live = new List<Transform>();
        RenderManager _manager;
        Camera _camera;
        Pose _target;
        Vector3 _velocity;
        float _refreshAt;
        float _fogAt;
        bool _isSettled;
        bool _isInitialized = false;

        bool _isFocused;
        public bool isFocused { get { return _isFocused; } }

        Bounds _combatBounds;
        public Bounds combatBounds { get { return _combatBounds; } }

        public int bodyCount { get { return _live.Count; } }

        public Button toggle { get { return _toggle; } }

        public void Init(RenderManager manager)
        {
            _manager = manager;
            _camera = manager.gameCamera;
            _target = manager.overviewPose;
            _isFocused = false;
            _isSettled = true;
            _isInitialized = _camera != null;
            if (_toggle != null)
            {
                _toggle.onClick.RemoveListener(Toggle);
                _toggle.onClick.AddListener(Toggle);
            }

            SetLabel();
        }

        void LateUpdate()
        {
            if (!_isInitialized || _camera == null)
            {
                return;
            }

            if (_isFocused && Time.unscaledTime >= _refreshAt)
            {
                RefreshBounds();
            }

            if (!_isFocused)
            {
                _target = _manager.overviewPose;
            }

            Transform cameraTransform = _camera.transform;
            float deltaTime = Time.unscaledDeltaTime;
            cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, _target.position, ref _velocity, 0.28f,
                                                          Mathf.Infinity, deltaTime);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, _target.rotation, 1f - Mathf.Exp(-10f * deltaTime));

            // Widen at once when a spawn leaves the safe viewport, only the zoom in eases
            if (_isFocused && !AreAllBodiesVisible())
            {
                Pose safe = HLBattleFocusBounds.Fit(_combatBounds, cameraTransform.eulerAngles.x, _camera.fieldOfView, _camera.aspect);
                cameraTransform.position = safe.position;
                _velocity = Vector3.zero;
            }

            if (Time.unscaledTime >= _fogAt)
            {
                _fogAt = Time.unscaledTime + 0.1f;
                _manager.look.UpdateFog(cameraTransform.position);
            }

            if (!_isFocused && !_isSettled && Vector3.Distance(cameraTransform.position, _target.position) < 0.03f)
            {
                _isSettled = true;
                ShowForeground(true);
            }
        }

        public void MarkDirty()
        {
            _refreshAt = 0f;
        }

        public void Toggle()
        {
            if (_isFocused)
            {
                Overview();
            }
            else
            {
                Focus();
            }
        }

        public void Focus()
        {
            if (!_isInitialized)
            {
                return;
            }

            _isFocused = true;
            _isSettled = false;
            _refreshAt = 0f;
            ShowForeground(false);
            RefreshBounds();
            SetLabel();
        }

        public void Overview()
        {
            if (!_isInitialized)
            {
                return;
            }

            _isFocused = false;
            _isSettled = false;
            _target = _manager.overviewPose;
            SetLabel();
        }

        public bool AreAllBodiesVisible()
        {
            if (_camera == null || _live.Count == 0)
            {
                return false;
            }

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 viewport = _camera.WorldToViewportPoint(_combatBounds.center + Vector3.Scale(_combatBounds.extents, sign));
                if (viewport.z <= _camera.nearClipPlane || viewport.x < 0.06f || viewport.x > 0.94f
                    || viewport.y < 0.18f || viewport.y > 0.84f)
                {
                    return false;
                }
            }

            return true;
        }

        void RefreshBounds()
        {
            _refreshAt = Time.unscaledTime + 0.4f;
            _live.Clear();
            EntityManager entityManager = _manager.entityManager;
            if (entityManager != null && entityManager.entities != null)
            {
                AddLive(entityManager.GetEntities(Entity.EntityType.Player));
                AddLive(entityManager.GetEntities(Entity.EntityType.Computer));
            }

            if (_manager.player != null && _manager.player.character != null)
            {
                _live.Add(_manager.player.character.transform);
            }

            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Transform body in _live)
            {
                Bounds bodyBounds = BodyBounds(body);
                if (hasBounds)
                {
                    bounds.Encapsulate(bodyBounds);
                }
                else
                {
                    bounds = bodyBounds;
                    hasBounds = true;
                }
            }

            List<Transform> gone = new List<Transform>();
            foreach (Transform body in _bodies.Keys)
            {
                if (body == null || !_live.Contains(body))
                {
                    gone.Add(body);
                }
            }

            foreach (Transform body in gone)
            {
                _bodies.Remove(body);
            }

            if (!hasBounds)
            {
                Overview();
                return;
            }

            bounds.Expand(new Vector3(0.8f, 0.5f, 0.8f));
            _combatBounds = bounds;
            _target = HLBattleFocusBounds.Fit(bounds, _pitch, _camera.fieldOfView, _camera.aspect);
        }

        // Authored mesh bounds include heads and roots, transient effects and lines are left out
        Bounds BodyBounds(Transform body)
        {
            if (!_bodies.TryGetValue(body, out Renderer[] renderers))
            {
                renderers = body.GetComponentsInChildren<Renderer>();
                _bodies[body] = renderers;
            }

            bool hasMesh = false;
            Bounds bounds = new Bounds(body.position + Vector3.up, new Vector3(1.2f, 2f, 1.2f));
            foreach (Renderer bodyRenderer in renderers)
            {
                bool isBody = bodyRenderer != null && bodyRenderer.enabled && bodyRenderer.gameObject.activeInHierarchy
                              && bodyRenderer is MeshRenderer && bodyRenderer.bounds.size.magnitude < 8f;
                if (!isBody)
                {
                    continue;
                }

                if (hasMesh)
                {
                    bounds.Encapsulate(bodyRenderer.bounds);
                }
                else
                {
                    bounds = bodyRenderer.bounds;
                    hasMesh = true;
                }
            }

            return bounds;
        }

        void AddLive(List<GameObject> entities)
        {
            foreach (GameObject entityGo in entities)
            {
                if (entityGo != null && entityGo.activeInHierarchy)
                {
                    _live.Add(entityGo.transform);
                }
            }
        }

        void ShowForeground(bool show)
        {
            HLEnvironmentForeground foreground = _manager.foreground;
            if (foreground == null)
            {
                return;
            }

            foreground.enabled = show;
            if (show)
            {
                foreground.Build();
            }
        }

        void SetLabel()
        {
            if (_label != null)
            {
                _label.text = _isFocused ? "Overview" : "Focus battle";
            }
        }
    }

    public static class HLBattleFocusBounds
    {
        // All eight corners fit x 0.08 to 0.92 and y 0.20 to 0.82, with room for tall heads and the HUD
        public static Pose Fit(Bounds bounds, float pitch, float fov, float aspect)
        {
            Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
            Quaternion inverse = Quaternion.Inverse(rotation);
            float tan = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
            float distance = 2f;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 local = inverse * Vector3.Scale(bounds.extents, sign);
                distance = Mathf.Max(distance, Mathf.Abs(local.x) / (0.84f * tan * aspect) - local.z);
                distance = Mathf.Max(distance, (local.y - 0.64f * tan * local.z) / (0.58f * tan));
                distance = Mathf.Max(distance, (-local.y - 0.6f * tan * local.z) / (0.66f * tan));
            }

            Vector3 position = bounds.center - rotation * Vector3.forward * distance - rotation * Vector3.up * (0.06f * tan * distance);
            return new Pose(position, rotation);
        }
    }
}
