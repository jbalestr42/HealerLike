using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Creatures
{
    public static class CreatureAssetAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        static readonly string materialPath = "Assets/Render/Look/Look_Default.mat";
        static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

        [MenuItem("Tools/Render/Author Creature Assets")]
        public static void Author()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath);
            if (!material || !meshes)
            {
                Debug.LogError($"[CreatureAssetAuthoring] Missing {materialPath} or {meshesPath}.");
                return;
            }

            Directory.CreateDirectory(root + "Data");
            Directory.CreateDirectory(root + "Prefabs");
            CreatureRecipe healer = CreatureRecipeAuthoring.SaveRecipe("Healer", CreatureRecipeParts.Healer(), 13, 2, 17);
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
