using HealerLike.Render.Deliveries;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public static class DeliverySpellAuthoring
    {
        [MenuItem("Tools/Render/Author Spell-owned Deliveries")]
        public static void Apply()
        {
            DeliveryVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<DeliveryVocabulary>(
                "Assets/Render/Deliveries/Data/DeliveryVocabulary.asset");
            if (!vocabulary)
            {
                throw new System.InvalidOperationException("Missing delivery vocabulary.");
            }

            Undo.RecordObject(vocabulary, "Author spell deliveries");
            DeliverySpellVocabulary.Apply(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssets();
            Debug.Log("[DeliverySpellAuthoring] Authored detached spell fragments, chains and beams.");
        }
    }
}
