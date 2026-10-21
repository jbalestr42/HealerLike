using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Creatures
{
    public static class CreatureAssetAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        static readonly string materialPath = "Assets/Render/Look/Look_Default.mat";
        static readonly string bodyMaterialPath = "Assets/Render/Look/Look_Body.mat";
        static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
        static readonly int healerRoots = 13;
        static readonly int healerArms = 2;
        static readonly int healerSeed = 17;

        [MenuItem("Tools/Render/Author Creature Assets")]
        public static void Author()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath);
            LookVocabulary vocabulary = CreatureRecipeAuthoring.LoadVocabulary();
            if (!material || !meshes || !vocabulary)
            {
                Debug.LogError($"[CreatureAssetAuthoring] Missing {materialPath}, {meshesPath} or the vocabulary.");
                return;
            }

            foreach (string folder in new string[] { "Data", "Prefabs" })
            {
                if (!AssetDatabase.IsValidFolder(root + folder))
                {
                    AssetDatabase.CreateFolder(root.TrimEnd('/'), folder);
                }
            }

            List<CreaturePart> parts = CreatureRecipeParts.Healer(vocabulary);
            CreatureRecipe healer = CreatureRecipeAuthoring.SaveRecipe(
                "Healer",
                parts,
                healerRoots,
                healerArms,
                healerSeed,
                vocabulary
            );
            if (!healer)
            {
                return;
            }

            // Every other unit is derived from its data, the healer is the one authored view
            CharacterView(healer, material, meshes);
            AssetDatabase.SaveAssetIfDirty(healer);
            Debug.Log("[CreatureAssetAuthoring] Healer recipe and view prefab authored.");
        }

        static void CharacterView(CreatureRecipe recipe, Material material, PrimitiveMeshes meshes)
        {
            GameObject view = new GameObject("HealerCharacter");
            try
            {
                view.AddComponent<StatusObserver>();
                CharacterView characterView = view.AddComponent<CharacterView>();
                using (SerializedObject data = new SerializedObject(characterView))
                {
                    data.FindProperty("_recipe").objectReferenceValue = recipe;
                    data.FindProperty("_showBody").boolValue = false;
                    data.FindProperty("_visualAnchor").objectReferenceValue = view.transform;
                    data.FindProperty("_material").objectReferenceValue = material;
                    data.FindProperty("_bodyMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(
                        bodyMaterialPath
                    );
                    data.FindProperty("_meshes").objectReferenceValue = meshes;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                view.AddComponent<HealPulse>();
                view.AddComponent<TrampleZone>();
                PrefabUtility.SaveAsPrefabAsset(view, root + "Prefabs/HealerCharacter.prefab");
            }
            finally
            {
                Object.DestroyImmediate(view);
            }
        }
    }
}
