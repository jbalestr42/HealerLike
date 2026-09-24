using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // What leaves the studio on purpose: a shape handed to the shared vocabulary, a preset written as an asset
    public static class SpellStudioPublishing
    {
        // Replaces the vocabulary's entry for the preset's element with a copy of the preset's composed shape
        public static bool PublishEntry(SpellStudioPreset preset)
        {
            if (preset == null || preset.vocabulary == null || preset.vocabulary.elements == null)
            {
                return false;
            }

            EffectRecipe recipe = preset.Compose();
            if (recipe == null || recipe.entry == null)
            {
                return false;
            }

            // Odin serializes the dictionary into backing data, so only a complete snapshot can undo it
            Undo.RegisterCompleteObjectUndo(preset.vocabulary, "Apply Spell Studio shape");
            preset.vocabulary.elements[recipe.element] = SpellPresetBounds.CloneEntry(recipe.entry);
            EditorUtility.SetDirty(preset.vocabulary);
            AssetDatabase.SaveAssetIfDirty(preset.vocabulary);
            return true;
        }

        // A copy of the preset saved at path; the other dirty assets of the project stay unsaved
        public static SpellStudioPreset SaveCopy(SpellStudioPreset source, string path)
        {
            SpellStudioPreset copy = Object.Instantiate(source);
            copy.hideFlags = HideFlags.None;
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, path);
            if (!AssetDatabase.Contains(copy))
            {
                Debug.LogError("[SpellStudioPublishing] Could not save the preset at " + path);
                Object.DestroyImmediate(copy);
                return null;
            }

            AssetDatabase.SaveAssetIfDirty(copy);
            return copy;
        }
    }
}
