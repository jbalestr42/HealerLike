using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // Writes the creature studio's starter recipes as assets, leaving the ones already there as the author left them
    public static class CreatureStudioSamples
    {
        public static readonly string Folder = "Assets/Render/Studio/Data/Samples";

        [MenuItem("Tools/Render/Creature Studio Samples")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Render/Studio/Data", "Samples");
            }

            for (int i = 0; i < CreatureStudioAuthoring.SampleNames.Length; i++)
            {
                string path = Folder + "/" + CreatureStudioAuthoring.SampleNames[i] + ".asset";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                {
                    continue;
                }

                CreatureRecipe recipe = CreatureStudioAuthoring.BuildSample(i);
                if (recipe == null)
                {
                    continue;
                }

                recipe.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(recipe, path);
                AssetDatabase.SaveAssetIfDirty(recipe);
            }
        }
    }
}
