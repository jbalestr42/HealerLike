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

        readonly BattleBodyBounds _bodies = new BattleBodyBounds();
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
        bool _hasViewport;
        Rect _viewport = BattleFocusBounds.FocusFrame;
        Rect _visibleViewport = BattleFocusBounds.VisibleFrame;

        public bool isFocused { get { return _isFocused; } }

        public void ShowLegacyControl(bool show)
        {
            if (_toggle != null)
            {
                _toggle.gameObject.SetActive(show);
            }
        }

        public void SetViewport(Rect viewport)
        {
            _hasViewport = true;
            _visibleViewport = viewport;
            _viewport = StageViewport.Inset(viewport, 0.025f);
            MarkDirty();
        }

        public void Init(RenderManager manager)
        {
            Clear();
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
            _bodies.Clear();
            _isInitialized = false;
            if (_toggle != null)
            {
                _toggle.onClick.RemoveListener(Toggle);
            }
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
                Pose safe = Fit(_combatBounds, cameraTransform.rotation);
                cameraTransform.position = safe.position;
                _velocity = Vector3.zero;
            }

            if (Time.unscaledTime >= _fogAt)
            {
                _fogAt = Time.unscaledTime + fogInterval;
                Vector2 fogRange = StageCalibration.BackgroundFog(cameraTransform.position, _manager.board,
                    cameraTransform.eulerAngles.y);
                _manager.look.UpdateFog(fogRange);
            }

            float remaining = Vector3.Distance(cameraTransform.position, _target.position);
            if (remaining < settleDistance)
            {
                _manager.FrameEnvironment();
            }
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
            if (_camera == null || _bodies.count == 0)
            {
                return false;
            }

            Pose view = new Pose(_camera.transform.position, _camera.transform.rotation);
            return StageCalibration.Contains(_combatBounds, view, _camera.fieldOfView, _camera.aspect,
                                             _visibleViewport);
        }

        void RefreshBounds()
        {
            _refreshAt = Time.unscaledTime + boundsInterval;
            if (!_bodies.TryRead(_manager.entityManager, out Bounds bounds))
            {
                Overview();
                return;
            }

            bounds.Expand(BattleFocusBounds.Padding);
            _combatBounds = bounds;
            Quaternion rotation = Quaternion.Euler(_pitch, _manager.overviewPose.rotation.eulerAngles.y, 0f);
            _target = Fit(bounds, rotation);
        }

        Pose Fit(Bounds bounds, Quaternion rotation)
        {
            return _hasViewport
                ? StageViewport.Fit(bounds, rotation, _camera.fieldOfView, _camera.aspect, _viewport)
                : BattleFocusBounds.Fit(bounds, rotation, _camera.fieldOfView, _camera.aspect);
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
