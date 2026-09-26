using System.Collections.Generic;
using System;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Validation of the fragment entries selected for one unit, before geometry or recipe allocation.
    public static class CreatureFragments
    {
        // The selected fragments hold valid parts, and the body and stem have real sizes
        public static void Check(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
        {
            bool isPlant = channels.side == LookSide.Plant;
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            if (isPlant && (!float.IsFinite(head.plantStemScale) || head.plantStemScale < 0f))
            {
                errors.Add("Plant family stem scale must be finite and nonnegative; zero keeps legacy length.");
            }

            if (isPlant && head.plantStem != null && !head.plantStem.IsValid())
            {
                errors.Add("Invalid articulated plant stem profile.");
            }

            CheckParts(CreatureFragments.Pick(body.plant, body.stone, isPlant), "body", CountBand.One, errors);
            CheckParts(
                CreatureFragments.Pick(head.plant, head.stone, isPlant),
                "head",
                head.carriesCount ? channels.count : CountBand.One,
                errors
            );
            bool isStemSized = RenderMath.IsPositive(stem.limbLength);
            if (isPlant)
            {
                isStemSized = RenderMath.IsPositive(stem.length) && RenderMath.IsPositive(stem.thickness);
            }

            if (
                !RenderMath.IsPositive(body.scale)
                || !float.IsFinite(body.bodyLift)
                || !isStemSized
                || !float.IsFinite(body.headScale)
                || body.headScale < 0f
                || !float.IsFinite(body.stemScale)
                || body.stemScale < 0f
                || !(isPlant ? stem.plantShape : stem.stoneLimbShape).IsValid()
            )
            {
                errors.Add("Invalid layout, shape profile or selected band dimensions.");
            }

            if (channels.accessory == AccessoryKind.None)
            {
                return;
            }

            LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
            bool isMiniHead = channels.accessory == AccessoryKind.MiniHead;
            CheckParts(
                CreatureFragments.Pick(accessory.plant, accessory.stone, isPlant),
                "accessory",
                CountBand.One,
                errors
            );
            bool isMiniHeadPlaced =
                RenderMath.IsFinite(accessory.miniHeadAt) && RenderMath.IsPositive(accessory.miniHeadScale);
            if (!Enum.IsDefined(typeof(AccessorySocket), accessory.socket) || (isMiniHead && !isMiniHeadPlaced))
            {
                errors.Add("Accessory socket and miniature head settings are invalid.");
            }

            if (isMiniHead)
            {
                LookVocabulary.HeadEntry miniHead = vocabulary.heads[channels.accessoryHead];
                CheckParts(
                    CreatureFragments.Pick(miniHead.plant, miniHead.stone, isPlant),
                    "miniature head",
                    CountBand.One,
                    errors
                );
            }
        }

        static void CheckParts(LookPart[] parts, string label, CountBand band, List<string> errors)
        {
            if (parts == null || parts.Length == 0)
            {
                errors.Add("The selected " + label + " fragment has no parts.");
                return;
            }

            if (parts.Length > 256)
            {
                errors.Add("The selected " + label + " fragment exceeds 256 source parts.");
                return;
            }

            if (!FragmentPlacement.TryValidate(parts, band, out string attachmentError))
            {
                errors.Add("The selected " + label + " fragment contains invalid part data: " + attachmentError);
                return;
            }

            foreach (LookPart part in parts)
            {
                if (!IsValid(part))
                {
                    errors.Add("The selected " + label + " fragment contains invalid part data.");
                    break;
                }
            }
        }

        static bool IsValid(LookPart part)
        {
            bool isNamed = !string.IsNullOrEmpty(part.id);
            bool isShapeKnown =
                Enum.IsDefined(typeof(Primitive), part.primitive) && Enum.IsDefined(typeof(PartRole), part.role);
            bool isToneKnown =
                Enum.IsDefined(typeof(ColourRole), part.colour) && Enum.IsDefined(typeof(CountBand), part.minCount);
            bool isPlaced = RenderMath.IsFinite(part.position) && RenderMath.IsFinite(part.euler);
            bool isSized =
                RenderMath.IsPositive(part.size.x)
                && RenderMath.IsPositive(part.size.y)
                && RenderMath.IsPositive(part.size.z);
            bool isGlowValid = float.IsFinite(part.glow) && part.glow >= 0f;
            return isNamed && isShapeKnown && isToneKnown && isPlaced && isSized && isGlowValid && part.shape.IsValid();
        }

        public static LookPart[] Pick(LookPart[] plant, LookPart[] stone, bool isPlant)
        {
            if (isPlant)
            {
                return plant;
            }

            return stone;
        }
    }
}
