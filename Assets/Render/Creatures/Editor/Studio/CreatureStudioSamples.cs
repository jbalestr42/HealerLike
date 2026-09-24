using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures.Editor.Studio
{
    public static class CreatureStudioSamples
    {
        public const string Folder = "Assets/Render/Creatures/Data/StudioSamples";

        [MenuItem("Tools/Render/Creature Studio Samples")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Render/Creatures/Data", "StudioSamples");
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
