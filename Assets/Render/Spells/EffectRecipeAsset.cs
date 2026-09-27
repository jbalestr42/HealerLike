using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace HealerLike.Render.Spells
{
    // A whole authored composition. Runtime always takes an owned snapshot before changing live counts.
    [CreateAssetMenu(menuName = "Custom/Data/Render/Spell Recipe")]
    public class EffectRecipeAsset : SerializedScriptableObject
    {
        public EffectRecipe recipe;
        public EffectRecipe InstantiateRecipe() => EffectValidator.TryValidate(recipe, out _)
            ? EffectRecipeCopy.Copy(recipe) : null;
    }

    public static class EffectRecipeCopy
    {
        public static EffectRecipe Copy(EffectRecipe source)
        {
            if (source == null) return null;
            return new EffectRecipe { element = source.element, entry = Entry(source.entry),
                channels = source.channels, motion = source.motion, socket = source.socket,
                family = source.family, tempo = source.tempo, cycleSeconds = source.cycleSeconds,
                colour = source.colour, count = source.count, scale = source.scale, palette = source.palette,
                additions = Array.ConvertAll(source.additions ?? Array.Empty<EffectRecipe>(), Copy) };
        }
        public static ElementEntry Entry(ElementEntry source)
        {
            if (source == null) return null;
            return new ElementEntry { parts = Parts(source.parts), stackBeads = Parts(source.stackBeads),
                criticalRings = Parts(source.criticalRings), sideRim = Parts(source.sideRim),
                motion = source.motion, socket = source.socket, count = source.count,
                minCount = source.minCount, cycleSeconds = source.cycleSeconds,
                presentation = source.presentation?.Clone(), ground = source.ground?.Clone(),
                groundRadius = source.groundRadius, groundStrength = source.groundStrength };
        }
        static HealerLike.Render.Creatures.LookPart[] Parts(HealerLike.Render.Creatures.LookPart[] parts)
            => parts == null ? Array.Empty<HealerLike.Render.Creatures.LookPart>()
                : (HealerLike.Render.Creatures.LookPart[])parts.Clone();
    }
}
