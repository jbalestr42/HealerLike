using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public partial class SpellLooks
    {
        // An authored row replaces the entire composition. Otherwise every buff contributes in declaration order.
        public List<EffectRecipe> Compose(EffectVocabulary vocabulary, ABuffHandlerFactory factory,
            GameObject source, GameObject target)
        {
            var recipes = new List<EffectRecipe>();
            if (factory != null && buffs != null && buffs.TryGetValue(factory, out SpellLook authored)
                && authored != null)
            {
                if (authored.recipe != null)
                {
                    EffectRecipe baked = authored.recipe.InstantiateRecipe();
                    if (baked != null) recipes.Add(baked);
                    return recipes;
                }
                EffectRecipe legacy = EffectComposer.Compose(vocabulary, authored.element, authored.family,
                    authored.tempo, EffectDerivation.Period(factory), 1, 0, 0);
                if (legacy != null) recipes.Add(legacy);
                return recipes;
            }
            EffectContext context = EffectContext.Default;
            context.origin = EffectDerivation.Origin(source);
            IReadOnlyList<EffectChannels> layers = EffectDerivation.Layers(factory, IsSameSide(source, target), context);
            if (layers.Count == 0)
            {
                EffectRecipe fallback = EffectComposer.Compose(vocabulary,
                    EffectDerivation.Channels(factory, IsSameSide(source, target), context), 1, 0);
                if (fallback != null) recipes.Add(fallback);
            }
            foreach (EffectChannels channels in layers)
            {
                EffectRecipe recipe = EffectComposer.Compose(vocabulary, channels, 1, 0);
                if (recipe != null) recipes.Add(recipe);
            }
            if (!EffectValidator.TryValidateComposition(recipes, out _)) recipes.Clear();
            return recipes;
        }
    }
}
