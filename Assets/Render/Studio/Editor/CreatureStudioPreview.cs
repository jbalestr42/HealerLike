using System;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    /// <summary>A private renderer scene sampling the real creature rig without gameplay.</summary>
    public sealed class CreatureStudioPreview : IDisposable
    {
        const float Step = 1f / 60f;
        static readonly FootFrame Frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        PreviewRenderUtility _preview;
        GameObject _root, _subject, _ground, _selection;
        Transform[] _selectionEdges;
        PrimitiveMeshes _meshes;
        Material _material, _bodyMaterial, _stoneMaterial;
        LookSide _side;
        CreatureRecipe _source, _working;
        CreatureRig _rig;
        float _time = -1f;
        int _steps;
        bool _dirty = true, _fitRequested = true, _disposed;
        float _health = 1f, _charge, _glow;
        Vector3? _aim;
        Vector2 _orbit = new Vector2(18f, 30f);
        Vector3 _target;
        float _distance = 5f, _aspect = -1f;
        int _dragControl;

        public LookSide Side { get => _side; set { if (_side != value) { _side = value; _dirty = true; } } }
        public bool ShowGround { get; set; } = true;
        public int SelectedPartIndex { get; set; } = -1;
        public int SelectedPart { get => SelectedPartIndex; set => SelectedPartIndex = value; }
        public string LastError { get; private set; }
        public float Health { get => _health; set => SetReadout(ref _health, value, 1f); }
        public float Charge { get => _charge; set => SetReadout(ref _charge, value, 0f); }
        public float Glow { get => _glow; set => SetReadout(ref _glow, value, 0f); }
        public Vector3? Aim
        {
            get => _aim;
            set
            {
                Vector3? safe = value;
                if (value.HasValue && (!float.IsFinite(value.Value.x) || !float.IsFinite(value.Value.y) || !float.IsFinite(value.Value.z))) safe = null;
                if (_aim != safe) _dirty = true;
                _aim = safe;
            }
        }

        void SetReadout(ref float field, float value, float fallback)
        {
            float safe = float.IsFinite(value) ? Mathf.Clamp01(value) : fallback;
            if (field != safe) _dirty = true;
            field = safe;
        }

        public void Refresh() { _dirty = true; _fitRequested = true; }
        public void ResetCamera() { _orbit = new Vector2(18f, 30f); _fitRequested = true; }

        /// <summary>The rig is preview-owned. Authoring data stays detached from the source asset.</summary>
        public CreatureRig Sample(CreatureRecipe recipe, float time)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CreatureStudioPreview));
            EnsurePreview();
            if (_source != recipe) { _source = recipe; _dirty = true; _fitRequested = true; }
            float safeTime = float.IsFinite(time) ? Mathf.Clamp(time, 0f, 120f) : 0f;
            if (_dirty || safeTime < _time) Rebuild();
            if (_rig != null)
            {
                _rig.SetReadout(_aim, _health, _charge, _glow);
                int targetSteps = Mathf.FloorToInt(safeTime * 60f + .00001f);
                while (_steps < targetSteps)
                {
                    _steps++;
                    _rig.Tick(_steps * Step, Step, Frame);
                }
                // Idle motion is absolute-time; aim integrates only complete fixed steps.
                _rig.Tick(safeTime, 0f, Frame);
                _time = safeTime;
            }
            if (_ground) _ground.SetActive(ShowGround);
            UpdateSelection();
            return _rig;
        }

        public void Draw(Rect rect, CreatureRecipe recipe, float time)
        {
            if (_disposed || rect.width < 2f || rect.height < 2f) return;
            HandleInput(rect);
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, new Color(.075f, .095f, .115f));
            Sample(recipe, time);
            if (!string.IsNullOrEmpty(LastError) || _rig == null)
            {
                GUI.Label(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, 90f), LastError ?? "Choose a creature to preview", EditorStyles.wordWrappedLabel);
                return;
            }
            ConfigureCamera(rect.width, rect.height);
            _preview.BeginPreview(rect, GUIStyle.none);
            try
            {
                using (new SpellStudioPreviewPipelineScope())
                using (new SpellStudioPreviewLookScope(_preview.camera)) _preview.Render(true, false);
            }
            finally
            {
                Texture result = _preview.EndPreview();
                if (result) GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);
            }
            GUI.Label(new Rect(rect.x + 12f, rect.yMax - 24f, rect.width - 24f, 18f), "Drag to orbit  ·  Scroll to zoom  ·  Shift-drag to pan", EditorStyles.whiteMiniLabel);
        }

        /// <summary>The caller owns the returned PNG-ready texture, including after preview disposal.</summary>
        public Texture2D Capture(CreatureRecipe recipe, float time, int width, int height)
        {
            if (Sample(recipe, time) == null) throw new InvalidOperationException(LastError ?? "Choose a creature to capture.");
            int safeWidth = Mathf.Clamp(width, 16, 4096), safeHeight = Mathf.Clamp(height, 16, 4096);
            Vector3 target = _target;
            float distance = _distance, aspect = _aspect;
            bool fitRequested = _fitRequested;
            Camera camera = _preview.camera;
            Vector3 cameraPosition = camera.transform.position;
            Quaternion cameraRotation = camera.transform.rotation;
            float cameraAspect = camera.aspect, fieldOfView = camera.fieldOfView;
            float near = camera.nearClipPlane, far = camera.farClipPlane;
            CameraClearFlags clearFlags = camera.clearFlags;
            Color background = camera.backgroundColor;
            bool allowHDR = camera.allowHDR;
            try
            {
                ConfigureCamera(safeWidth, safeHeight, false);
                _preview.BeginStaticPreview(new Rect(0f, 0f, safeWidth, safeHeight));
                Texture2D result = null;
                bool rendered = false;
                try
                {
                    using (new SpellStudioPreviewPipelineScope())
                using (new SpellStudioPreviewLookScope(camera)) _preview.Render(true, false);
                    rendered = true;
                }
                finally
                {
                    result = _preview.EndStaticPreview();
                    if (!rendered && result) Object.DestroyImmediate(result);
                }
                return result;
            }
            finally
            {
                _target = target; _distance = distance; _aspect = aspect; _fitRequested = fitRequested;
                camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                camera.aspect = cameraAspect; camera.fieldOfView = fieldOfView;
                camera.nearClipPlane = near; camera.farClipPlane = far;
                camera.clearFlags = clearFlags; camera.backgroundColor = background; camera.allowHDR = allowHDR;
            }
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
            _preview.lights[1].intensity = .55f;
            _preview.lights[1].transform.rotation = Quaternion.Euler(320f, 145f, 0f);
            _preview.ambientColor = new Color(.35f, .39f, .45f);
            _meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
            Material bodySource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");
            Material stoneSource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat");
            if (!_meshes || !source || !bodySource || !stoneSource)
            {
                LastError = "Preview requires PrimitiveMeshes.asset and Look_Default, Look_Body and Look_Stone materials from Assets/Render.";
                return;
            }
            _material = new Material(source) { name = "Creature Studio Preview Material", hideFlags = HideFlags.HideAndDontSave };
            _bodyMaterial = new Material(bodySource) { name = "Creature Studio Body Material", hideFlags = HideFlags.HideAndDontSave };
            _stoneMaterial = new Material(stoneSource) { name = "Creature Studio Stone Material", hideFlags = HideFlags.HideAndDontSave };
            _root = new GameObject("Creature Studio Preview") { hideFlags = HideFlags.HideAndDontSave };
            _preview.AddSingleGO(_root);
            _subject = new GameObject("Creature Preview Subject");
            _subject.transform.SetParent(_root.transform, false);
            _ground = new GameObject("Creature Preview Ground");
            _ground.transform.SetParent(_root.transform, false);
            if (_meshes.disc)
            {
                Transform disc = PrimitiveMeshes.Geometry("Ground", _ground.transform, _meshes.disc, _material, new Color(.15f, .19f, .22f));
                disc.localScale = Vector3.one * 8f;
                disc.localPosition = Vector3.down * .012f;
            }
            for (int i = -8; i <= 8; i++)
            {
                Color colour = i == 0 ? new Color(.32f, .40f, .45f) : new Color(.22f, .28f, .31f);
                Transform x = PrimitiveMeshes.Geometry("Grid X", _ground.transform, _meshes.cylinder, _material, colour);
                PrimitiveMeshes.Segment(x, new Vector3(-4f, 0f, i * .5f), new Vector3(4f, 0f, i * .5f), .002f);
                Transform z = PrimitiveMeshes.Geometry("Grid Z", _ground.transform, _meshes.cylinder, _material, colour);
                PrimitiveMeshes.Segment(z, new Vector3(i * .5f, 0f, -4f), new Vector3(i * .5f, 0f, 4f), .002f);
            }
            _selection = new GameObject("Selected Part Bounds");
            _selection.transform.SetParent(_root.transform, false);
            _selectionEdges = new Transform[12];
            for (int i = 0; i < _selectionEdges.Length; i++)
                _selectionEdges[i] = PrimitiveMeshes.Geometry("Selection Edge", _selection.transform, _meshes.cylinder, _material, new Color(1f, .72f, .2f));
            _selection.SetActive(false);
            HideTree(_root);
        }

        void Rebuild()
        {
            _dirty = false;
            _time = 0f;
            _steps = 0;
            DestroyRig();
            if (!_subject) return;
            LastError = null;
            if (!_source) return;
            if (!CreatureValidator.TryValidate(_source, out string error)) { LastError = error; return; }
            _working = Object.Instantiate(_source);
            _working.name = "Creature Studio Working Recipe";
            _working.hideFlags = HideFlags.HideAndDontSave;
            // Init xors its parent id into the authored seed. Compensate only on the private
            // copy so independent previews and exports use identical authored idle and colours.
            IdleDefinition idle = _working.idle;
            idle.seed ^= _subject.transform.GetEntityId().GetHashCode();
            _working.idle = idle;
            _rig = new CreatureRig();
            Material shared = _side == LookSide.Stone ? _stoneMaterial : _material;
            Material body = _side == LookSide.Stone ? _stoneMaterial : _bodyMaterial;
            if (!_rig.Init(_working, _subject.transform, shared, body, _meshes, 1f))
            {
                LastError = "The creature rig could not be built. Check the recipe diagnostics.";
                DestroyRig();
                return;
            }
            _rig.SetReadout(_aim, _health, _charge, _glow);
            _rig.Tick(0f, 0f, Frame);
            HideTree(_subject);
        }

        void DestroyRig()
        {
            // Runtime disposal may defer destruction in Play mode; the private editor scene
            // can clean up immediately to prevent an overlapping old frame while scrubbing.
            GameObject generated = _rig != null && _rig.root ? _rig.root.gameObject : null;
            _rig?.Dispose();
            _rig = null;
            if (generated) Object.DestroyImmediate(generated);
            if (_working) Object.DestroyImmediate(_working);
            _working = null;
        }

        void ConfigureCamera(float width, float height, bool refitAspect = true)
        {
            Camera camera = _preview.camera;
            camera.aspect = width / Mathf.Max(1f, height);
            camera.fieldOfView = 34f;
            if (refitAspect && !Mathf.Approximately(_aspect, camera.aspect)) _fitRequested = true;
            _aspect = camera.aspect;
            if (_fitRequested)
            {
                Bounds bounds = SubjectBounds();
                _target = bounds.center;
                float halfAngle = Mathf.Atan(Mathf.Tan(17f * Mathf.Deg2Rad) * Mathf.Min(1f, camera.aspect));
                _distance = Mathf.Max(1f, bounds.extents.magnitude * 1.18f / Mathf.Sin(halfAngle));
                _fitRequested = false;
            }
            Quaternion rotation = Quaternion.Euler(_orbit.x, _orbit.y, 0f);
            camera.transform.SetPositionAndRotation(_target - rotation * Vector3.forward * _distance, rotation);
            camera.nearClipPlane = .02f;
            camera.farClipPlane = Mathf.Max(100f, _distance * 3f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f, .095f, .115f);
            camera.allowHDR = true;
        }

        Bounds SubjectBounds()
        {
            Bounds bounds = new Bounds(Vector3.up, Vector3.one * .1f);
            bool found = false;
            foreach (Renderer renderer in _subject.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.bounds.size.sqrMagnitude < .00001f) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            float margin = _source ? Mathf.Abs(_source.idle.breathAmount) + Mathf.Sin(Mathf.Abs(_source.idle.swayDegrees) * Mathf.Deg2Rad) : 0f;
            bounds.Expand(bounds.size.magnitude * Mathf.Clamp(margin, .04f, .5f));
            return bounds;
        }

        void UpdateSelection()
        {
            if (!_selection) return;
            bool show = _rig != null && SelectedPartIndex >= 0 && SelectedPartIndex < _rig.partTransforms.Count;
            _selection.SetActive(show);
            if (!show) return;
            Bounds bounds = _rig.partTransforms[SelectedPartIndex].GetComponent<Renderer>().bounds;
            bounds.Expand(.018f);
            int edge = 0;
            for (int axis = 0; axis < 3; axis++)
                for (int a = 0; a < 2; a++)
                    for (int b = 0; b < 2; b++)
                    {
                        Vector3 start = bounds.min, end = bounds.min;
                        int next = (axis + 1) % 3, last = (axis + 2) % 3;
                        start[next] = end[next] = a == 0 ? bounds.min[next] : bounds.max[next];
                        start[last] = end[last] = b == 0 ? bounds.min[last] : bounds.max[last];
                        end[axis] = bounds.max[axis];
                        PrimitiveMeshes.Segment(_selectionEdges[edge++], start, end, .003f);
                    }
        }

        void HandleInput(Rect rect)
        {
            Event e = Event.current;
            int control = GUIUtility.GetControlID("CreatureStudioCamera".GetHashCode(), FocusType.Passive, rect);
            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                _distance = Mathf.Clamp(_distance * Mathf.Exp(e.delta.y * .04f), .3f, 100f);
                GUI.changed = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && e.button <= 2)
            {
                _dragControl = control;
                GUIUtility.hotControl = control;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _dragControl != 0 && GUIUtility.hotControl == _dragControl)
            {
                if (e.shift || e.button == 2)
                    _target += Quaternion.Euler(_orbit.x, _orbit.y, 0f) * new Vector3(-e.delta.x, e.delta.y, 0f) * (_distance / Mathf.Max(100f, rect.height));
                else
                {
                    _orbit.y += e.delta.x * .5f;
                    _orbit.x = Mathf.Clamp(_orbit.x + e.delta.y * .5f, -80f, 85f);
                }
                GUI.changed = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _dragControl != 0 && GUIUtility.hotControl == _dragControl)
            {
                GUIUtility.hotControl = 0;
                _dragControl = 0;
                e.Use();
            }
        }

        static void HideTree(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_dragControl != 0 && GUIUtility.hotControl == _dragControl) GUIUtility.hotControl = 0;
            DestroyRig();
            _preview?.Cleanup();
            _preview = null;
            if (_material) Object.DestroyImmediate(_material);
            if (_bodyMaterial) Object.DestroyImmediate(_bodyMaterial);
            if (_stoneMaterial) Object.DestroyImmediate(_stoneMaterial);
            _root = _subject = null;
        }
    }
}
