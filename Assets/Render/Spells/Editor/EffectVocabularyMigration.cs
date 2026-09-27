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
                Debug.LogError("Select an EffectVocabulary asset first.");
                return;
            }

            Migrate(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssets();
        }

        public static void Migrate(EffectVocabulary vocabulary)
        {
            if (vocabulary == null) return;
            if (vocabulary.cells == null) vocabulary.cells = new System.Collections.Generic.Dictionary<EffectCell, EffectElement>();
            foreach (var pair in EffectVocabulary.LegacyCells()) vocabulary.cells[pair.Key] = pair.Value;
        }
    }
}
