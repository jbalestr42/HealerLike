using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // One runtime SpellEffect in a private scene, on its target creature, sampled by the studio timeline.
    // It never runs gameplay: the effect is disabled and only Sample moves it.
    public class SpellStudioPreview : IPreviewSubject
    {
        static readonly Color background = new Color(0.075f, 0.095f, 0.115f);
        static readonly float hatchMultiplier = 0.2f;
        // Poses sampled over one motion cycle when the view is framed
        static readonly int framingPoses = 8;

        readonly StudioPreviewScene _scene = new StudioPreviewScene();
        readonly StudioPreviewCamera _camera = new StudioPreviewCamera();
        readonly SpellPreviewTarget _target = new SpellPreviewTarget();
        SpellEffect _effect;
        SpellStudioPreset _preset;
        float _time = -1f;
        bool _isDirty = true;
        bool _isDisposed;
        bool _isGroundShown = true;
        bool _isReferenceShown = true;
        string _error;

        public SpellPreviewTarget target { get { return _target; } }

        public bool isGroundShown { get { return _isGroundShown; } set { _isGroundShown = value; } }

        public bool isReferenceShown
        {
            get { return _isReferenceShown; }
            set
            {
                if (_isReferenceShown != value)
                {
                    _camera.Refit();
                }
                _isReferenceShown = value;
            }
        }

        // Why the preview draws a message instead of the spell, null while it draws the spell
        public string error
        {
            get
            {
                if (!string.IsNullOrEmpty(_target.error))
                {
                    return _target.error;
                }

                if (!string.IsNullOrEmpty(_scene.error))
                {
                    return _scene.error;
                }
                return _error;
            }
        }

        public void Init()
        {
            _camera.Init(new Vector2(28f, 32f), 0.5f, 25f, 1.12f, "SpellStudioCamera");
        }

        // Rebuilds the target and the effect on the next sample and refits the view
        public void Refresh()
        {
            _isDirty = true;
            _camera.Refit();
            _target.MarkDirty();
        }

        public void ResetCamera()
        {
            _camera.Reset();
        }

        public void Draw(Rect rect, SpellStudioPreset preset, float time)
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
            if (!preset)
            {
                GUI.Label(rect, "Choose a spell to preview", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Sample(preset, time);
            if (!string.IsNullOrEmpty(error))
            {
                StudioPreviewFrame.DrawMessage(rect, error);
                return;
            }

            _camera.Apply(_scene.utility.camera, rect.width, rect.height, true, this);
            StudioPreviewFrame.Draw(_scene.utility, rect);
        }

        // The effect at this time, without an IMGUI event; the preview owns it. Seeking back rebuilds the effect
        // rather than running it backwards.
        public SpellEffect Sample(SpellStudioPreset preset, float time)
        {
            if (_isDisposed)
            {
                Debug.LogError("[SpellStudioPreview] Sampled after Dispose");
                return null;
            }

            if (!_scene.isStarted)
            {
                BuildScene();
            }

            if (_target.isDirty && _target.root)
            {
                _target.Rebuild(_scene);
                _isDirty = true;
                _camera.Refit();
            }

            if (_preset != preset)
            {
                _preset = preset;
                _isDirty = true;
                _camera.Refit();
            }

            float safeTime = 0f;
            if (float.IsFinite(time))
            {
                safeTime = Mathf.Max(0f, time);
            }

            if (_isDirty || safeTime < _time)
            {
                RebuildEffect();
            }

            if (_effect && safeTime != _time)
            {
                _effect.Advance(Mathf.Max(0f, safeTime - _time));
                _time = safeTime;
            }

            _scene.ShowGround(_isGroundShown);
            if (_target.root)
            {
                _target.root.SetActive(_isReferenceShown);
            }
            return _effect;
        }

        // A texture the caller owns, from the user's framing at the capture's size. Null, with the error logged,
        // when the preview would draw a message instead.
        public Texture2D Capture(SpellStudioPreset preset, float time, int width, int height)
        {
            if (!preset)
            {
                Debug.LogError("[SpellStudioPreview] Nothing to capture");
                return null;
            }

            Sample(preset, time);
            if (_isDisposed)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("[SpellStudioPreview] " + error);
                return null;
            }

            Camera camera = _scene.utility.camera;
            int safeWidth = StudioPreviewFrame.ClampSize(width);
            int safeHeight = StudioPreviewFrame.ClampSize(height);
            _camera.BeginCapture(camera);
            _camera.Apply(camera, safeWidth, safeHeight, false, this);
            Texture2D image = StudioPreviewFrame.Capture(_scene.utility, safeWidth, safeHeight);
            _camera.EndCapture(camera);
            return image;
        }

        // The target and the whole motion of the effect, so playback never clips a rising or falling spell
        public Bounds GetSubjectBounds()
        {
            Bounds bounds = new Bounds(Vector3.up, Vector3.one * 0.1f);
            bool isFound = false;
            if (_isReferenceShown)
            {
                isFound = StudioPreviewFrame.Encapsulate(_target.root, ref bounds, isFound);
            }

            if (!_effect)
            {
                return bounds;
            }

            for (int i = 0; i <= framingPoses; i++)
            {
                float phase = i / (float)framingPoses;
                if (_effect.recipe.socket != EffectSocket.Link)
                {
                    _effect.Pose(phase, phase * _effect.recipe.cycleSeconds);
                }
                isFound = StudioPreviewFrame.Encapsulate(_effect.gameObject, ref bounds, isFound);
            }

            _effect.Advance(0f);
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
            _target.Dispose();
            _scene.Dispose();
            _effect = null;
        }

        void BuildScene()
        {
            _scene.Init("Spell Studio");
            if (!string.IsNullOrEmpty(_scene.error))
            {
                return;
            }

            _scene.bodyMaterial.SetFloat("_HLHatchMultiplier", hatchMultiplier);
            _scene.stoneMaterial.SetFloat("_HLHatchMultiplier", hatchMultiplier);
            // Keep the runtime shader, and the plant body's own treatment, under the studio lighting
            _scene.material.SetFloat("_HLToonThresholdOffset", 0f);
            _scene.material.SetColor("_HLShadeTint", Color.clear);
            _scene.material.SetFloat("_HLHatchMultiplier", hatchMultiplier);
            _target.Init(_scene);
            StudioPreviewScene.HideTree(_scene.root);
        }

        void RebuildEffect()
        {
            _isDirty = false;
            _time = 0f;
            if (_effect)
            {
                Object.DestroyImmediate(_effect.gameObject);
            }

            _effect = null;
            if (!_scene.root || !_preset)
            {
                return;
            }

            EffectRecipe recipe = _preset.Compose();
            if (recipe == null)
            {
                string[] warnings = SpellPresetValidator.Validate(_preset);
                _error = "Assign an effect vocabulary or enable a custom entry to build this spell.";
                if (warnings.Length > 0)
                {
                    _error = warnings[0];
                }
                return;
            }

            _error = null;
            _effect = SpellPreviewEffect.Build(_scene, _preset, recipe, _target);
        }
    }
}
