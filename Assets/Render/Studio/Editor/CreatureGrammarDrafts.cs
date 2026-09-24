using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's unsaved grammar presets, kept in EditorPrefs by value with their vocabulary and source
    // entity as GUIDs, next to the override table and the parts preview surface the studio was left on
    public class CreatureGrammarDrafts
    {
        static readonly string vocabularyPath = "Assets/Render/Creatures/Data/LookVocabulary.asset";

        readonly List<CreatureGrammarPreset> _items = new List<CreatureGrammarPreset>();

        public List<CreatureGrammarPreset> items { get { return _items; } }

        static string key { get { return "HealerLike.CreatureStudio.GrammarDrafts." + Application.dataPath; } }

        // A blank preset on the override table's vocabulary, the shipped one without a table
        public CreatureGrammarPreset NewDraft(CreatureLooks creatureLooks)
        {
            CreatureGrammarPreset preset = Add(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
            preset.name = "Grammar draft";
            preset.displayName = preset.name;
            if (creatureLooks != null)
            {
                preset.vocabulary = creatureLooks.vocabulary;
            }
            else
            {
                preset.vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(vocabularyPath);
            }
            return preset;
        }

        public CreatureGrammarPreset Duplicate(CreatureGrammarPreset source)
        {
            CreatureGrammarPreset copy = Add(Object.Instantiate(source));
            copy.name = Label(source) + " copy";
            copy.displayName = copy.name;
            return copy;
        }

        public void Persist(CreatureGrammarPreset selected, CreatureLooks creatureLooks, LookSide manualSurface,
            bool isGrammarMode)
        {
            CreatureGrammarDraftCollection collection = new CreatureGrammarDraftCollection();
            collection.manualSurface = manualSurface;
            collection.creatureLooksAsset = Guid(creatureLooks);
            collection.grammarMode = isGrammarMode;
            collection.selectedIndex = _items.IndexOf(selected);
            collection.selectedAsset = Guid(selected);
            foreach (CreatureGrammarPreset draft in _items)
            {
                if (draft == null)
                {
                    continue;
                }

                CreatureGrammarDraftRecord record = new CreatureGrammarDraftRecord();
                record.json = JsonUtility.ToJson(draft);
                record.vocabulary = Guid(draft.vocabulary);
                record.source = Guid(draft.sourceEntity);
                collection.items.Add(record);
            }
            EditorPrefs.SetString(key, JsonUtility.ToJson(collection));
        }

        // The kept collection, its drafts added to the list; null when nothing was kept
        public CreatureGrammarDraftCollection Restore()
        {
            string json = StudioPrefs.ReadJson(key);
            if (json == null)
            {
                return null;
            }

            CreatureGrammarDraftCollection collection = JsonUtility.FromJson<CreatureGrammarDraftCollection>(json);
            if (collection == null || collection.items == null)
            {
                return null;
            }

            foreach (CreatureGrammarDraftRecord record in collection.items)
            {
                CreatureGrammarPreset preset = Add(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
                JsonUtility.FromJsonOverwrite(record.json, preset);
                preset.name = Label(preset);
                preset.vocabulary = Load<LookVocabulary>(record.vocabulary);
                preset.sourceEntity = Load<EntityData>(record.source);
            }
            return collection;
        }

        // The kept selection: a saved preset, else a draft by index
        public CreatureGrammarPreset Selection(CreatureGrammarDraftCollection collection)
        {
            CreatureGrammarPreset selected = Load<CreatureGrammarPreset>(collection.selectedAsset);
            int index = collection.selectedIndex;
            if (selected == null && index >= 0 && index < _items.Count)
            {
                selected = _items[index];
            }
            return selected;
        }

        public void Dispose()
        {
            foreach (CreatureGrammarPreset draft in _items)
            {
                if (draft != null)
                {
                    Object.DestroyImmediate(draft);
                }
            }
            _items.Clear();
        }

        public static string Label(CreatureGrammarPreset preset)
        {
            if (string.IsNullOrWhiteSpace(preset.displayName))
            {
                return preset.name;
            }
            return preset.displayName;
        }

        public static AssetType Load<AssetType>(string guid) where AssetType : Object
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<AssetType>(AssetDatabase.GUIDToAssetPath(guid));
        }

        CreatureGrammarPreset Add(CreatureGrammarPreset preset)
        {
            preset.hideFlags = HideFlags.HideAndDontSave;
            _items.Add(preset);
            return preset;
        }

        static string Guid(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}
