using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    public static class SpellRecipeBaking
    {
        [MenuItem("Tools/Render/Bake Selected Spell Recipe")]
        public static void BakeSelected()
        {
            SpellStudioPreset preset = Selection.activeObject as SpellStudioPreset;
            if (!preset) { Debug.LogError("Select a SpellStudioPreset to bake."); return; }
            EffectRecipe recipe = preset.Compose();
            if (!EffectValidator.TryValidate(recipe, out string error))
            { Debug.LogError(error); return; }
            string path = EditorUtility.SaveFilePanelInProject("Bake spell recipe", preset.name + "Recipe",
                "asset", "Save the whole authored composition", "Assets/Render/Spells/Data");
            if (string.IsNullOrEmpty(path)) return;
            EffectRecipeAsset asset = ScriptableObject.CreateInstance<EffectRecipeAsset>();
            asset.recipe = EffectRecipeCopy.Copy(recipe);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }
    }
}
