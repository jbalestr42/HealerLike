using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Creatures
{
    public static class HLCreatureAssetAuthoring
    {
        static readonly string root = "Assets/Render/Creatures/";
        static readonly string materialPath = "Assets/Render/Look/HLLook_Default.mat";
        static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

        [MenuItem("Tools/Render/Author Creature Assets")]
        public static void Author()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            HLPrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<HLPrimitiveMeshes>(meshesPath);
            if (!material || !meshes)
            {
                Debug.LogError($"[HLCreatureAssetAuthoring] Missing {materialPath} or {meshesPath}.");
                return;
            }

            Directory.CreateDirectory(root + "Data");
            Directory.CreateDirectory(root + "Prefabs");
            HLCreatureRecipe healer = CreatureRecipeAuthoring.SaveRecipe("HLHealer", CreatureRecipeParts.Healer(), 6, 2, 17);
            HLCreatureRecipe fern = CreatureRecipeAuthoring.SaveRecipe("HLSpiralFern", CreatureRecipeParts.Fern(), 7, 2, 31);
            HLCreatureRecipe arch = CreatureRecipeAuthoring.SaveRecipe("HLHangingArch", CreatureRecipeParts.Arch(), 6, 4, 57);
            HLCreatureRecipe rosette = CreatureRecipeAuthoring.SaveRecipe("HLBladeRosette", CreatureRecipeParts.Rosette(), 6, 2, 103);
            HLCreatureRecipe stack = CreatureRecipeAuthoring.SaveRecipe("HLSphereStack", CreatureRecipeParts.Stack(), 8, 1, 89);
            if (!healer || !fern || !arch || !rosette || !stack)
            {
                return;
            }

            // Views are instantiated under his models, which keep their own sockets and colliders
            View("HLNormal", fern, material, meshes);
            View("HLTest", stack, material, meshes);
            View("HLSwarm", arch, material, meshes);
            View("HLFastShoot", fern, material, meshes);
            View("HLTripleShoot", arch, material, meshes);
            View("HLMultiShot", arch, material, meshes);
            View("HLRandomShoot", fern, material, meshes);
            View("HLChainLightning", stack, material, meshes);
            View("HLChanneling", stack, material, meshes);
            View("HLSoldier", stack, material, meshes);
            View("HLHitArmorBuffer", rosette, material, meshes);
            CharacterView(healer, material, meshes);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HLCreatureAssetAuthoring] Creature recipes and view prefabs authored.");
        }

        // Recipe-only refresh keeps existing prefab presentation and delivery overrides intact
        public static void AuthorBeautyRecipes()
        {
            CreatureRecipeAuthoring.SaveRecipe("HLHealer", CreatureRecipeParts.Healer(), 6, 2, 17);
            CreatureRecipeAuthoring.SaveRecipe("HLSpiralFern", CreatureRecipeParts.Fern(), 7, 2, 31);
            CreatureRecipeAuthoring.SaveRecipe("HLHangingArch", CreatureRecipeParts.Arch(), 6, 4, 57);
            CreatureRecipeAuthoring.SaveRecipe("HLBladeRosette", CreatureRecipeParts.Rosette(), 6, 2, 103);
            CreatureRecipeAuthoring.SaveRecipe("HLSphereStack", CreatureRecipeParts.Stack(), 8, 1, 89);
            AssetDatabase.SaveAssets();
        }

        // The view sits under his model, which carries the model scale, so the root stays at scale one
        static void View(string name, HLCreatureRecipe recipe, Material material, HLPrimitiveMeshes meshes)
        {
            GameObject view = new GameObject(name);
            view.AddComponent<HLCreatureBuilder>().SetRecipe(recipe, material, meshes);
            view.AddComponent<HLStatusObserver>();
            view.AddComponent<HLHealPulse>();
            view.AddComponent<HLTrampleZone>();
            view.AddComponent<HLRangePreview>();
            PrefabUtility.SaveAsPrefabAsset(view, root + "Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(view);
        }

        static void CharacterView(HLCreatureRecipe recipe, Material material, HLPrimitiveMeshes meshes)
        {
            GameObject view = new GameObject("HLHealerCharacter");
            HLCharacterView characterView = view.AddComponent<HLCharacterView>();
            SerializedObject data = new SerializedObject(characterView);
            data.FindProperty("_recipe").objectReferenceValue = recipe;
            data.FindProperty("_visualAnchor").objectReferenceValue = view.transform;
            data.FindProperty("_material").objectReferenceValue = material;
            data.FindProperty("_meshes").objectReferenceValue = meshes;
            data.ApplyModifiedPropertiesWithoutUndo();
            view.AddComponent<HLHealPulse>();
            view.AddComponent<HLTrampleZone>();
            PrefabUtility.SaveAsPrefabAsset(view, root + "Prefabs/HLHealerCharacter.prefab");
            Object.DestroyImmediate(view);
        }
    }
}
