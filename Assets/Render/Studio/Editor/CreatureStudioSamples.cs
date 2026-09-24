using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    public static class CreatureStudioSamples
    {
        public const string Folder = "Assets/Render/Studio/Data/Samples";

        [MenuItem("Tools/Render/Creature Studio Samples")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Render/Studio/Data", "Samples");
            foreach (string name in CreatureStudioAuthoring.SampleNames)
            {
                string path = Folder + "/" + name + ".asset";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) continue;
                var recipe = CreatureStudioAuthoring.BuildSample(System.Array.IndexOf(CreatureStudioAuthoring.SampleNames, name));
                if (recipe == null) continue;
                recipe.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(recipe, path);
                AssetDatabase.SaveAssetIfDirty(recipe);
            }
            Debug.Log("[Creature Studio] Starter recipes are ready in " + Folder);
        }
    }
}
