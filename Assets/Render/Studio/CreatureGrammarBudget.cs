using System.Collections.Generic;
using System.Globalization;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio
{
    // The budget uses the requested count. Neither Studio nor the composer changes count to make a recipe fit.
    public static class CreatureGrammarBudget
    {
        // Run on channels whose vocabulary entries all exist
        public static void Check(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
        {
            PartList layout = LookComposer.Layout(channels, vocabulary);
            int parts = layout == null ? 0 : layout.count;
            int limit = System.Math.Min(vocabulary.maxParts, CreatureValidator.MaxParts);
            if (parts > limit)
            {
                errors.Add(channels.count + " requires " + parts + " parts, exceeding the budget of " + limit
                    + "; count is preserved.");
            }

            if (channels.accessory == AccessoryKind.None)
            {
                return;
            }

            float minimum = LookComposer.AccessoryClearance(channels.side, vocabulary);

            if (layout != null && LookMeasure.OutlineReach(layout, vocabulary.Unit(channels.side)) < minimum)
            {
                errors.Add("The accessory does not extend far enough beyond the body/head silhouette "
                    + "for the production grammar.");
            }
        }

        // Informational lines, none of them stops the composer
        public static string[] Notes(CreatureGrammarPreset preset)
        {
            List<string> notes = new List<string>();
            if (preset.deriveFromEntity)
            {
                notes.Add("Channels are read from the entity's actual skill, item and attribute data; "
                    + "bake or read into manual fields to detach.");
            }

            LookVocabulary vocabulary = preset.vocabulary;
            if (vocabulary == null)
            {
                return notes.ToArray();
            }

            notes.Add("Production maxParts: " + vocabulary.maxParts
                + ". Recipes that exceed this budget are rejected; the requested head count is preserved.");
            if (vocabulary.isReachPinned)
            {
                string reach = vocabulary.pinnedReach.ToString("0.###", CultureInfo.InvariantCulture);
                notes.Add("Reach is pinned by this vocabulary to " + reach
                    + " body units; changing Reach does not change root extent.");
            }

            if (preset.Channels().side == LookSide.Stone)
            {
                notes.Add("Stone recipes stand on limbs; they have no plant roots or liana arms.");
            }
            return notes.ToArray();
        }

    }
}
