using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HealerLike.Render.Environment;

namespace HealerLike.Render.Stage
{
    // Moves the camera in on the fight when a wave starts and back to the overview when the round ends
    public class BattleFocus : MonoBehaviour
    {
        [SerializeField] Button _toggle;
        [SerializeField] Text _label;
        [SerializeField] float _pitch = StageCalibration.PortraitPitch;

        // Camera easing: position smoothing time, rotation rate, and the distance at which the overview has settled
        static readonly float smoothTime = 0.28f;
        static readonly float slerpRate = 10f;
        static readonly float settleDistance = 0.03f;
        static readonly float fogInterval = 0.1f;
        static readonly float boundsInterval = 0.4f;

        readonly Dictionary<Transform, Renderer[]> _bodies = new Dictionary<Transform, Renderer[]>();
        readonly List<Transform> _live = new List<Transform>();
        RenderManager _manager;
        Button _nextWaveButton;
        Camera _camera;
        Pose _target;
        Vector3 _velocity;
        float _refreshAt;
        float _fogAt;
        bool _isSettled;
        bool _isInitialized = false;

        bool _isFocused;
        Bounds _combatBounds;

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

            // A wave starts on the HUD's next wave button, the round ends on the game type's signal
            Clear();
            GameView gameView = FindAnyObjectByType<GameView>(FindObjectsInactive.Include);
            if (gameView != null && gameView.gameHUD != null)
            {
                _nextWaveButton = gameView.gameHUD.nextWaveButton;
            }

            if (_nextWaveButton != null)
            {
                _nextWaveButton.onClick.AddListener(Focus);
            }

            AscensionGameType.OnRoundEnd.AddListener(Overview);
            SetLabel();
        }

        public void Clear()
        {
            AscensionGameType.OnRoundEnd.RemoveListener(Overview);
            if (_nextWaveButton != null)
            {
                _nextWaveButton.onClick.RemoveListener(Focus);
            }

            _nextWaveButton = null;
        }

        void OnDestroy()
        {
            Clear();
        }

        // The RenderManager ticks it after the zones and the grass; a disabled focus leaves the camera alone
        public void Tick()
        {
            if (!_isInitialized || !isActiveAndEnabled || _camera == null)
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
            cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, _target.position, ref _velocity,
                                                          smoothTime, Mathf.Infinity, deltaTime);
            float turn = 1f - Mathf.Exp(-slerpRate * deltaTime);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, _target.rotation, turn);

            // Widen at once when a spawn leaves the safe viewport, only the zoom in eases
            if (_isFocused && !AreAllBodiesVisible())
            {
                Pose safe = BattleFocusBounds.Fit(_combatBounds, cameraTransform.eulerAngles.x, _camera.fieldOfView,
                                                  _camera.aspect);
                cameraTransform.position = safe.position;
                _velocity = Vector3.zero;
            }

            if (Time.unscaledTime >= _fogAt)
            {
                _fogAt = Time.unscaledTime + fogInterval;
                Vector2 fogRange = StageCalibration.BackgroundFog(cameraTransform.position, _manager.board);
                _manager.look.UpdateFog(fogRange);
            }

            float remaining = Vector3.Distance(cameraTransform.position, _target.position);
            if (!_isFocused && !_isSettled && remaining < settleDistance)
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

            Pose view = new Pose(_camera.transform.position, _camera.transform.rotation);
            return StageCalibration.Contains(_combatBounds, view, _camera.fieldOfView, _camera.aspect,
                                             BattleFocusBounds.VisibleFrame);
        }

        void RefreshBounds()
        {
            _refreshAt = Time.unscaledTime + boundsInterval;
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

            bounds.Expand(BattleFocusBounds.Padding);
            _combatBounds = bounds;
            _target = BattleFocusBounds.Fit(bounds, _pitch, _camera.fieldOfView, _camera.aspect);
        }

        Bounds BodyBounds(Transform body)
        {
            if (!_bodies.TryGetValue(body, out Renderer[] renderers))
            {
                renderers = body.GetComponentsInChildren<Renderer>();
                _bodies[body] = renderers;
            }

            return BattleFocusBounds.Body(body, renderers);
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
            EnvironmentForeground foreground = _manager.foreground;
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
}
