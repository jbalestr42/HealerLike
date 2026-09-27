using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Spells
{
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
            return Element(null, channels);
        }

        public static EffectElement Element(EffectVocabulary vocabulary, EffectChannels channels)
        {
            EffectOperation operation = channels.operation;
            EffectAspect aspect = channels.aspect;
            if (operation == EffectOperation.Damage && channels.family != EffectFamily.Damage) operation = ToOperation(channels.family);
            if (channels.group != AttributeGroup.Offence) aspect = (EffectAspect)channels.group;
            EffectTempo tempo = channels.family == EffectFamily.Rot || channels.family == EffectFamily.Renew
                ? EffectTempo.PerPeriod : channels.tempo;
            EffectElement element;
            if (vocabulary != null && vocabulary.TryGetElement(operation, aspect, tempo, out element)) return element;
            if (vocabulary != null) return default(EffectElement);
            return LegacyElement(operation, aspect, tempo);
        }

        static EffectOperation ToOperation(EffectFamily family)
        {
            switch (family)
            {
                case EffectFamily.Heal:
                case EffectFamily.Renew: return EffectOperation.Heal;
                case EffectFamily.Boon: return EffectOperation.Boon;
                case EffectFamily.Bane: return EffectOperation.Bane;
                default: return EffectOperation.Damage;
            }
        }

        static EffectElement LegacyElement(EffectOperation operation, EffectAspect aspect, EffectTempo tempo)
        {
            if (tempo == EffectTempo.PerPeriod)
            {
                if (operation == EffectOperation.Damage) return EffectElement.Drips;
                if (operation == EffectOperation.Heal) return EffectElement.Stalks;
            }
            if (operation == EffectOperation.Ward) return EffectElement.Plates;
            if (operation == EffectOperation.Mana) return EffectElement.ManaUp;
            return operation == EffectOperation.Damage ? EffectElement.Burst
                : operation == EffectOperation.Heal ? EffectElement.Rise
                : operation == EffectOperation.Boon ? (aspect == EffectAspect.Defence ? EffectElement.Plates
                    : aspect == EffectAspect.Prevention ? EffectElement.Bud : EffectElement.Orbit)
                : aspect == EffectAspect.Offence ? EffectElement.Press : EffectElement.Crack;
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
            if (recipe != null && recipe.presentation != null && recipe.presentation.scalesWithAmount)
            {
                recipe.scale *= Mathf.Lerp(BurstScaleMin, BurstScaleMax, Mathf.Sqrt(Mathf.Clamp01(amount)));
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
            if (vocabulary == null) return null;
            if (channels.operation == EffectOperation.Damage && channels.family != EffectFamily.Damage)
                channels.operation = ToOperation(channels.family);
            if (channels.aspect == EffectAspect.Offence && channels.group != AttributeGroup.Offence)
                channels.aspect = (EffectAspect)channels.group;
            EffectFamily family = channels.family;
            EffectRecipe recipe = Compose(vocabulary, Element(vocabulary, channels), family, channels.tempo, channels.periodSeconds,
                           stacks, charges, 0f);
            if (!EffectCompositionValidator.TryValidate(channels, vocabulary, out string compositionError))
            {
                Debug.LogError("[EffectComposer] " + compositionError);
                return null;
            }
            if (!EffectValidator.TryValidate(recipe, out string recipeError))
            {
                Debug.LogError("[EffectComposer] " + recipeError);
                return null;
            }
            recipe.channels = channels;
            if (recipe.presentation != null && recipe.presentation.enabled)
                recipe.scale *= vocabulary.MagnitudeScale(channels.magnitude);
            return recipe;
        }

        public static EffectRecipe Compose(EffectVocabulary vocabulary, EffectElement element, EffectFamily family,
                                           EffectTempo tempo, float periodSeconds, int stacks, float charges,
                                           float amount, ElementEntry entryOverride = null,
                                           bool useColourOverride = false, Color colourOverride = default(Color))
        {
            if (vocabulary == null && entryOverride == null)
            {
                return null;
            }

            ElementEntry entry = entryOverride != null ? entryOverride : vocabulary.GetEntry(element);
            if (!EffectValidator.TryValidateEntry(entry, out string error))
            {
                Debug.LogError("[EffectComposer] " + error);
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

            recipe.scale = entry.presentation != null ? entry.presentation.scale : 1f;
            recipe.palette = vocabulary != null ? vocabulary.palette : null;
            recipe.colour = useColourOverride ? colourOverride
                : entry.presentation != null && entry.presentation.enabled && recipe.palette != null
                    ? recipe.palette.Colour(entry.presentation.colourRole, family)
                    : Colour(recipe.palette, element, family);
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
