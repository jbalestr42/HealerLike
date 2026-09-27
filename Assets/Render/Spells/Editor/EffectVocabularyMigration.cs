using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using System;
using System.Collections.Generic;

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

        // Safe for batchmode and idempotent. This is intentionally explicit rather than
        // an import hook, so opening an old project cannot rewrite authored assets.
        public static void MigrateAllAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EffectVocabulary"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(path);
                Migrate(vocabulary);
                if (vocabulary != null) EditorUtility.SetDirty(vocabulary);
            }
            AssetDatabase.SaveAssets();
        }

        public static void Migrate(EffectVocabulary vocabulary)
        {
            if (vocabulary == null) return;
            if (vocabulary.table == null) vocabulary.table = new System.Collections.Generic.Dictionary<EffectCell, EffectCellEntry>();
            if (vocabulary.cells == null) vocabulary.cells = new Dictionary<EffectCell, EffectCellEntries>();
            if (vocabulary.pieces == null) vocabulary.pieces = new Dictionary<EffectPiece, ElementEntry>();
            foreach (var pair in vocabulary.elements ?? new Dictionary<EffectElement, ElementEntry>())
            {
                if (pair.Value == null) continue;
                if (string.IsNullOrEmpty(pair.Value.label)) pair.Value.label = pair.Key.ToString();
            }
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
                ElementEntry once = vocabulary.elements != null && vocabulary.elements.TryGetValue(pair.Value, out ElementEntry onceEntry)
                    ? onceEntry : null;
                ElementEntry periodicEntry = vocabulary.elements != null && vocabulary.elements.TryGetValue(periodic, out ElementEntry periodicValue)
                    ? periodicValue : null;
                vocabulary.cells[pair.Key] = new EffectCellEntries(once, periodicEntry, hasPeriodic);
            }

            CopyPiece(vocabulary, EffectPiece.Beam, EffectElement.Beam);
            CopyPiece(vocabulary, EffectPiece.Ring, EffectElement.Ring);
            CopyPiece(vocabulary, EffectPiece.Litter, EffectElement.Litter);
        }

        static void CopyPiece(EffectVocabulary vocabulary, EffectPiece piece, EffectElement legacy)
        {
            if (vocabulary.pieces.ContainsKey(piece)) return;
            if (vocabulary.elements != null && vocabulary.elements.TryGetValue(legacy, out ElementEntry entry))
            {
                vocabulary.pieces[piece] = entry;
            }
        }
    }
}
