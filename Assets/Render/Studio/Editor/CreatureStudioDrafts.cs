using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's unsaved recipes, the starters on a first open, else what the author left, kept in
    // EditorPrefs by value. Each recipe remembers the surface it is previewed on: drafts in memory and with the
    // drafts, saved recipes in EditorPrefs by GUID.
    public class CreatureStudioDrafts
    {
        readonly List<CreatureRecipe> _items = new List<CreatureRecipe>();
        readonly Dictionary<CreatureRecipe, LookSide> _surfaces = new Dictionary<CreatureRecipe, LookSide>();
        CreatureRecipe _restoredSelection;

        public List<CreatureRecipe> items { get { return _items; } }

        public CreatureRecipe restoredSelection { get { return _restoredSelection; } }

        static string key { get { return "HealerLike.CreatureStudio.Drafts." + Application.dataPath; } }

        public void Init()
        {
            if (Restore())
            {
                return;
            }

            for (int i = 0; i < CreatureStudioAuthoring.SampleNames.Length; i++)
            {
                CreatureRecipe draft = CreatureStudioAuthoring.BuildSample(i);
                if (draft != null)
                {
                    Add(draft);
                }
            }
        }

        public CreatureRecipe Add(CreatureRecipe draft)
        {
            draft.hideFlags = HideFlags.HideAndDontSave;
            _items.Add(draft);
            return draft;
        }

        public void Persist(CreatureRecipe selection)
        {
            CreatureDraftCollection collection = new CreatureDraftCollection();
            collection.selectedIndex = _items.IndexOf(selection);
            collection.selectedAsset = "";
            if (selection != null && AssetDatabase.Contains(selection))
            {
                collection.selectedAsset = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selection));
            }

            foreach (CreatureRecipe draft in _items)
            {
                if (draft == null)
                {
                    continue;
                }

                CreatureDraftRecord record = new CreatureDraftRecord();
                record.name = draft.name;
                record.json = JsonUtility.ToJson(draft);
                record.surface = GetSurface(draft);
                record.hasSurface = true;
                collection.items.Add(record);
            }
            EditorPrefs.SetString(key, JsonUtility.ToJson(collection));
        }

        public void Dispose()
        {
            foreach (CreatureRecipe draft in _items)
            {
                if (draft != null)
                {
                    Object.DestroyImmediate(draft);
                }
            }
            _items.Clear();
            _surfaces.Clear();
            _restoredSelection = null;
        }

        // The surface the author chose for the recipe, else the one its body reads as
        public LookSide GetSurface(CreatureRecipe recipe)
        {
            if (_surfaces.ContainsKey(recipe))
            {
                return _surfaces[recipe];
            }

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(recipe));
            if (!string.IsNullOrEmpty(guid) && EditorPrefs.HasKey(SurfaceKey(guid)))
            {
                return (LookSide)EditorPrefs.GetInt(SurfaceKey(guid));
            }
            return SpellPreviewTarget.InferSide(recipe);
        }

        // A preview setting only: the recipe is neither dirtied nor announced
        public void RememberSurface(CreatureRecipe recipe, LookSide side)
        {
            if (recipe == null)
            {
                return;
            }

            _surfaces[recipe] = side;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(recipe));
            if (!string.IsNullOrEmpty(guid))
            {
                EditorPrefs.SetInt(SurfaceKey(guid), (int)side);
            }
        }

        bool Restore()
        {
            if (!StudioPrefs.TryRead(key, out CreatureDraftCollection collection))
            {
                return false;
            }
            if (collection.items == null)
            {
                StudioPrefs.Discard(key);
                return false;
            }

            foreach (CreatureDraftRecord record in collection.items)
            {
                if (record == null || !StudioPrefs.TryDraft(record.json, out CreatureRecipe draft))
                {
                    Dispose();
                    StudioPrefs.Discard(key);
                    return false;
                }
                Add(draft);
                draft.name = record.name;
                if (record.hasSurface)
                {
                    _surfaces[draft] = record.surface;
                }
            }

            if (!string.IsNullOrEmpty(collection.selectedAsset))
            {
                string path = AssetDatabase.GUIDToAssetPath(collection.selectedAsset);
                _restoredSelection = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
            }

            int index = collection.selectedIndex;
            if (_restoredSelection == null && index >= 0 && index < _items.Count)
            {
                _restoredSelection = _items[index];
            }
            return _items.Count > 0;
        }

        static string SurfaceKey(string guid)
        {
            return "HealerLike.CreatureStudio.Surface." + Application.dataPath + "." + guid;
        }
    }
}
