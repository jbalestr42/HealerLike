using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // A workbench over the renderer's spell vocabulary: presets drafted, previewed on a creature and saved,
    // without touching the shared assets unless asked. Library on the left, preview and timeline in the middle,
    // the preset's fields on the right. SpellStudioMenu opens it.
    public class SpellStudioWindow : EditorWindow
    {
        static readonly Color accent = new Color(0.4f, 0.86f, 0.74f);
        static readonly Color highlight = new Color(0.14f, 0.25f, 0.25f);
        static readonly Color smallText = new Color(0.6f, 0.67f, 0.73f);
        static readonly Color cardText = new Color(0.84f, 0.89f, 0.92f);
        static readonly float libraryWidth = 210f;
        static readonly float inspectorWidth = 340f;
        static readonly float gap = 8f;
        static readonly float top = 77f;

        // The preview's target, never a gameplay rule; the surface is 0 automatic, 1 plant, 2 stone
        [SerializeField] CreatureRecipe _targetCreature;
        [SerializeField] int _targetSurface;

        readonly StudioStyles _styles = new StudioStyles();
        readonly StudioTimeline _timeline = new StudioTimeline();
        readonly SpellStudioDrafts _drafts = new SpellStudioDrafts();
        readonly SpellLibraryPane _library = new SpellLibraryPane();
        readonly SpellViewport _viewport = new SpellViewport();
        readonly SpellInspectorPane _inspector = new SpellInspectorPane();
        SpellStudioPreset _selected;
        SerializedObject _serialized;

        public SpellStudioPreset selected { get { return _selected; } }

        public SerializedObject serialized { get { return _serialized; } }

        public SpellStudioDrafts drafts { get { return _drafts; } }

        public StudioStyles styles { get { return _styles; } }

        public StudioTimeline timeline { get { return _timeline; } }

        public CreatureRecipe targetCreature { get { return _targetCreature; } set { _targetCreature = value; } }

        public int targetSurface { get { return _targetSurface; } set { _targetSurface = value; } }

        // The preview length: one motion cycle for an impact, the authored length for a status
        public float duration
        {
            get
            {
                if (_selected == null)
                {
                    return 2f;
                }
                return Mathf.Max(0.01f, _selected.previewDuration);
            }
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Spell Studio");
            minSize = new Vector2(1040f, 640f);
            _viewport.Init(this);
            _drafts.Init();
            _library.Init(this);
            _inspector.Init(this);
            _library.Reload();
            SpellStudioPreset first = _drafts.restoredSelection;
            if (first == null && _drafts.items.Count > 0)
            {
                first = _drafts.items[0];
            }

            Select(first);
            _timeline.Init();
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += ReadSelection;
            EditorApplication.projectChanged += ReloadAssets;
            RenderGrammarLibraryWindow.OnAssetChanged.AddListener(OnGrammarAssetChanged);
        }

        void OnDisable()
        {
            _drafts.Persist(_selected);
            EditorApplication.update -= OnEditorUpdate;
            Undo.undoRedoPerformed -= ReadSelection;
            EditorApplication.projectChanged -= ReloadAssets;
            RenderGrammarLibraryWindow.OnAssetChanged.RemoveListener(OnGrammarAssetChanged);
            _viewport.Dispose();
            _drafts.Dispose();
            if (_serialized != null)
            {
                _serialized.Dispose();
            }
            _serialized = null;
        }

        void OnGUI()
        {
            if (!_styles.isReady)
            {
                _styles.Init(accent, highlight, smallText, cardText);
            }

            _viewport.HandleShortcuts();
            StudioStyles.DrawBackground(position);
            DrawHeader();
            float height = position.height - 87f;
            float viewportWidth = position.width - libraryWidth - inspectorWidth - 20f - gap * 2f;
            _library.Draw(new Rect(10f, top, libraryWidth, height));
            _viewport.Draw(new Rect(libraryWidth + 10f + gap, top, viewportWidth, height));
            _inspector.Draw(new Rect(position.width - inspectorWidth - 10f, top, inspectorWidth, height));
            if (GUI.changed)
            {
                Repaint();
            }
        }

        public void Select(SpellStudioPreset preset)
        {
            if (_serialized != null)
            {
                _serialized.ApplyModifiedProperties();
                _serialized.Dispose();
            }

            _selected = preset;
            _serialized = null;
            if (preset != null)
            {
                _serialized = new SerializedObject(preset);
            }

            _timeline.time = 0f;
            _inspector.Reset();
            RefreshPreview();
            Repaint();
        }

        // Reads the selected preset again after an undo or an edit made elsewhere
        public void ReadSelection()
        {
            if (_serialized != null)
            {
                _serialized.Update();
            }

            RefreshPreview();
            Repaint();
        }

        public void RefreshPreview()
        {
            _inspector.InvalidateChecks();
            _viewport.preview.Refresh();
        }

        public void ReloadAssets()
        {
            RefreshPreview();
            _library.Reload();
            Repaint();
        }

        public void NewDraft()
        {
            Select(_drafts.NewDraft(SelectedVocabulary()));
        }

        public void NewGrammarDraft()
        {
            Select(_drafts.NewGrammarDraft(SelectedVocabulary()));
        }

        public void NewHandlerDraft(ABuffHandlerFactory handler)
        {
            Select(_drafts.NewHandlerDraft(SelectedVocabulary(), handler));
        }

        public void Duplicate()
        {
            if (_selected != null)
            {
                Select(_drafts.Duplicate(_selected));
            }
        }

        void OnEditorUpdate()
        {
            if (_timeline.Tick(duration, _selected != null))
            {
                Repaint();
            }
        }

        void OnGrammarAssetChanged(Object asset)
        {
            ReadSelection();
        }

        EffectVocabulary SelectedVocabulary()
        {
            if (_selected == null)
            {
                return null;
            }
            return _selected.vocabulary;
        }

        void DrawHeader()
        {
            GUI.Label(new Rect(20f, 12f, 260f, 30f), "Spell Studio", _styles.title);
            if (GUI.Button(new Rect(260f, 17f, 115f, 24f), "Creatures →"))
            {
                EditorApplication.ExecuteMenuItem("Tools/Render/Creature Studio");
            }

            GUI.Label(new Rect(21f, 43f, 520f, 22f), "RENDER LAB  /  Create, shape and rehearse your spell effects",
                _styles.small);
            Rect rect = new Rect(position.width - 325f, 24f, 95f, 26f);
            if (GUI.Button(rect, "New spell"))
            {
                NewDraft();
                GUIUtility.ExitGUI();
            }

            rect.x += 102f;
            using (new EditorGUI.DisabledScope(_selected == null))
            {
                if (GUI.Button(rect, "Save as…"))
                {
                    SaveAs();
                    GUIUtility.ExitGUI();
                }
            }

            rect.x += 102f;
            using (new EditorGUI.DisabledScope(_selected == null || !AssetDatabase.Contains(_selected)))
            {
                if (GUI.Button(rect, "Save"))
                {
                    AssetDatabase.SaveAssetIfDirty(_selected);
                    ShowNotification(new GUIContent("Preset saved"));
                }
            }
        }

        void SaveAs()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save spell preset", SpellStudioDrafts.Label(_selected),
                "asset", "Choose where to store this spell preset.", "Assets/Render/Studio/Data/Presets");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            SpellStudioPreset copy = SpellStudioPublishing.SaveCopy(_selected,
                AssetDatabase.GenerateUniqueAssetPath(path));
            if (copy == null)
            {
                return;
            }

            ReloadAssets();
            Select(copy);
            EditorGUIUtility.PingObject(copy);
            ShowNotification(new GUIContent("Spell preset saved"));
        }
    }
}
