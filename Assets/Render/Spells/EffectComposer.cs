using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Zones;

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
        // The element's size on its socket, a harder hit draws a bigger burst
        public float scale = 1f;
        public LookPalette palette;
    }

    // Turns the channels of a handler into one element of the vocabulary, sized by its stacks, charges or amount
    public static class EffectComposer
    {
        // Heal spheres go from three to eight as the amount goes from nothing to this share of the maximum health
        public static readonly float FullAmount = 0.5f;
        // The burst's size from the smallest hit to a hit of the whole health
        public static readonly float BurstScaleMin = 0.8f;
        public static readonly float BurstScaleMax = 1.6f;

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

        // A resolved change on a unit: a heal rises, a hit bursts, mana goes up or down; amount is the share of
        // the maximum, it sizes the heal and the burst
        public static EffectRecipe Impact(EffectVocabulary vocabulary, ResourceKind resource, bool isGain, float amount)
        {
            EffectFamily family = EffectFamily.Damage;
            EffectElement element = EffectElement.Burst;
            if (isGain)
            {
                family = EffectFamily.Heal;
                element = EffectElement.Rise;
            }

            if (resource == ResourceKind.Mana)
            {
                element = Mana(isGain);
            }

            EffectRecipe recipe = Compose(vocabulary, element, family, EffectTempo.Once, 0f, 1, 0f, amount);
            if (recipe != null && element == EffectElement.Burst)
            {
                recipe.scale = Mathf.Lerp(BurstScaleMin, BurstScaleMax, Mathf.Sqrt(Mathf.Clamp01(amount)));
            }

            return recipe;
        }

        // An area's footprint: a heal ring, or the bane litter for anything hostile; the entry's cycle is the pulse
        public static EffectRecipe Area(EffectVocabulary vocabulary, ZoneKind kind)
        {
            if (kind == ZoneKind.Hostile)
            {
                return Compose(vocabulary, EffectElement.Litter, EffectFamily.Bane, EffectTempo.Once, 0f, 1, 0f, 0f);
            }

            return Compose(vocabulary, EffectElement.Ring, EffectFamily.Heal, EffectTempo.Once, 0f, 1, 0f, 0f);
        }

        // A beam in the family's accent, lime for a heal so gold stays with Boon
        public static EffectRecipe Link(EffectVocabulary vocabulary, EffectFamily family)
        {
            return Compose(vocabulary, EffectElement.Beam, family, EffectTempo.Once, 0f, 1, 0f, 0f);
        }

        // HitArmor charges draw as the Boon defence plates, one plate per charge
        public static EffectRecipe Shield(EffectVocabulary vocabulary, float charges)
        {
            return Compose(vocabulary, EffectElement.Plates, EffectFamily.Boon, EffectTempo.ForDuration, 0f, 1, charges,
                           0f);
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
                Debug.LogError("[EffectComposer] No palette.");
                return Color.magenta;
            }

            if (element == EffectElement.ManaUp || element == EffectElement.ManaDown)
            {
                return palette.Colour(ColourRole.Mana, family);
            }
            return palette.Colour(ColourRole.Accent, family);
        }
    }
}
