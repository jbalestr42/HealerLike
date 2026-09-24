using System;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells.Studio;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Spells.Editor.Studio
{
    /// <summary>A privately owned preview scene. It never runs gameplay or changes the open scene.</summary>
    public sealed class SpellStudioPreview : IDisposable
    {
        PreviewRenderUtility _preview;
        GameObject _root, _ground, _reference;
        SpellEffect _effect;
        CreatureRig _rig;
        PrimitiveMeshes _meshes;
        Material _material;
        SpellStudioPreset _preset;
        bool _dirty = true, _disposed, _fitRequested = true;
        Bounds _frameBounds;
        float _frameAspect = -1f;
        float _time = -1f;
        Vector2 _orbit = new Vector2(28f, 32f);
        Vector3 _target = new Vector3(0f, 0.6f, 0f);
        float _distance = 3.7f;
        int _dragControl;
        string _error;

        public bool ShowGround { get; set; } = true;
        bool _showReference = true;
        public bool ShowReference
        {
            get => _showReference;
            set { if (_showReference != value) _fitRequested = true; _showReference = value; }
        }

        public void Refresh() { _dirty = true; _fitRequested = true; }

        public void ResetCamera()
        {
            _orbit = new Vector2(28f, 32f);
            _target = new Vector3(0f, 0.6f, 0f);
            _distance = 3.7f;
            _fitRequested = true;
        }

        public void Draw(Rect rect, SpellStudioPreset preset, float time)
        {
            if (_disposed || rect.width < 2f || rect.height < 2f) return;
            HandleInput(rect);
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, new Color(0.075f, 0.095f, 0.115f));
            if (!preset)
            {
                GUI.Label(rect, "Choose a spell to preview", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            Sample(preset, time);
            if (!string.IsNullOrEmpty(_error))
            {
                GUI.Label(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, 70f), _error, EditorStyles.wordWrappedLabel);
                return;
            }

            ConfigureCamera(rect.width, rect.height);
            _preview.BeginPreview(rect, GUIStyle.none);
            try
            {
                // The project material has a UniversalForwardOnly pass: rendering through SRP is essential.
                using (new SpellStudioPreviewLookScope(_preview.camera))
                    _preview.Render(true, false);
            }
            finally
            {
                Texture texture = _preview.EndPreview();
                if (texture) GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
            }
            GUI.Label(new Rect(rect.x + 12f, rect.yMax - 24f, rect.width - 24f, 18f),
                "Drag to orbit  ·  Scroll to zoom  ·  Shift-drag to pan", EditorStyles.whiteMiniLabel);
        }

        /// <summary>Samples the runtime effect without requiring an IMGUI event. The returned effect is owned by this preview.</summary>
        public SpellEffect Sample(SpellStudioPreset preset, float time)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SpellStudioPreview));
            EnsurePreview();
            if (_preset != preset) { _preset = preset; _dirty = true; _fitRequested = true; }
            float safeTime = float.IsFinite(time) ? Mathf.Max(0f, time) : 0f;
            if (_dirty || safeTime < _time) RebuildEffect();
            if (_effect && safeTime != _time)
            {
                _effect.Advance(Mathf.Max(0f, safeTime - _time));
                _time = safeTime;
            }
            if (_ground) _ground.SetActive(ShowGround);
            if (_reference) _reference.SetActive(ShowReference);
            return _effect;
        }

        /// <summary>Captures the current camera and visibility settings. The caller owns the returned texture.</summary>
        public Texture2D Capture(SpellStudioPreset preset, float time, int width, int height)
        {
            Sample(preset, time);
            if (!string.IsNullOrEmpty(_error)) throw new InvalidOperationException(_error);
            if (!preset) throw new ArgumentNullException(nameof(preset));
            ConfigureCamera(width, height);
            _preview.BeginStaticPreview(new Rect(0f, 0f, Mathf.Clamp(width, 16, 4096), Mathf.Clamp(height, 16, 4096)));
            Texture2D result = null;
            try
            {
                using (new SpellStudioPreviewLookScope(_preview.camera))
                    _preview.Render(true, false);
            }
            finally
            {
                result = _preview.EndStaticPreview();
            }
            return result;
        }

        void ConfigureCamera(float width, float height)
        {
            Camera camera = _preview.camera;
            camera.aspect = Mathf.Max(0.1f, width / Mathf.Max(1f, height));
            camera.fieldOfView = 34f;
            if (!Mathf.Approximately(_frameAspect, camera.aspect)) _fitRequested = true;
            _frameAspect = camera.aspect;
            if (_fitRequested)
            {
                _frameBounds = SubjectBounds();
                _target = _frameBounds.center;
                float halfAngle = Mathf.Atan(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * Mathf.Min(1f, camera.aspect));
                _distance = Mathf.Max(1f, _frameBounds.extents.magnitude * 1.12f / Mathf.Sin(halfAngle));
                _fitRequested = false;
            }
            Quaternion rotation = Quaternion.Euler(_orbit.x, _orbit.y, 0f);
            camera.transform.SetPositionAndRotation(_target - rotation * Vector3.forward * _distance, rotation);
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 34f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.095f, 0.115f);
            camera.allowHDR = true;
        }

        Bounds SubjectBounds()
        {
            Bounds bounds = new Bounds(Vector3.up, Vector3.one * 0.1f);
            bool found = false;
            void Include(GameObject subject)
            {
                if (!subject) return;
                foreach (Renderer renderer in subject.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || renderer.bounds.size.sqrMagnitude < 0.00001f) continue;
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
            }
            if (ShowReference) Include(_reference);
            if (_effect)
            {
                // Frame the complete motion so playback never clips a rising or falling spell.
                for (int i = 0; i <= 8; i++)
                {
                    float phase = i / 8f;
                    if (_effect.recipe.socket != EffectSocket.Link)
                        _effect.Pose(phase, phase * _effect.recipe.cycleSeconds);
                    Include(_effect.gameObject);
                }
                _effect.Advance(0f);
            }
            return bounds;
        }

        void EnsurePreview()
        {
            if (_preview != null) return;
            _preview = new PreviewRenderUtility();
            foreach (Light light in _preview.lights)
            {
                light.enabled = true;
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
            }
            _preview.lights[0].intensity = 1.25f;
            _preview.lights[0].transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            _preview.lights[1].intensity = 0.55f;
            _preview.lights[1].transform.rotation = Quaternion.Euler(320f, 145f, 0f);
            _preview.ambientColor = new Color(0.35f, 0.39f, 0.45f);
            _meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
            if (!_meshes || !source)
            {
                _error = "Preview requires PrimitiveMeshes.asset and Look_Default.mat from Assets/Render.";
                return;
            }
            _material = new Material(source) { name = "Spell Studio Preview Material", hideFlags = HideFlags.HideAndDontSave };
            // Keep the runtime shader, with a neutral studio surface calibration.
            _material.SetFloat("_HLToonThresholdOffset", 0f);
            _material.SetColor("_HLShadeTint", Color.clear);
            _material.SetFloat("_HLHatchMultiplier", 0.2f);
            _root = new GameObject("Spell Studio Preview") { hideFlags = HideFlags.HideAndDontSave };
            _preview.AddSingleGO(_root);
            _ground = new GameObject("Preview Ground");
            _ground.transform.SetParent(_root.transform, false);
            // Geometry grid avoids publishing shader globals into the user's open scene.
            if (_meshes.disc)
            {
                Transform disc = PrimitiveMeshes.Geometry("Ground", _ground.transform, _meshes.disc, _material,
                    new Color(0.15f, 0.19f, 0.22f));
                disc.localScale = Vector3.one * 8f;
                disc.localPosition = Vector3.down * 0.012f;
            }
            for (int i = -8; i <= 8; i++)
            {
                Color colour = i == 0 ? new Color(0.32f, 0.40f, 0.45f) : new Color(0.22f, 0.28f, 0.31f);
                Transform x = PrimitiveMeshes.Geometry("Grid X", _ground.transform, _meshes.cylinder, _material, colour);
                PrimitiveMeshes.Segment(x, new Vector3(-4f, 0f, i * 0.5f), new Vector3(4f, 0f, i * 0.5f), 0.002f);
                Transform z = PrimitiveMeshes.Geometry("Grid Z", _ground.transform, _meshes.cylinder, _material, colour);
                PrimitiveMeshes.Segment(z, new Vector3(i * 0.5f, 0f, -4f), new Vector3(i * 0.5f, 0f, 4f), 0.002f);
            }
            _reference = new GameObject("Reference Creature");
            _reference.transform.SetParent(_root.transform, false);
            CreatureRecipe creature = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/Healer.asset");
            if (creature)
            {
                _rig = new CreatureRig();
                if (_rig.Init(creature, _reference.transform, _material, _meshes))
                    _rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
            }
            HideTree(_root);
        }

        void RebuildEffect()
        {
            _dirty = false;
            _time = 0f;
            if (_effect) Object.DestroyImmediate(_effect.gameObject);
            _effect = null;
            if (!_root || !_preset) return;
            EffectRecipe recipe = _preset.Compose();
            if (recipe == null)
            {
                _error = "Assign an effect vocabulary or enable a custom entry to build this spell.";
                return;
            }
            _error = null;
            GameObject effectObject = new GameObject("Preview Spell");
            effectObject.transform.SetParent(_root.transform, false);
            _effect = effectObject.AddComponent<SpellEffect>();
            _effect.enabled = false; // Only the editor timeline may advance this effect, including during Play mode.
            _effect.Init(recipe, _meshes, _material);
            EffectAnchors anchors = new EffectAnchors
            {
                foot = Vector3.zero, bodyCentre = Vector3.up * 0.3f, bodyRadius = 0.3f,
                neck = Vector3.up * 0.6f, headCentre = Vector3.up * 0.75f, headRadius = 0.15f
            };
            if (_rig != null && _rig.TryGetAnchors(out EffectAnchors actual)) anchors = actual;
            if (recipe.socket == EffectSocket.Link)
            {
                _effect.SetEndpoints(new Vector3(-0.9f, 0.6f, 0f), new Vector3(0.9f, 0.6f, 0f), false);
            }
            else if (recipe.socket == EffectSocket.Ground)
            {
                _effect.transform.localPosition = Vector3.up * 0.02f;
            }
            else
            {
                EffectPlacement.Place(_effect, _root.transform, anchors);
            }
            _effect.transform.localScale *= _preset.SafeScale;
            if (recipe.tempo != EffectTempo.Once)
                _effect.SetStatus(_preset.SafeStacks, 0f, _preset.PreviewDuration, ClockKind.Simulation);
            _effect.SetSide(_preset.SafeSide);
            if (_preset.critical) _effect.ShowCritical();
            HideTree(effectObject);
        }

        void HandleInput(Rect rect)
        {
            Event e = Event.current;
            int control = GUIUtility.GetControlID("SpellStudioCamera".GetHashCode(), FocusType.Passive, rect);
            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                _distance = Mathf.Clamp(_distance * Mathf.Exp(e.delta.y * 0.04f), 0.5f, 25f);
                GUI.changed = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && e.button <= 2)
            {
                _dragControl = control;
                GUIUtility.hotControl = control;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == _dragControl && _dragControl != 0)
            {
                if (e.shift || e.button == 2)
                {
                    Quaternion rotation = Quaternion.Euler(_orbit.x, _orbit.y, 0f);
                    _target += rotation * new Vector3(-e.delta.x, e.delta.y, 0f) * (_distance / Mathf.Max(100f, rect.height));
                }
                else
                {
                    _orbit.y += e.delta.x * 0.5f;
                    _orbit.x = Mathf.Clamp(_orbit.x + e.delta.y * 0.5f, -80f, 85f);
                }
                GUI.changed = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && GUIUtility.hotControl == _dragControl && _dragControl != 0)
            {
                GUIUtility.hotControl = 0;
                _dragControl = 0;
                e.Use();
            }
        }

        static void HideTree(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_dragControl != 0 && GUIUtility.hotControl == _dragControl) GUIUtility.hotControl = 0;
            _rig?.Dispose();
            _rig = null;
            _preview?.Cleanup();
            _preview = null;
            if (_material) Object.DestroyImmediate(_material);
            _root = null;
            _effect = null;
        }
    }
}
