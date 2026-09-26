using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The real creature rig in a private scene, built from a private copy of the recipe and sampled by the
    // studio timeline at a fixed 60 steps a second, so any seek lands on the same pose
    public class CreatureStudioPreview : IPreviewSubject, System.IDisposable
    {
        static readonly Color background = new Color(0.075f, 0.095f, 0.115f);

        readonly StudioPreviewScene _scene = new StudioPreviewScene();
        readonly StudioPreviewCamera _camera = new StudioPreviewCamera();
        readonly CreaturePreviewSelection _selection = new CreaturePreviewSelection();
        readonly CreaturePreviewRig _rig = new CreaturePreviewRig();
        GameObject _subject;
        LookSide _side;
        CreatureRecipe _source;
        float _time = -1f;
        bool _isDirty = true;
        bool _isDisposed;
        bool _isGroundShown = true;
        int _selectedPart = -1;
        float _health = 1f;
        float _charge;
        float _glow;
        Vector3? _aim;
        string _lastError;
        Bounds? _framingBounds;

        // A roster supplies one union of its subjects, so each card keeps the same world scale.
        public Bounds? framingBounds
        {
            get { return _framingBounds; }
            set { _framingBounds = value; _camera.Refit(); }
        }

        public LookSide side
        {
            get { return _side; }
            set
            {
                if (_side != value)
                {
                    _side = value;
                    _isDirty = true;
                }
            }
        }

        public bool isGroundShown { get { return _isGroundShown; } set { _isGroundShown = value; } }

        // The part the selection box wraps, -1 for none
        public int selectedPart { get { return _selectedPart; } set { _selectedPart = value; } }

        public string lastError { get { return _lastError; } }

        public float health { get { return _health; } set { SetReadout(ref _health, value, 1f); } }

        public float charge { get { return _charge; } set { SetReadout(ref _charge, value, 0f); } }

        public float glow { get { return _glow; } set { SetReadout(ref _glow, value, 0f); } }

        // Where the rig aims, null for straight ahead; a point that is not finite counts as none
        public Vector3? aim
        {
            get { return _aim; }
            set
            {
                Vector3? safe = value;
                if (value.HasValue && !RenderMath.IsFinite(value.Value))
                {
                    safe = null;
                }

                if (_aim != safe)
                {
                    _isDirty = true;
                }
                _aim = safe;
            }
        }

        public void Init()
        {
            _camera.Init(new Vector2(18f, 30f), 0.3f, 100f, 1.18f, "CreatureStudioCamera");
        }

        public void Refresh()
        {
            _isDirty = true;
            _camera.Refit();
        }

        public void ResetCamera()
        {
            _camera.Reset();
        }

        // The rig at this time; the preview owns it and the source recipe is never touched
        public CreatureRig Sample(CreatureRecipe recipe, float time)
        {
            if (_isDisposed)
            {
                Debug.LogError("[CreatureStudioPreview] Sampled after Dispose");
                return null;
            }

            if (!_scene.isStarted)
            {
                BuildScene();
            }

            if (_source != recipe)
            {
                _source = recipe;
                _isDirty = true;
                _camera.Refit();
            }

            float safeTime = 0f;
            if (float.IsFinite(time))
            {
                safeTime = Mathf.Clamp(time, 0f, 120f);
            }

            if (_isDirty || safeTime < _time)
            {
                Rebuild();
            }

            if (_rig.rig != null)
            {
                _rig.Advance(safeTime, _aim, _health, _charge, _glow);
                _time = safeTime;
            }

            _scene.ShowGround(_isGroundShown);
            _selection.Show(_rig.rig, _selectedPart);
            return _rig.rig;
        }

        public void Draw(Rect rect, CreatureRecipe recipe, float time)
        {
            if (_isDisposed || rect.width < 2f || rect.height < 2f)
            {
                return;
            }

            _camera.HandleInput(rect);
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(rect, background);
            Sample(recipe, time);
            if (!string.IsNullOrEmpty(_lastError) || _rig.rig == null)
            {
                string message = "Choose a creature to preview";
                if (!string.IsNullOrEmpty(_lastError))
                {
                    message = _lastError;
                }
                StudioPreviewFrame.DrawMessage(rect, message);
                return;
            }

            _camera.Apply(_scene.utility.camera, rect.width, rect.height, true, this);
            StudioPreviewFrame.Draw(_scene.utility, rect);
        }

        // A texture the caller owns, from the user's framing at the capture's size. Null, with the error logged,
        // when there is no rig to capture.
        public Texture2D Capture(CreatureRecipe recipe, float time, int width, int height)
        {
            if (Sample(recipe, time) == null)
            {
                string message = "Choose a creature to capture.";
                if (!string.IsNullOrEmpty(_lastError))
                {
                    message = _lastError;
                }
                Debug.LogError("[CreatureStudioPreview] " + message);
                return null;
            }

            int safeWidth = StudioPreviewFrame.ClampSize(width);
            int safeHeight = StudioPreviewFrame.ClampSize(height);
            return StudioPreviewFrame.Capture(_scene.utility, _camera, this, safeWidth, safeHeight);
        }

        // The rig's renderers, grown by how far its idle breath and sway can carry it
        public Bounds GetSubjectBounds()
        {
            if (_framingBounds.HasValue)
            {
                return _framingBounds.Value;
            }
            return GetContentBounds();
        }

        public Bounds GetContentBounds()
        {
            Bounds bounds = new Bounds(Vector3.up, Vector3.one * 0.1f);
            StudioPreviewFrame.Encapsulate(_subject, ref bounds, false);
            float margin = 0f;
            if (_source)
            {
                float sway = Mathf.Sin(Mathf.Abs(_source.idle.swayDegrees) * Mathf.Deg2Rad);
                margin = Mathf.Abs(_source.idle.breathAmount) + sway;
            }

            bounds.Expand(bounds.size.magnitude * Mathf.Clamp(margin, 0.04f, 0.5f));
            return bounds;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _camera.Release();
            _rig.Destroy();
            _scene.Dispose();
            _subject = null;
        }

        void SetReadout(ref float field, float value, float fallback)
        {
            float safe = fallback;
            if (float.IsFinite(value))
            {
                safe = Mathf.Clamp01(value);
            }

            if (field != safe)
            {
                _isDirty = true;
            }
            field = safe;
        }

        void BuildScene()
        {
            _scene.Init("Creature Studio");
            if (!string.IsNullOrEmpty(_scene.error))
            {
                _lastError = _scene.error;
                return;
            }

            _subject = _scene.AddChild("Creature Preview Subject");
            _selection.Init(_scene);
            StudioPreviewScene.HideTree(_scene.root);
        }

        void Rebuild()
        {
            _isDirty = false;
            _time = 0f;
            _rig.Destroy();
            if (!_subject)
            {
                return;
            }

            _lastError = null;
            if (!_source)
            {
                return;
            }

            _lastError = _rig.Build(_source, _subject.transform, _scene, _side);
            if (_lastError != null)
            {
                return;
            }

            _rig.Start(_aim, _health, _charge, _glow);
            StudioPreviewScene.HideTree(_subject);
        }
    }
}
