using System.Collections.Generic;
using System;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Runtime and Studio share the selected-unit contract. Callers own logging and display of diagnostics.
    public static class CreatureCompositionValidator
    {
        public static bool TryValidate(UnitChannels channels, LookVocabulary vocabulary, out string error)
        {
            List<string> errors = new List<string>();
            Check(channels, vocabulary, errors);
            error = errors.Count == 0 ? null : errors[0];
            return errors.Count == 0;
        }

        public static void Check(UnitChannels channels, LookVocabulary vocabulary, List<string> errors)
        {
            int start = errors.Count;
            if (!AreDefined(channels))
            {
                errors.Add("Choose valid grammar channel values.");
            }

            if (vocabulary == null)
            {
                errors.Add("Choose a look vocabulary.");
                return;
            }

            CheckTables(vocabulary, channels, errors);
            if (errors.Count != start)
            {
                return;
            }

            CreatureFragments.Check(vocabulary, channels, errors);
            if (channels.side == LookSide.Plant)
            {
                CreatureRootSettings.Check(vocabulary, channels, errors);
            }

            CheckPalette(channels, vocabulary, errors);
        }

        static void CheckPalette(UnitChannels channels, LookVocabulary vocabulary, List<string> errors)
        {
            bool isPlant = channels.side == LookSide.Plant;
            HashSet<ColourRole> roles = new HashSet<ColourRole> { ColourRole.Stem, ColourRole.Wilt, ColourRole.Ochre };
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            AddColours(roles, CreatureFragments.Pick(body.plant, body.stone, isPlant), CountBand.One);
            AddColours(
                roles,
                CreatureFragments.Pick(head.plant, head.stone, isPlant),
                head.carriesCount ? channels.count : CountBand.One
            );
            if (isPlant && vocabulary.armCount > 0)
            {
                roles.Add(ColourRole.Accent);
            }

            if (channels.accessory != AccessoryKind.None)
            {
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                AddColours(roles, CreatureFragments.Pick(accessory.plant, accessory.stone, isPlant), CountBand.One);
            }

            if (channels.accessory == AccessoryKind.MiniHead)
            {
                LookVocabulary.HeadEntry mini = vocabulary.heads[channels.accessoryHead];
                AddColours(roles, CreatureFragments.Pick(mini.plant, mini.stone, isPlant), CountBand.One);
            }

            foreach (ColourRole role in roles)
            {
                if (!RenderMath.IsFinite(vocabulary.Colour(role, channels.accent, channels.side)))
                {
                    errors.Add("Palette colours must be finite.");
                    return;
                }
            }
        }

        static void AddColours(HashSet<ColourRole> roles, LookPart[] parts, CountBand count)
        {
            if (parts == null)
            {
                return;
            }

            foreach (LookPart part in parts)
            {
                if (part.minCount <= count)
                {
                    roles.Add(part.colour);
                }
            }
        }

        // Every entry the channels select is present
        static void CheckTables(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
        {
            if (vocabulary.palette == null)
            {
                errors.Add("The vocabulary needs a palette.");
            }

            if (
                vocabulary.bodies == null
                || !vocabulary.bodies.ContainsKey(channels.mass)
                || vocabulary.bodies[channels.mass] == null
            )
            {
                errors.Add("The vocabulary is missing the selected mass/body entry.");
            }

            if (!HasHead(vocabulary, channels.head))
            {
                errors.Add("The vocabulary is missing the selected head entry.");
            }

            if (
                vocabulary.stems == null
                || !vocabulary.stems.ContainsKey(channels.stem)
                || vocabulary.stems[channels.stem] == null
            )
            {
                errors.Add("The vocabulary is missing the selected stem entry.");
            }

            if (channels.side == LookSide.Plant && vocabulary.roots == null)
            {
                errors.Add("The vocabulary root table is missing.");
            }

            bool hasAccessory = channels.accessory != AccessoryKind.None;
            if (
                hasAccessory
                && (
                    vocabulary.accessories == null
                    || !vocabulary.accessories.ContainsKey(channels.accessory)
                    || vocabulary.accessories[channels.accessory] == null
                )
            )
            {
                errors.Add("The vocabulary is missing the selected accessory entry.");
            }

            if (channels.accessory == AccessoryKind.MiniHead && !HasHead(vocabulary, channels.accessoryHead))
            {
                errors.Add("The vocabulary is missing the selected miniature head entry.");
            }

            float sideScale = vocabulary.stoneScale;
            if (channels.side == LookSide.Plant)
            {
                sideScale = vocabulary.plantScale;
            }

            if (!RenderMath.IsPositive(vocabulary.bodyUnit) || !RenderMath.IsPositive(sideScale))
            {
                errors.Add("Body unit and the selected side scale must be finite and positive.");
            }

            if (!vocabulary.layoutSettings.IsValid(channels.side))
            {
                errors.Add("Invalid layout, shape profile or selected band dimensions.");
            }

            if (vocabulary.maxParts < 1 || vocabulary.maxParts > CreatureValidator.MaxParts)
            {
                errors.Add("Vocabulary maxParts must be between 1 and " + CreatureValidator.MaxParts + ".");
            }
        }

        static bool AreDefined(UnitChannels channels)
        {
            bool isBodyKnown =
                Enum.IsDefined(typeof(LookSide), channels.side)
                && Enum.IsDefined(typeof(HeadKind), channels.head)
                && Enum.IsDefined(typeof(CountBand), channels.count);
            bool isFrameKnown =
                Enum.IsDefined(typeof(StemBand), channels.stem)
                && Enum.IsDefined(typeof(MassBand), channels.mass)
                && Enum.IsDefined(typeof(ReachBand), channels.reach);
            bool isAccessoryKnown =
                Enum.IsDefined(typeof(AccessoryKind), channels.accessory)
                && (
                    channels.accessory != AccessoryKind.MiniHead
                    || Enum.IsDefined(typeof(HeadKind), channels.accessoryHead)
                )
                && Enum.IsDefined(typeof(EffectFamily), channels.accent);
            return isBodyKnown && isFrameKnown && isAccessoryKnown;
        }

        static bool HasHead(LookVocabulary vocabulary, HeadKind head)
        {
            return vocabulary.heads != null && vocabulary.heads.ContainsKey(head) && vocabulary.heads[head] != null;
        }
    }
}
