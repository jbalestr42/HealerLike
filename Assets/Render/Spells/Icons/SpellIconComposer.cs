using System.Collections.Generic;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Icons use the same vocabulary and whole-recipe overrides as the world, with no second shape table.
    public static class SpellIconComposer
    {
        public static SpellIconRecipe Compose(object source, EffectVocabulary vocabulary, SpellLooks looks)
        {
            SpellIconDescription description = SpellIconDerivation.Read(source);
            if (description == null || vocabulary == null)
            {
                return null;
            }
            SpellIconRecipe icon = new SpellIconRecipe
            {
                reach = EffectDerivation.Reach(null, description.context.targetCount),
                origin = description.context.origin
            };
            foreach (EffectChannels channels in description.layers)
            {
                if (!Add(icon, EffectComposer.Compose(vocabulary, channels, 3, 3)))
                {
                    return null;
                }
            }
            foreach (ABuffHandlerFactory handler in description.handlers)
            {
                List<EffectRecipe> recipes = SpellLooks.ComposeHandler(vocabulary, looks, handler,
                    description.isSameSide, description.context, 3, 3);
                if (recipes.Count == 0)
                {
                    return null;
                }
                foreach (EffectRecipe recipe in recipes)
                {
                    if (!Add(icon, recipe))
                    {
                        return null;
                    }
                }
            }
            return EffectValidator.TryValidateComposition(icon.layers, out _) ? icon : null;
        }

        static bool Add(SpellIconRecipe icon, EffectRecipe recipe)
        {
            if (!EffectValidator.TryValidate(recipe, out _))
            {
                return false;
            }
            // Preserve socket relationships and channel additions inside each complete authored layer.
            icon.layers.Add(EffectRecipeCopy.Copy(recipe));
            foreach (EffectRecipe layer in SpellIconRecipe.Entries(recipe))
            {
                if (layer.channels.trigger != EffectTrigger.Cast)
                {
                    icon.trigger = layer.channels.trigger;
                }
            }
            return true;
        }
    }
}
