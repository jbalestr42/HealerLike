using UnityEditor;

namespace HealerLike.Render.Spells.Studio.Editor
{
    /// <summary>Explicitly transfers a studio shape into the vocabulary used by the renderer.</summary>
    public static class SpellStudioPublishing
    {
        public static bool PublishEntry(SpellStudioPreset preset)
        {
            if (preset == null || preset.vocabulary == null || preset.vocabulary.elements == null)
                return false;

            EffectRecipe recipe = preset.Compose();
            if (recipe == null || recipe.entry == null)
                return false;

            // Odin serializes the vocabulary dictionary. A complete snapshot includes its backing data.
            Undo.RegisterCompleteObjectUndo(preset.vocabulary, "Apply Spell Studio shape");
            preset.vocabulary.elements[recipe.element] = SpellStudioPreset.CloneEntry(recipe.entry);
            EditorUtility.SetDirty(preset.vocabulary);
            AssetDatabase.SaveAssetIfDirty(preset.vocabulary);
            return true;
        }
    }
}
