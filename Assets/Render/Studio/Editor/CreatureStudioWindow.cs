using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // A workbench over the creature grammar and the recipes it bakes. Grammar mode composes a preset live, parts
    // mode edits a recipe part by part; both preview the real rig. Library on the left, preview in the middle,
    // the mode's inspector on the right. CreatureStudioMenu opens it.
    public class CreatureStudioWindow : EditorWindow
    {
        static readonly Color accent = new Color(0.75f, 0.68f, 1f);
        static readonly Color highlight = new Color(0.22f, 0.18f, 0.32f);
        static readonly Color smallText = new Color(0.62f, 0.68f, 0.75f);
        static readonly Color cardText = new Color(0.85f, 0.88f, 0.94f);
        static readonly float libraryWidth = 210f;
        static readonly float inspectorWidth = 370f;
        static readonly float top = 77f;

        readonly StudioStyles _styles = new StudioStyles();
        readonly CreatureStudioDrafts _drafts = new CreatureStudioDrafts();
        readonly CreatureGrammarMode _grammar = new CreatureGrammarMode();
        readonly CreatureLibraryPane _library = new CreatureLibraryPane();
        readonly CreaturePartsInspector _parts = new CreaturePartsInspector();
        readonly CreatureGrammarInspector _grammarInspector = new CreatureGrammarInspector();
        readonly CreatureViewport _viewport = new CreatureViewport();
        readonly CreatureStudioHeader _header = new CreatureStudioHeader();
        readonly CreatureRosterPane _roster = new CreatureRosterPane();
        bool _isRosterMode;
        bool _isRosterReady;
        CreatureRecipe _selected;
        SerializedObject _serialized;
        bool _isGrammarMode = true;
        CreatureRecipe _partsSelection;
        LookSide _manualSurface;

        public CreatureRecipe selected { get { return _selected; } }

        public SerializedObject serialized { get { return _serialized; } }

        public CreatureStudioDrafts drafts { get { return _drafts; } }

        public CreatureGrammarMode grammar { get { return _grammar; } }

        public CreaturePartsInspector parts { get { return _parts; } }

        public StudioStyles styles { get { return _styles; } }

        public bool isGrammarMode { get { return _isGrammarMode; } }
        public bool isRosterMode { get { return _isRosterMode; } }

        // The recipe parts mode returns to
        public CreatureRecipe partsSelection { get { return _partsSelection; } }

        // The surface parts mode previews on, per recipe
        public LookSide manualSurface { get { return _manualSurface; } set { _manualSurface = value; } }

        public LookSide previewSide
        {
            get
            {
                if (_isGrammarMode)
                {
                    return _grammar.previewSide;
                }
                return _manualSurface;
            }
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Creature Studio");
            minSize = new Vector2(1120f, 680f);
            _viewport.Init(this);
            _header.Init(this);
            _drafts.Init();
            _library.Init(this);
            _parts.Init(this);
            _grammarInspector.Init(this);
            _library.Reload();
            CreatureRecipe first = _drafts.restoredSelection;
            if (first == null && _drafts.items.Count > 0)
            {
                first = _drafts.items[0];
            }

            Select(first);
            _partsSelection = _selected;
            _grammar.Init(this);
            _isGrammarMode = true;
            _isRosterMode = false;
            _grammar.Regenerate();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.projectChanged += ReloadAssets;
            Undo.undoRedoPerformed += OnUndoRedo;
            RenderGrammarLibraryWindow.OnAssetChanged.AddListener(OnGrammarLibraryChanged);
        }

        void OnDisable()
        {
            CreatureRecipe selection = _selected;
            if (_isGrammarMode)
            {
                selection = _partsSelection;
            }

            _drafts.Persist(selection);
            _grammar.Dispose();
            _roster.Dispose();
            _isRosterReady = false;
            RenderGrammarLibraryWindow.OnAssetChanged.RemoveListener(OnGrammarLibraryChanged);
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.projectChanged -= ReloadAssets;
            Undo.undoRedoPerformed -= OnUndoRedo;
            _viewport.Dispose();
            if (_serialized != null)
            {
                _serialized.Dispose();
            }

            _serialized = null;
            _drafts.Dispose();
        }

        void OnGUI()
        {
            if (!_styles.isReady)
            {
                _styles.Init(accent, highlight, smallText, cardText);
            }

            _viewport.HandleShortcuts();
            StudioStyles.DrawBackground(position);
            _header.Draw(position);
            float height = position.height - 87f;
            if (_isRosterMode)
            {
                _roster.Draw(new Rect(10f, top, position.width - 20f, height));
                return;
            }
            Rect library = new Rect(10f, top, libraryWidth, height);
            Rect inspector = new Rect(position.width - inspectorWidth - 10f, top, inspectorWidth, height);
            if (_isGrammarMode)
            {
                _library.DrawGrammar(library);
            }
            else
            {
                _library.DrawParts(library);
            }

            _viewport.Draw(new Rect(libraryWidth + 18f, top, position.width - libraryWidth - inspectorWidth - 36f,
                height));
            if (_isGrammarMode)
            {
                _grammarInspector.Draw(inspector);
            }
            else
            {
                _parts.Draw(inspector);
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }

        public void Select(CreatureRecipe recipe)
        {
            if (_serialized != null)
            {
                _serialized.ApplyModifiedProperties();
                _serialized.Dispose();
            }

            _selected = recipe;
            if (!_isGrammarMode && recipe != null)
            {
                _manualSurface = _drafts.GetSurface(recipe);
            }

            _serialized = null;
            if (recipe != null)
            {
                _serialized = new SerializedObject(recipe);
            }

            _parts.Reset();
            _viewport.Restart();
            RefreshPreview();
            Repaint();
        }

        // Parts mode on the recipe, else on the first draft
        public void SwitchToParts(CreatureRecipe recipe)
        {
            _isRosterMode = false;
            _isGrammarMode = false;
            _partsSelection = recipe;
            if (_partsSelection == null && _drafts.items.Count > 0)
            {
                _partsSelection = _drafts.items[0];
            }
            Select(_partsSelection);
        }

        public void SwitchToGrammar()
        {
            _isRosterMode = false;
            if (!_isGrammarMode)
            {
                _partsSelection = _selected;
            }

            _isGrammarMode = true;
            _grammar.Regenerate();
        }

        public void SelectGrammar(CreatureGrammarPreset preset)
        {
            _isRosterMode = false;
            if (!_isGrammarMode)
            {
                _partsSelection = _selected;
            }

            _grammar.Select(preset);
            _grammarInspector.Reset();
            _isGrammarMode = true;
            _grammar.Regenerate();
        }

        public void RefreshPreview()
        {
            _parts.InvalidateChecks();
            _viewport.Refresh();
        }

        public void ReloadAssets()
        {
            _library.Reload();
            _grammar.Reload();
            if (_isRosterReady)
            {
                _roster.Reload();
            }
            RefreshPreview();
            Repaint();
        }

        // Copies the composed recipe into a new parts draft and opens it
        public void BakeGrammar()
        {
            CreatureRecipe recipe = _grammar.Bake();
            if (recipe == null)
            {
                return;
            }

            _drafts.Add(recipe);
            _drafts.RememberSurface(recipe, _grammar.channels.side);
            SwitchToParts(recipe);
        }

        void OnEditorUpdate()
        {
            if (_isRosterMode)
            {
                if (_roster.RefreshIfChanged())
                {
                    Repaint();
                }
                return;
            }
            _viewport.Tick();
        }

        public void SwitchToRoster()
        {
            _isRosterMode = true;
            if (!_isRosterReady)
            {
                _roster.Init();
                _isRosterReady = true;
            }
            else
            {
                _roster.Refresh();
            }
            Repaint();
        }

        void OnUndoRedo()
        {
            if (_isRosterReady)
            {
                _roster.Refresh();
            }
            if (_serialized != null)
            {
                _serialized.Update();
            }

            if (_isGrammarMode)
            {
                _grammar.Regenerate();
            }
            else
            {
                RenderGrammarLibraryWindow.OnAssetChanged.Invoke(_selected);
            }

            RefreshPreview();
            Repaint();
        }

        void OnGrammarLibraryChanged(Object changed)
        {
            if (_isRosterReady)
            {
                _roster.Refresh();
            }
            if (_isGrammarMode)
            {
                _grammar.Regenerate();
            }
            else
            {
                RefreshPreview();
            }
            Repaint();
        }

        // Reads the selected recipe again after an edit made elsewhere
        public void ReadSelection()
        {
            if (_serialized != null)
            {
                _serialized.Update();
            }

            RefreshPreview();
            Repaint();
        }
    }
}
