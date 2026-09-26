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
        readonly CreatureStudioLayout _layout = new CreatureStudioLayout();
        readonly CreatureStudioDrafts _drafts = new CreatureStudioDrafts();
        readonly CreatureGrammarMode _grammar = new CreatureGrammarMode();
        bool _isRosterMode;
        bool _isRosterReady;
        readonly StudioSelection<CreatureRecipe> _selection = new StudioSelection<CreatureRecipe>();
        bool _isGrammarMode = true;
        CreatureRecipe _partsSelection;
        LookSide _manualSurface;

        public CreatureRecipe selected { get { return _selection.asset; } }

        public SerializedObject serialized { get { return _selection.serialized; } }

        public CreatureStudioDrafts drafts { get { return _drafts; } }

        public CreatureGrammarMode grammar { get { return _grammar; } }

        public CreaturePartsInspector parts { get { return _layout.parts; } }

        public StudioStyles styles { get { return _layout.styles; } }

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
            _layout.Init(this);
            _drafts.Init();
            _layout.library.Reload();
            CreatureRecipe first = _drafts.restoredSelection;
            if (first == null && _drafts.items.Count > 0)
            {
                first = _drafts.items[0];
            }

            Select(first);
            _partsSelection = _selection.asset;
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
            RenderGrammarLibraryWindow.OnAssetChanged.RemoveListener(OnGrammarLibraryChanged);
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.projectChanged -= ReloadAssets;
            Undo.undoRedoPerformed -= OnUndoRedo;
            try
            {
                _drafts.Persist(_isGrammarMode ? _partsSelection : _selection.asset);
            }
            finally
            {
                try
                {
                    _grammar.Dispose();
                }
                finally
                {
                    _layout.Dispose();
                    _isRosterReady = false;
                    _selection.Dispose();
                    _drafts.Dispose();
                }
            }
        }

        void OnGUI()
        {
            _layout.Draw();
        }

        public void Select(CreatureRecipe recipe)
        {
            _selection.Select(recipe);
            if (!_isGrammarMode && recipe != null)
            {
                _manualSurface = _drafts.GetSurface(recipe);
            }

            _layout.parts.Reset();
            _layout.viewport.Restart();
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
                _partsSelection = _selection.asset;
            }

            _isGrammarMode = true;
            _grammar.Regenerate();
        }

        public void SelectGrammar(CreatureGrammarPreset preset)
        {
            _isRosterMode = false;
            if (!_isGrammarMode)
            {
                _partsSelection = _selection.asset;
            }

            _grammar.Select(preset);
            _layout.grammarInspector.Reset();
            _isGrammarMode = true;
            _grammar.Regenerate();
        }

        public void RefreshPreview()
        {
            _layout.parts.InvalidateChecks();
            _layout.viewport.Refresh();
        }

        public void ReloadAssets()
        {
            _layout.library.Reload();
            _grammar.Reload();
            if (_isRosterReady)
            {
                _layout.roster.Reload();
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
                if (_layout.roster.RefreshIfChanged())
                {
                    Repaint();
                }

                return;
            }

            _layout.viewport.Tick();
        }

        public void SwitchToRoster()
        {
            _isRosterMode = true;
            if (!_isRosterReady)
            {
                _layout.roster.Init();
                _isRosterReady = true;
            }
            else
            {
                _layout.roster.Refresh();
            }

            Repaint();
        }

        void OnUndoRedo()
        {
            if (_isRosterReady)
            {
                _layout.roster.Refresh();
            }

            if (_selection.serialized != null)
            {
                _selection.serialized.Update();
            }

            if (_isGrammarMode)
            {
                _grammar.Regenerate();
            }
            else
            {
                RenderGrammarLibraryWindow.OnAssetChanged.Invoke(_selection.asset);
            }

            RefreshPreview();
            Repaint();
        }

        void OnGrammarLibraryChanged(Object changed)
        {
            if (_isRosterReady)
            {
                _layout.roster.Refresh();
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
            if (_selection.serialized != null)
            {
                _selection.serialized.Update();
            }

            RefreshPreview();
            Repaint();
        }
    }
}
