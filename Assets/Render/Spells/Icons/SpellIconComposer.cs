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
                if (looks != null && looks.buffs != null && looks.buffs.TryGetValue(handler, out SpellLook authored)
                    && authored != null)
                {
                    EffectRecipe recipe = authored.recipe != null ? authored.recipe.InstantiateRecipe()
                        : EffectComposer.Compose(vocabulary, authored.element, authored.family, authored.tempo,
                            EffectDerivation.Period(handler), 3, 3, .5f);
                    if (!Add(icon, recipe))
                    {
                        return null;
                    }
                    continue;
                }
                IReadOnlyList<EffectChannels> layers = EffectDerivation.Layers(handler, description.isSameSide,
                    description.context);
                if (layers.Count == 0)
                {
                    if (!Add(icon, EffectComposer.Compose(vocabulary, EffectDerivation.Channels(handler,
                        description.isSameSide, description.context), 3, 3)))
                    {
                        return null;
                    }
                }
                foreach (EffectChannels channels in layers)
                {
                    if (!Add(icon, EffectComposer.Compose(vocabulary, channels, 3, 3)))
                    {
                        return null;
                    }
                }
            }
            int parts = 0;
            foreach (EffectRecipe layer in icon.layers)
            {
                parts += layer.entry.parts.Length + (layer.entry.stackBeads?.Length ?? 0)
                    + (layer.entry.sideRim?.Length ?? 0) + (layer.entry.criticalRings?.Length ?? 0);
                if (layer.channels.trigger != EffectTrigger.Cast)
                {
                    icon.trigger = layer.channels.trigger;
                }
            }
            return icon.layers.Count > 0 && parts <= EffectValidator.MaxParts ? icon : null;
        }

        static bool Add(SpellIconRecipe icon, EffectRecipe recipe)
        {
            if (!EffectValidator.TryValidate(recipe, out _))
            {
                return false;
            }
            // Own snapshots: posing an icon must never alter an asset or a live status.
            Stack<EffectRecipe> pending = new Stack<EffectRecipe>();
            pending.Push(EffectRecipeCopy.Copy(recipe));
            while (pending.Count > 0)
            {
                EffectRecipe layer = pending.Pop();
                if (layer.additions != null)
                {
                    for (int i = layer.additions.Length - 1; i >= 0; i--)
                    {
                        pending.Push(layer.additions[i]);
                    }
                }
                layer.additions = System.Array.Empty<EffectRecipe>();
                icon.layers.Add(layer);
            }
            return true;
        }
    }
}
