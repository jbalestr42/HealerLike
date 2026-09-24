using System.Collections.Generic;
using System.Globalization;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio
{
    // The part budget of a grammar preset and the notes that explain it. The composer drops fanned head copies to
    // fit the vocabulary's maxParts before it measures the accessory, and the check follows it the same way.
    public static class CreatureGrammarBudget
    {
        // Run on channels whose vocabulary entries all exist
        public static void Check(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
        {
            if (PartCount(vocabulary, channels, 1) > CreatureValidator.MaxParts)
            {
                errors.Add("Even one head copy exceeds the runtime limit of " + CreatureValidator.MaxParts + " parts.");
            }

            if (channels.accessory == AccessoryKind.None)
            {
                return;
            }

            float minimum = LookComposer.StoneAccessoryReach;
            if (channels.side == LookSide.Plant)
            {
                minimum = LookComposer.PlantAccessoryReach;
            }

            UnitChannels composed = channels;
            if (!vocabulary.heads[channels.head].carriesCount)
            {
                composed.count = FittedCount(vocabulary, channels);
            }

            if (LookMeasure.AccessoryReach(composed, vocabulary) < minimum)
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
                + ". The composer reduces multiple head copies if they exceed this budget.");
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

        // Five head copies fall to three, then one, until the parts fit maxParts
        static CountBand FittedCount(LookVocabulary vocabulary, UnitChannels channels)
        {
            int copies = LookComposer.Copies(channels.count);
            while (PartCount(vocabulary, channels, copies) > vocabulary.maxParts && copies > 1)
            {
                if (copies > 3)
                {
                    copies = 3;
                }
                else
                {
                    copies = 1;
                }
            }

            if (copies == 1)
            {
                return CountBand.One;
            }

            if (copies == 3)
            {
                return CountBand.Few;
            }
            return CountBand.Many;
        }

        // The parts the composer lays out: body, the stem or the two limbs, the head copies with their stalks,
        // the accessory and its mini head
        static int PartCount(LookVocabulary vocabulary, UnitChannels channels, int copies)
        {
            bool isPlant = channels.side == LookSide.Plant;
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            int count = Visible(CreatureGrammarValidator.Pick(body.plant, body.stone, isPlant), CountBand.One);
            if (isPlant)
            {
                count += 1;
            }
            else
            {
                count += 2;
            }

            LookPart[] headParts = CreatureGrammarValidator.Pick(head.plant, head.stone, isPlant);
            if (head.carriesCount)
            {
                count += Visible(headParts, channels.count);
            }
            else
            {
                count += Visible(headParts, CountBand.One) * copies;
                if (isPlant && copies > 1)
                {
                    count += copies;
                }
            }

            if (channels.accessory == AccessoryKind.None)
            {
                return count;
            }

            LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
            count += Visible(CreatureGrammarValidator.Pick(accessory.plant, accessory.stone, isPlant), CountBand.One);
            if (channels.accessory == AccessoryKind.MiniHead)
            {
                LookVocabulary.HeadEntry miniHead = vocabulary.heads[channels.accessoryHead];
                count += Visible(CreatureGrammarValidator.Pick(miniHead.plant, miniHead.stone, isPlant), CountBand.One);
            }
            return count;
        }

        static int Visible(LookPart[] parts, CountBand band)
        {
            int count = 0;
            foreach (LookPart part in parts)
            {
                if (part.minCount <= band)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
