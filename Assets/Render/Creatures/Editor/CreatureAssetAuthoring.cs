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

        [MenuItem("Tools/Render/Author Creature Assets")]
        public static void Author()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath);
            if (!material || !meshes)
            {
                Debug.LogError($"[HLCreatureAssetAuthoring] Missing {materialPath} or {meshesPath}.");
                return;
            }

            Directory.CreateDirectory(root + "Data");
            Directory.CreateDirectory(root + "Prefabs");
            CreatureRecipe healer = CreatureRecipeAuthoring.SaveRecipe("Healer", CreatureRecipeParts.Healer(), 6, 2, 17);
            CreatureRecipe fern = CreatureRecipeAuthoring.SaveRecipe("SpiralFern", CreatureRecipeParts.Fern(), 7, 2, 31);
            CreatureRecipe arch = CreatureRecipeAuthoring.SaveRecipe("HangingArch", CreatureRecipeParts.Arch(), 6, 4, 57);
            CreatureRecipe rosette = CreatureRecipeAuthoring.SaveRecipe("BladeRosette", CreatureRecipeParts.Rosette(), 6, 2, 103);
            CreatureRecipe stack = CreatureRecipeAuthoring.SaveRecipe("SphereStack", CreatureRecipeParts.Stack(), 8, 1, 89);
            if (!healer || !fern || !arch || !rosette || !stack)
            {
                return;
            }

            // Views are instantiated under his models, which keep their own sockets and colliders
            View("Normal", fern, material, meshes);
            View("Test", stack, material, meshes);
            View("Swarm", arch, material, meshes);
            View("FastShoot", fern, material, meshes);
            View("TripleShoot", arch, material, meshes);
            View("MultiShot", arch, material, meshes);
            View("RandomShoot", fern, material, meshes);
            View("ChainLightning", stack, material, meshes);
            View("Channeling", stack, material, meshes);
            View("Soldier", stack, material, meshes);
            View("HitArmorBuffer", rosette, material, meshes);
            CharacterView(healer, material, meshes);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HLCreatureAssetAuthoring] Creature recipes and view prefabs authored.");
        }

        // Recipe-only refresh keeps existing prefab presentation and delivery overrides intact
        public static void AuthorBeautyRecipes()
        {
            CreatureRecipeAuthoring.SaveRecipe("Healer", CreatureRecipeParts.Healer(), 6, 2, 17);
            CreatureRecipeAuthoring.SaveRecipe("SpiralFern", CreatureRecipeParts.Fern(), 7, 2, 31);
            CreatureRecipeAuthoring.SaveRecipe("HangingArch", CreatureRecipeParts.Arch(), 6, 4, 57);
            CreatureRecipeAuthoring.SaveRecipe("BladeRosette", CreatureRecipeParts.Rosette(), 6, 2, 103);
            CreatureRecipeAuthoring.SaveRecipe("SphereStack", CreatureRecipeParts.Stack(), 8, 1, 89);
            AssetDatabase.SaveAssets();
        }

        // The view sits under his model, which carries the model scale, so the root stays at scale one
        static void View(string name, CreatureRecipe recipe, Material material, PrimitiveMeshes meshes)
        {
            GameObject view = new GameObject(name);
            view.AddComponent<CreatureBuilder>().SetRecipe(recipe, material, meshes);
            view.AddComponent<StatusObserver>();
            view.AddComponent<HealPulse>();
            view.AddComponent<TrampleZone>();
            view.AddComponent<RangePreview>();
            PrefabUtility.SaveAsPrefabAsset(view, root + "Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(view);
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
