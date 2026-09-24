using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // The studio's starting spells: six authored ones, each with its own copy of the shipped geometry, and three
    // that stay linked to the vocabulary through the grammar or a real gameplay handler
    public static class SpellStudioSamples
    {
        public static readonly string Folder = "Assets/Render/Studio/Data/Samples";
        public static readonly string VocabularyPath = "Assets/Render/Spells/Data/EffectVocabulary.asset";
        public static readonly string SpellLooksPath = "Assets/Render/Spells/Data/SpellLooks.asset";
        public static readonly int Count = 9;
        // The samples from this index on keep the vocabulary's own entries
        public static readonly int FirstLinked = 6;

        static readonly string[] names =
        {
            "Verdant Bloom", "Rotfall", "Aegis", "Arc Link", "Astral Refill", "Ember Corona", "Healing pulse",
            "Defence boon", "Opposing debuff"
        };

        // Null, with an error, outside 0..Count-1
        public static string NameAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                Debug.LogError("[SpellStudioSamples] No sample at " + index);
                return null;
            }
            return names[index];
        }

        // Writes the samples that are not in the folder yet; the ones there are the author's and stay as they are
        [MenuItem("Tools/Render/Spell Studio Samples")]
        public static void Create()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(VocabularyPath);
            if (vocabulary == null)
            {
                Debug.LogError("[SpellStudioSamples] The renderer vocabulary is missing: " + VocabularyPath);
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Render/Studio/Data", "Samples");
            }

            for (int i = 0; i < Count; i++)
            {
                string path = Folder + "/" + NameAt(i) + ".asset";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                {
                    continue;
                }

                SpellStudioPreset preset = Build(vocabulary, i);
                if (preset == null)
                {
                    continue;
                }

                AssetDatabase.CreateAsset(preset, path);
                if (EditorUtility.IsPersistent(preset))
                {
                    AssetDatabase.SaveAssetIfDirty(preset);
                }
                else
                {
                    Object.DestroyImmediate(preset);
                }
            }
        }

        // An unsaved preset the caller owns; null when an index is out of range or the vocabulary lacks the entry
        public static SpellStudioPreset Build(EffectVocabulary vocabulary, int index)
        {
            string title = NameAt(index);
            if (title == null || vocabulary == null)
            {
                return null;
            }

            SpellStudioPreset preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            preset.name = title;
            preset.displayName = title;
            preset.vocabulary = vocabulary;
            preset.overrideColour = true;
            preset.side = Entity.EntityType.Player;
            bool isBuilt;
            if (index >= FirstLinked)
            {
                SpellSampleSettings.Linked(preset, index);
                isBuilt = preset.Compose() != null;
            }
            else
            {
                SpellSampleSettings.Authored(preset, index);
                isBuilt = preset.CaptureEntry();
            }

            if (isBuilt)
            {
                return preset;
            }

            Object.DestroyImmediate(preset);
            return null;
        }

        // The family an element reads best in when the studio shows it on its own
        public static EffectFamily Family(EffectElement element)
        {
            switch (element)
            {
                case EffectElement.Rise:
                    return EffectFamily.Heal;
                case EffectElement.Stalks:
                    return EffectFamily.Renew;
                case EffectElement.Drips:
                    return EffectFamily.Rot;
                case EffectElement.Press:
                case EffectElement.Crack:
                    return EffectFamily.Bane;
                case EffectElement.Orbit:
                case EffectElement.Plates:
                case EffectElement.Bud:
                    return EffectFamily.Boon;
                default:
                    return EffectFamily.Damage;
            }
        }
    }
}
