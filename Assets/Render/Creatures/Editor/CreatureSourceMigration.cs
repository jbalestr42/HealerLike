using System;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Narrow, idempotent authoring migration. Only these two new fields change in the existing arrays.
    public static class CreatureSourceMigration
    {
        [MenuItem("Tools/Render/Migrate Creature Source Metadata")]
        public static void Run()
        {
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(GrowthStoneVocabulary.AssetPath);
            if (!vocabulary) throw new InvalidOperationException("Missing authored vocabulary");
            Apply(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[CreatureSourceMigration] Source metadata saved; existing arrays and palette preserved.");
        }

        public static void Apply(LookVocabulary vocabulary)
        {
            foreach (var entry in vocabulary.heads)
            {
                Map(entry.Key, entry.Value.plant, false);
                Map(entry.Key, entry.Value.stone, true);
            }
        }

        static void Map(HeadKind family, LookPart[] parts, bool stone)
        {
            int outlets = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                LookPart part = parts[i];
                // Named mapping is exclusively an authoring operation, never a runtime inference.
                bool source = family == HeadKind.GiftHeal
                    ? (stone ? part.id == "Bud" : part.id == "HealSeed")
                    : family == HeadKind.GiftBane ? part.id == "BanePendant" : part.id == "Bud";
                part.isSource = source;
                part.sourceAnchor = source
                    ? (family == HeadKind.Arch || family == HeadKind.SelfTick ? ShapeAnchor.Bottom : ShapeAnchor.Top)
                    : ShapeAnchor.Center;
                parts[i] = part;
                if (source) outlets++;
            }
            if (outlets == 0) throw new InvalidOperationException("No authored outlet for " + family + "/" + stone);
        }
    }
}
