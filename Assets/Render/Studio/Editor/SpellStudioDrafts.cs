using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // The spell studio's unsaved presets: one per vocabulary element and the linked samples on a first open, else
    // what the author left, kept in EditorPrefs by value with the asset references as GUIDs
    public class SpellStudioDrafts
    {
        static readonly string defaultName = "Untitled spell";

        readonly List<SpellStudioPreset> _items = new List<SpellStudioPreset>();
        SpellStudioPreset _restoredSelection;

        public List<SpellStudioPreset> items { get { return _items; } }

        // The draft or saved preset selected when the drafts were last kept, null on a first open
        public SpellStudioPreset restoredSelection { get { return _restoredSelection; } }

        static string key { get { return "HealerLike.SpellStudio.Drafts." + Application.dataPath; } }

        public void Init()
        {
            if (!Restore())
            {
                CreateDefaults();
            }
        }

        public void Persist(SpellStudioPreset selected)
        {
            SpellDraftCollection collection = new SpellDraftCollection();
            collection.selectedDraftIndex = _items.IndexOf(selected);
            collection.selectedAssetGuid = "";
            if (selected != null && AssetDatabase.Contains(selected))
            {
                collection.selectedAssetGuid = Guid(selected);
            }

            foreach (SpellStudioPreset draft in _items)
            {
                if (draft == null)
                {
                    continue;
                }

                SpellDraftRecord record = new SpellDraftRecord();
                record.json = JsonUtility.ToJson(draft);
                record.vocabularyGuid = Guid(draft.vocabulary);
                record.handlerGuid = Guid(draft.sourceHandler);
                record.looksGuid = Guid(draft.spellLooks);
                record.projectileGuid = Guid(draft.sourceProjectile);
                collection.items.Add(record);
            }
            EditorPrefs.SetString(key, JsonUtility.ToJson(collection));
        }

        public void Dispose()
        {
            foreach (SpellStudioPreset draft in _items)
            {
                if (draft != null)
                {
                    UnityEngine.Object.DestroyImmediate(draft);
                }
            }
            _items.Clear();
            _restoredSelection = null;
        }

        // A blank preset on the given vocabulary, the shipped one when null
        public SpellStudioPreset NewDraft(EffectVocabulary vocabulary)
        {
            SpellStudioPreset draft = Add(ScriptableObject.CreateInstance<SpellStudioPreset>());
            draft.name = defaultName;
            draft.displayName = defaultName;
            draft.vocabulary = vocabulary;
            if (draft.vocabulary == null)
            {
                draft.vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(SpellStudioSamples.VocabularyPath);
            }
            draft.spellLooks = AssetDatabase.LoadAssetAtPath<SpellLooks>(SpellStudioSamples.SpellLooksPath);
            return draft;
        }

        // A held defence boon on the grammar, the channels a new grammar preset starts from
        public SpellStudioPreset NewGrammarDraft(EffectVocabulary vocabulary)
        {
            SpellStudioPreset draft = NewDraft(vocabulary);
            draft.mode = SpellStudioMode.GrammarChannels;
            draft.family = EffectFamily.Boon;
            draft.attributeGroup = AttributeGroup.Defence;
            draft.tempo = EffectTempo.ForDuration;
            draft.name = "Defence boon";
            draft.displayName = draft.name;
            return draft;
        }

        public SpellStudioPreset NewHandlerDraft(EffectVocabulary vocabulary, ABuffHandlerFactory handler)
        {
            SpellStudioPreset draft = NewDraft(vocabulary);
            draft.mode = SpellStudioMode.GameplayHandler;
            draft.sourceHandler = handler;
            draft.name = HandlerLabel(handler);
            draft.displayName = draft.name;
            draft.isSameSide = true;
            return draft;
        }

        // A detached copy: the authored entry is copied too, the vocabulary stays shared
        public SpellStudioPreset Duplicate(SpellStudioPreset source)
        {
            SpellStudioPreset copy = Add(UnityEngine.Object.Instantiate(source));
            copy.name = Label(source) + " copy";
            copy.displayName = copy.name;
            return copy;
        }

        public static string Label(SpellStudioPreset preset)
        {
            if (string.IsNullOrWhiteSpace(preset.displayName))
            {
                return preset.name;
            }
            return preset.displayName;
        }

        // The handler's folder under Assets/Data, then its asset name
        public static string HandlerLabel(ABuffHandlerFactory handler)
        {
            string path = AssetDatabase.GetAssetPath(handler);
            string folder = Path.GetFileName(Path.GetDirectoryName(path));
            return ObjectNames.NicifyVariableName(folder) + " / " + handler.name;
        }

        bool Restore()
        {
            if (!StudioPrefs.TryRead(key, out SpellDraftCollection collection))
            {
                return false;
            }
            if (collection.items == null)
            {
                StudioPrefs.Discard(key);
                return false;
            }

            foreach (SpellDraftRecord record in collection.items)
            {
                if (record == null || !StudioPrefs.TryDraft(record.json, out SpellStudioPreset draft))
                {
                    Dispose();
                    StudioPrefs.Discard(key);
                    return false;
                }
                Add(draft);
                draft.name = draft.displayName;
                draft.vocabulary = Load<EffectVocabulary>(record.vocabularyGuid);
                draft.sourceHandler = Load<ABuffHandlerFactory>(record.handlerGuid);
                draft.spellLooks = Load<SpellLooks>(record.looksGuid);
                draft.sourceProjectile = Load<GameObject>(record.projectileGuid);
            }

            _restoredSelection = Load<SpellStudioPreset>(collection.selectedAssetGuid);
            int index = collection.selectedDraftIndex;
            if (_restoredSelection == null && index >= 0 && index < _items.Count)
            {
                _restoredSelection = _items[index];
            }
            return _items.Count > 0;
        }

        void CreateDefaults()
        {
            string path = SpellStudioSamples.VocabularyPath;
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(path);
            if (vocabulary == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:EffectVocabulary");
                if (guids.Length > 0)
                {
                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(path);
                }
            }

            foreach (EffectElement element in Enum.GetValues(typeof(EffectElement)))
            {
                SpellStudioPreset draft = Add(ScriptableObject.CreateInstance<SpellStudioPreset>());
                draft.name = ObjectNames.NicifyVariableName(element.ToString());
                draft.displayName = draft.name;
                draft.vocabulary = vocabulary;
                draft.element = element;
                draft.family = SpellStudioSamples.Family(element);
                draft.durationSeconds = 2.4f;
            }

            for (int i = SpellStudioSamples.FirstLinked; i < SpellStudioSamples.Count; i++)
            {
                SpellStudioPreset sample = SpellStudioSamples.Build(vocabulary, i);
                if (sample != null)
                {
                    Add(sample);
                }
            }
        }

        SpellStudioPreset Add(SpellStudioPreset draft)
        {
            draft.hideFlags = HideFlags.HideAndDontSave;
            _items.Add(draft);
            return draft;
        }

        static string Guid(UnityEngine.Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

        static AssetType Load<AssetType>(string guid) where AssetType : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<AssetType>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
