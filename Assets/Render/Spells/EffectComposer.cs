using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Everything a SpellEffect needs to build and move one element
    public class EffectRecipe
    {
        public EffectElement element;
        public ElementEntry entry;
        public EffectMotion motion;
        public EffectSocket socket;
        public EffectFamily family;
        public EffectTempo tempo;
        // One motion cycle: the period of a ticking handler, else the entry's own cycle
        public float cycleSeconds;
        public Color colour;
        public int count;
        public LookPalette palette;
    }

    // Turns the channels of a handler into one element of the vocabulary, sized by its stacks, charges or amount
    public static class EffectComposer
    {
        // Heal spheres go from three to eight as the amount goes from nothing to this share of the maximum health
        public static readonly float FullAmount = 0.5f;

        public static EffectElement Element(EffectChannels channels)
        {
            switch (channels.family)
            {
                case EffectFamily.Damage:
                    return EffectElement.Burst;
                case EffectFamily.Heal:
                    return EffectElement.Rise;
                case EffectFamily.Rot:
                    return EffectElement.Drips;
                case EffectFamily.Renew:
                    return EffectElement.Stalks;
                case EffectFamily.Boon:
                    if (channels.group == AttributeGroup.Defence)
                    {
                        return EffectElement.Plates;
                    }

                    if (channels.group == AttributeGroup.Prevention)
                    {
                        return EffectElement.Bud;
                    }
                    return EffectElement.Orbit;
                default:
                    if (channels.group == AttributeGroup.Offence)
                    {
                        return EffectElement.Press;
                    }
                    return EffectElement.Crack;
            }
        }

        // Mana draws with its own pair of elements, up for a gain and down for a loss
        public static EffectElement Mana(bool isGain)
        {
            return isGain ? EffectElement.ManaUp : EffectElement.ManaDown;
        }

        public static EffectRecipe Compose(EffectVocabulary vocabulary, EffectChannels channels, int stacks,
                                           float charges)
        {
            return Compose(vocabulary, Element(channels), channels.family, channels.tempo, channels.periodSeconds,
                           stacks, charges, 0f);
        }

        public static EffectRecipe Compose(EffectVocabulary vocabulary, EffectElement element, EffectFamily family,
                                           EffectTempo tempo, float periodSeconds, int stacks, float charges,
                                           float amount)
        {
            if (vocabulary == null)
            {
                return null;
            }

            ElementEntry entry = vocabulary.GetEntry(element);
            if (entry == null)
            {
                return null;
            }

            EffectRecipe recipe = new EffectRecipe();
            recipe.element = element;
            recipe.entry = entry;
            recipe.motion = entry.motion;
            recipe.socket = entry.socket;
            recipe.family = family;
            recipe.tempo = tempo;
            recipe.cycleSeconds = entry.cycleSeconds;
            if (tempo == EffectTempo.PerPeriod && float.IsFinite(periodSeconds) && periodSeconds > 0f)
            {
                recipe.cycleSeconds = periodSeconds;
            }

            recipe.palette = vocabulary.palette;
            recipe.colour = Colour(vocabulary.palette, element, family);
            recipe.count = Count(entry, stacks, charges, amount);
            return recipe;
        }

        // The entry's minimum, plus one shape part per stack, charge or step of amount past the first
        public static int Count(ElementEntry entry, int stacks, float charges, float amount)
        {
            int shapes = Shapes(entry);
            if (entry.count == EffectCount.Fixed)
            {
                return shapes;
            }

            int value = Mathf.Max(1, stacks);
            if (entry.count == EffectCount.Charges && charges > 0f)
            {
                value = Mathf.CeilToInt(charges);
            }
            else if (entry.count == EffectCount.Amount)
            {
                float share = float.IsFinite(amount) ? Mathf.Clamp01(Mathf.Abs(amount) / FullAmount) : 0f;
                value = 1 + Mathf.RoundToInt(share * (shapes - entry.minCount));
            }
            return Mathf.Clamp(entry.minCount + value - 1, Mathf.Min(entry.minCount, shapes), shapes);
        }

        public static int Shapes(ElementEntry entry)
        {
            int shapes = 0;
            foreach (LookPart part in entry.parts)
            {
                if (part.role != PartRole.Stem)
                {
                    shapes++;
                }
            }
            return shapes;
        }

        public static Color Colour(LookPalette palette, EffectElement element, EffectFamily family)
        {
            if (palette == null)
            {
                return Color.white;
            }

            if (element == EffectElement.ManaUp || element == EffectElement.ManaDown)
            {
                return palette.mana;
            }
            return palette.Accent(family);
        }
    }
}
