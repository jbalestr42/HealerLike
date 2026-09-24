using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's grammar mode, the one it opens in: a grammar preset composed live into a generated
    // recipe, the entity it may derive from, and the authored override the game would show in its place
    public class CreatureGrammarMode
    {
        static readonly string creatureLooksPath = "Assets/Render/Creatures/Data/CreatureLooks.asset";

        readonly CreatureGrammarDrafts _drafts = new CreatureGrammarDrafts();
        readonly List<CreatureGrammarPreset> _assets = new List<CreatureGrammarPreset>();
        readonly List<EntityData> _entities = new List<EntityData>();
        CreatureStudioWindow _window;
        string[] _entityNames = { "None" };
        CreatureGrammarPreset _selected;
        SerializedObject _serialized;
        CreatureRecipe _output;
        CreatureLooks _creatureLooks;
        UnitChannels _channels;
        string[] _warnings = new string[0];
        string[] _notes = new string[0];
        bool _isOverrideShown;

        public CreatureGrammarDrafts drafts { get { return _drafts; } }

        public List<CreatureGrammarPreset> assets { get { return _assets; } }

        public List<EntityData> entities { get { return _entities; } }

        // "None" first, then the entities by name, for the inspector's popup
        public string[] entityNames { get { return _entityNames; } }

        public CreatureGrammarPreset selected { get { return _selected; } }

        public SerializedObject serialized { get { return _serialized; } }

        // The recipe the grammar composed, null while the preset has errors; the mode owns it
        public CreatureRecipe output { get { return _output; } }

        public CreatureLooks creatureLooks { get { return _creatureLooks; } set { _creatureLooks = value; } }

        public UnitChannels channels { get { return _channels; } }

        public string[] warnings { get { return _warnings; } }

        public string[] notes { get { return _notes; } }

        // Whether the viewport shows the game's authored override instead of the composed recipe
        public bool isOverrideShown { get { return _isOverrideShown; } set { _isOverrideShown = value; } }

        public LookSide previewSide
        {
            get
            {
                if (_isOverrideShown)
                {
                    return LookDerivation.Side(_selected.sourceSide);
                }
                return _channels.side;
            }
        }

        public void Init(CreatureStudioWindow window)
        {
            _window = window;
            _creatureLooks = AssetDatabase.LoadAssetAtPath<CreatureLooks>(creatureLooksPath);
            CreatureGrammarDraftCollection kept = _drafts.Restore();
            if (kept != null)
            {
                window.manualSurface = kept.manualSurface;
                if (!string.IsNullOrEmpty(kept.creatureLooksAsset))
                {
                    _creatureLooks = CreatureGrammarDrafts.Load<CreatureLooks>(kept.creatureLooksAsset);
                }
                _selected = _drafts.Selection(kept);
            }

            if (_drafts.items.Count == 0)
            {
                _drafts.NewDraft(_creatureLooks);
            }

            if (_selected == null)
            {
                _selected = _drafts.items[0];
            }

            _serialized = new SerializedObject(_selected);
            Reload();
        }

        public void Dispose()
        {
            _drafts.Persist(_selected, _creatureLooks, _window.manualSurface, _window.isGrammarMode);
            if (_serialized != null)
            {
                _serialized.Dispose();
            }

            _serialized = null;
            if (_output != null)
            {
                Object.DestroyImmediate(_output);
            }

            _output = null;
            _drafts.Dispose();
        }

        // Lists the saved grammar presets and the game's entities again, sorted by name
        public void Reload()
        {
            _assets.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:CreatureGrammarPreset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CreatureGrammarPreset preset = AssetDatabase.LoadAssetAtPath<CreatureGrammarPreset>(path);
                if (preset != null)
                {
                    _assets.Add(preset);
                }
            }

            _assets.Sort(ComparePresets);
            _entities.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:EntityData"))
            {
                EntityData entity = AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
                if (entity != null)
                {
                    _entities.Add(entity);
                }
            }

            _entities.Sort(CompareEntities);
            _entityNames = new string[_entities.Count + 1];
            _entityNames[0] = "None — manual channels";
            for (int i = 0; i < _entities.Count; i++)
            {
                _entityNames[i + 1] = _entities[i].name;
            }
        }

        public void Select(CreatureGrammarPreset preset)
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
            _isOverrideShown = false;
        }

        // Composes the preset again and shows the result, or the game's override when that is what is auditioned
        public void Regenerate()
        {
            if (_selected == null)
            {
                return;
            }

            if (_serialized != null)
            {
                _serialized.Update();
            }

            _warnings = CreatureGrammarValidator.Validate(_selected);
            _notes = CreatureGrammarBudget.Notes(_selected);
            _channels = _selected.Channels();
            CreatureRecipe output = _selected.Compose();
            if (output != null)
            {
                output.hideFlags = HideFlags.HideAndDontSave;
                output.name = CreatureGrammarDrafts.Label(_selected) + " · generated";
            }

            CreatureRecipe old = _output;
            _output = output;
            CreatureRecipe authored = GetOverrideRecipe();
            if (_isOverrideShown && authored == null)
            {
                _isOverrideShown = false;
            }

            if (_isOverrideShown)
            {
                _window.Select(authored);
            }
            else
            {
                _window.Select(_output);
            }

            if (old != null)
            {
                Object.DestroyImmediate(old);
            }
            _window.Repaint();
        }

        // An independent recipe copied from the composed output, the override being auditioned or not
        public CreatureRecipe Bake()
        {
            if (_output == null)
            {
                return null;
            }

            CreatureRecipe recipe = CreatureStudioAuthoring.Clone(_output);
            recipe.name = CreatureGrammarDrafts.Label(_selected) + " baked";
            return recipe;
        }

        public void SaveAs()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save creature grammar preset",
                CreatureGrammarDrafts.Label(_selected), "asset", "Save the editable channels and vocabulary reference.",
                "Assets/Render/Studio/Data/Presets");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            CreatureGrammarPreset copy = Object.Instantiate(_selected);
            copy.hideFlags = HideFlags.None;
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssetIfDirty(copy);
            Reload();
            _window.SelectGrammar(copy);
            EditorGUIUtility.PingObject(copy);
        }

        public bool HasSourceOverride()
        {
            if (_selected == null || _selected.sourceEntity == null || _creatureLooks == null)
            {
                return false;
            }
            return _creatureLooks.entities != null && _creatureLooks.entities.ContainsKey(_selected.sourceEntity);
        }

        // The recipe of the game's authored view for the source entity, when that view carries a creature builder
        public CreatureRecipe GetOverrideRecipe()
        {
            if (!HasSourceOverride())
            {
                return null;
            }

            GameObject view = _creatureLooks.entities[_selected.sourceEntity];
            if (view == null)
            {
                return null;
            }

            CreatureBuilder builder = view.GetComponentInChildren<CreatureBuilder>(true);
            if (builder == null)
            {
                return null;
            }
            return builder.recipe;
        }

        static int ComparePresets(CreatureGrammarPreset a, CreatureGrammarPreset b)
        {
            string labelA = CreatureGrammarDrafts.Label(a);
            string labelB = CreatureGrammarDrafts.Label(b);
            return string.Compare(labelA, labelB, StringComparison.OrdinalIgnoreCase);
        }

        static int CompareEntities(EntityData a, EntityData b)
        {
            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
