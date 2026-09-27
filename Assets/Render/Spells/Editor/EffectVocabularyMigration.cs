using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells.Editor
{
    public static class EffectVocabularyMigration
    {
        [MenuItem("Tools/Render/Migrate Effect Vocabulary Cells")]
        public static void MigrateAsset()
        {
            EffectVocabulary vocabulary = Selection.activeObject as EffectVocabulary;
            if (vocabulary == null)
            {
                MigrateShippedAsset();
                return;
            }
            if (vocabulary == null)
            {
                Debug.LogError("Select an EffectVocabulary asset first.");
                return;
            }

            Migrate(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssets();
        }

        public static void MigrateShippedAsset()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(
                "Assets/Render/Spells/Data/EffectVocabulary.asset");
            if (vocabulary == null)
            {
                Debug.LogError("Shipped EffectVocabulary asset was not found.");
                return;
            }

            Migrate(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssets();
        }

        public static void Migrate(EffectVocabulary vocabulary)
        {
            if (vocabulary == null) return;
            if (vocabulary.table == null) vocabulary.table = new System.Collections.Generic.Dictionary<EffectCell, EffectCellEntry>();
            foreach (var pair in EffectVocabulary.LegacyCells())
            {
                EffectElement periodic = pair.Value;
                bool hasPeriodic = false;
                if (pair.Key.operation == EffectOperation.Damage)
                {
                    periodic = EffectElement.Drips;
                    hasPeriodic = true;
                }
                else if (pair.Key.operation == EffectOperation.Heal)
                {
                    periodic = EffectElement.Stalks;
                    hasPeriodic = true;
                }
                vocabulary.table[pair.Key] = new EffectCellEntry(pair.Value, periodic, hasPeriodic);
            }
        }
    }
}
