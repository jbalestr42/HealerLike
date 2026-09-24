using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Creatures
{
    public static class CreatureAssetAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        static readonly string materialPath = "Assets/Render/Look/Look_Default.mat";
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

            Directory.CreateDirectory(root + "Data");
            Directory.CreateDirectory(root + "Prefabs");
            List<CreaturePart> parts = CreatureRecipeParts.Healer(vocabulary);
            CreatureRecipe healer = CreatureRecipeAuthoring.SaveRecipe("Healer", parts, healerRoots, healerArms,
                healerSeed, vocabulary);
            if (!healer)
            {
                return;
            }

            // Every other unit is derived from its data, the healer is the one authored view
            CharacterView(healer, material, meshes);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreatureAssetAuthoring] Healer recipe and view prefab authored.");
        }

        static void CharacterView(CreatureRecipe recipe, Material material, PrimitiveMeshes meshes)
        {
            GameObject view = new GameObject("HealerCharacter");
            view.AddComponent<StatusObserver>();
            CharacterView characterView = view.AddComponent<CharacterView>();
            SerializedObject data = new SerializedObject(characterView);
            data.FindProperty("_recipe").objectReferenceValue = recipe;
            data.FindProperty("_visualAnchor").objectReferenceValue = view.transform;
            data.FindProperty("_material").objectReferenceValue = material;
            data.FindProperty("_meshes").objectReferenceValue = meshes;
            data.ApplyModifiedPropertiesWithoutUndo();
            view.AddComponent<HealPulse>();
            view.AddComponent<TrampleZone>();
            PrefabUtility.SaveAsPrefabAsset(view, root + "Prefabs/HealerCharacter.prefab");
            Object.DestroyImmediate(view);
        }
    }
}
