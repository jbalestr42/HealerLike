using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public partial class SpellLooks
    {
        public List<EffectRecipe> Compose(EffectVocabulary vocabulary, ABuffHandlerFactory factory,
            GameObject source, GameObject target)
        {
            EffectContext context = EffectDerivation.Context(source, target);
            return ComposeHandler(vocabulary, this, factory, EffectDerivation.IsSameSide(source, target), context);
        }

        // One resolution path for world effects, previews and icons. A bad layer rejects the whole handler.
        public static List<EffectRecipe> ComposeHandler(EffectVocabulary vocabulary, SpellLooks looks,
            ABuffHandlerFactory factory, bool isSameSide, EffectContext context, int stacks = 1, float charges = 0)
        {
            List<EffectRecipe> recipes = new List<EffectRecipe>();
            if (factory != null && looks != null && looks.buffs != null
                && looks.buffs.TryGetValue(factory, out SpellLook authored) && authored != null)
            {
                EffectRecipe recipe = authored.recipe != null ? authored.recipe.InstantiateRecipe()
                    : EffectComposer.Compose(vocabulary, authored.element, authored.family, authored.tempo,
                        EffectDerivation.Period(factory), stacks, charges, 0);
                if (recipe != null)
                {
                    recipes.Add(recipe);
                }
            }
            else
            {
                IReadOnlyList<EffectChannels> layers = EffectDerivation.Layers(factory, isSameSide, context);
                if (layers.Count == 0)
                {
                    layers = new[] { EffectDerivation.Channels(factory, isSameSide, context) };
                }
                foreach (EffectChannels channels in layers)
                {
                    EffectRecipe recipe = EffectComposer.Compose(vocabulary, channels, stacks, charges);
                    if (recipe == null)
                    {
                        recipes.Clear();
                        return recipes;
                    }
                    recipes.Add(recipe);
                }
            }
            if (!EffectValidator.TryValidateComposition(recipes, out _))
            {
                recipes.Clear();
            }
            return recipes;
        }
    }
}
