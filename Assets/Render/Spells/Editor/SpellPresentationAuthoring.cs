using UnityEditor;
using HealerLike.Render.Deliveries;

namespace HealerLike.Render.Spells.Editor
{
    public static class SpellPresentationAuthoring
    {
        public static void Apply()
        {
            SpellPolishVocabulary.Author();
            foreach (string guid in AssetDatabase.FindAssets("t:DeliveryVocabulary", new[] { "Assets/Render" }))
            {
                DeliveryVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<DeliveryVocabulary>(
                    AssetDatabase.GUIDToAssetPath(guid));
                DeliveryPresentationPresets.Apply(vocabulary);
                EditorUtility.SetDirty(vocabulary);
                AssetDatabase.SaveAssetIfDirty(vocabulary);
            }
        }
    }
}
