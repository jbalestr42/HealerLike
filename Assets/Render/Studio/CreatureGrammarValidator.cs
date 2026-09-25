using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio
{
    // What stops a grammar preset from composing: its channels, then the vocabulary tables they select, then the
    // fragments and sizes those entries hold, then the part budget. Each stage runs only once the one before is clean,
    // so the composer is never reached with a table it would read through.
    public static class CreatureGrammarValidator
    {
        public static string[] Validate(CreatureGrammarPreset preset)
        {
            List<string> errors = new List<string>();
            UnitChannels channels;
            string channelError;
            if (!preset.TryChannels(out channels, out channelError))
            {
                errors.Add(channelError);
            }

            if (!AreDefined(channels))
            {
                errors.Add("Choose valid grammar channel values.");
            }

            LookVocabulary vocabulary = preset.vocabulary;
            if (vocabulary == null)
            {
                errors.Add("Choose a look vocabulary.");
                return errors.ToArray();
            }

            CheckTables(vocabulary, channels, errors);
            if (errors.Count != 0)
            {
                return errors.ToArray();
            }

            CheckFragments(vocabulary, channels, errors);
            if (channels.side == LookSide.Plant)
            {
                CheckRoots(vocabulary, channels, errors);
            }

            foreach (ColourRole role in Enum.GetValues(typeof(ColourRole)))
            {
                if (!RenderMath.IsFinite(vocabulary.Colour(role, channels.accent, channels.side)))
                {
                    errors.Add("Palette colours must be finite.");
                    break;
                }
            }

            if (errors.Count != 0)
            {
                return errors.ToArray();
            }

            CreatureGrammarBudget.Check(vocabulary, channels, errors);
            return errors.ToArray();
        }

        // Every entry the channels select is present
        static void CheckTables(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
        {
            if (vocabulary.palette == null)
            {
                errors.Add("The vocabulary needs a palette.");
            }

            if (vocabulary.bodies == null || !vocabulary.bodies.ContainsKey(channels.mass)
                || vocabulary.bodies[channels.mass] == null)
            {
                errors.Add("The vocabulary is missing the selected mass/body entry.");
            }

            if (!HasHead(vocabulary, channels.head))
            {
                errors.Add("The vocabulary is missing the selected head entry.");
            }

            if (vocabulary.stems == null || !vocabulary.stems.ContainsKey(channels.stem)
                || vocabulary.stems[channels.stem] == null)
            {
                errors.Add("The vocabulary is missing the selected stem entry.");
            }

            if (channels.side == LookSide.Plant && vocabulary.roots == null)
            {
                errors.Add("The vocabulary root table is missing.");
            }

            bool hasAccessory = channels.accessory != AccessoryKind.None;
            if (hasAccessory && (vocabulary.accessories == null
                || !vocabulary.accessories.ContainsKey(channels.accessory)
                || vocabulary.accessories[channels.accessory] == null))
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

            if (!vocabulary.Layout.IsValid())
            {
                errors.Add("Layout proportions must be finite and within their supported ranges.");
            }

            if (vocabulary.maxParts < 1 || vocabulary.maxParts > CreatureValidator.MaxParts)
            {
                errors.Add("Vocabulary maxParts must be between 1 and " + CreatureValidator.MaxParts + ".");
            }
        }

        // The selected fragments hold valid parts, and the body and stem have real sizes
        static void CheckFragments(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
        {
            bool isPlant = channels.side == LookSide.Plant;
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];

            if (!float.IsFinite(head.plantStemScale) || head.plantStemScale < 0f)
            {
                errors.Add("Plant family stem scale must be finite and nonnegative; zero keeps legacy length.");
            }
            if (head.plantStem != null && !head.plantStem.IsValid())
            {
                errors.Add("Invalid articulated plant stem profile.");
            }

            CheckParts(Pick(body.plant, body.stone, isPlant), "body", CountBand.One, errors);
            CheckParts(Pick(head.plant, head.stone, isPlant), "head", head.carriesCount ? channels.count : CountBand.One,
                errors);

            bool isStemSized = RenderMath.IsPositive(stem.limbLength);
            if (isPlant)
            {
                isStemSized = RenderMath.IsPositive(stem.length) && RenderMath.IsPositive(stem.thickness);
            }

            if (!RenderMath.IsPositive(body.scale) || !float.IsFinite(body.bodyLift) || !isStemSized
                || !float.IsFinite(body.headScale) || body.headScale < 0f
                || !float.IsFinite(body.stemScale) || body.stemScale < 0f
                || !stem.plantShape.IsValid() || !stem.stoneLimbShape.IsValid())
            {
                errors.Add("Body scale and stem dimensions must be finite and positive, with valid shape profiles and body lift.");
            }

            if (channels.accessory == AccessoryKind.None)
            {
                return;
            }

            LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
            bool isMiniHead = channels.accessory == AccessoryKind.MiniHead;
            CheckParts(Pick(accessory.plant, accessory.stone, isPlant), "accessory", CountBand.One, errors);
            bool isMiniHeadPlaced = RenderMath.IsFinite(accessory.miniHeadAt)
                && RenderMath.IsPositive(accessory.miniHeadScale);
            if (!Enum.IsDefined(typeof(AccessorySocket), accessory.socket) || (isMiniHead && !isMiniHeadPlaced))
            {
                errors.Add("Accessory socket and miniature head settings are invalid.");
            }

            if (isMiniHead)
            {
                LookVocabulary.HeadEntry miniHead = vocabulary.heads[channels.accessoryHead];
                CheckParts(Pick(miniHead.plant, miniHead.stone, isPlant), "miniature head", CountBand.One, errors);
            }
        }

        // A plant's reach, its root and arm counts and the extent the renderer can draw
        static void CheckRoots(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
        {
            bool usesPinnedReach = vocabulary.isReachPinned || !vocabulary.roots.ContainsKey(channels.reach);
            if (usesPinnedReach && !RenderMath.IsPositive(vocabulary.pinnedReach))
            {
                errors.Add("Pinned reach must be finite and positive.");
            }

            if (!usesPinnedReach && !IsReachPositive(vocabulary.roots[channels.reach]))
            {
                errors.Add("The selected root entry requires finite positive reach.");
            }

            // The composer also reads Mid reach to choose the root segment count
            if (vocabulary.roots.ContainsKey(ReachBand.Mid) && !IsReachPositive(vocabulary.roots[ReachBand.Mid]))
            {
                errors.Add("The middle root entry requires finite positive reach.");
            }

            bool isRootCountSupported = vocabulary.rootCount >= 4 && vocabulary.rootCount <= 14;
            bool isArmCountSupported = vocabulary.armCount >= 0 && vocabulary.armCount <= ArmPool.MaxArms;
            bool areRootsSized = RenderMath.IsPositive(vocabulary.rootHip) && RenderMath.IsPositive(vocabulary.rootKnee)
                && RenderMath.IsPositive(vocabulary.rootThickness);
            if (!isRootCountSupported || !isArmCountSupported || !areRootsSized)
            {
                errors.Add("Plant roots or arm count are outside the renderer's supported ranges.");
            }

            if (errors.Count != 0)
            {
                return;
            }

            LookVocabulary.RootEntry entry;
            vocabulary.roots.TryGetValue(channels.reach, out entry);
            float thicknessScale = entry == null ? 1f : entry.ThicknessScale;
            if (entry != null && (!RenderMath.IsPositive(thicknessScale)
                || !entry.segmentShape.IsValid() || !entry.jointShape.IsValid()
                || !float.IsFinite(entry.taper) || entry.taper < 0f || entry.taper > 1f
                || !float.IsFinite(entry.jointScale) || entry.jointScale < 0f || entry.jointScale > 8f))
            {
                errors.Add("The selected root profiles or taper/joint proportions are invalid.");
                return;
            }

            float unit = vocabulary.Unit(channels.side);
            float extent = (vocabulary.Reach(channels.reach)
                + vocabulary.rootThickness * thicknessScale * 0.5f) * unit;
            if (extent > CreatureValidator.MaxRootReach)
            {
                errors.Add("The selected root reach and thickness exceed the renderer's maximum root extent.");
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
            bool isShapeKnown = Enum.IsDefined(typeof(Primitive), part.primitive)
                && Enum.IsDefined(typeof(PartRole), part.role);
            bool isToneKnown = Enum.IsDefined(typeof(ColourRole), part.colour)
                && Enum.IsDefined(typeof(CountBand), part.minCount);
            bool isPlaced = RenderMath.IsFinite(part.position) && RenderMath.IsFinite(part.euler);
            bool isSized = RenderMath.IsPositive(part.size.x) && RenderMath.IsPositive(part.size.y)
                && RenderMath.IsPositive(part.size.z);
            bool isGlowValid = float.IsFinite(part.glow) && part.glow >= 0f;

            return isNamed && isShapeKnown && isToneKnown && isPlaced && isSized && isGlowValid
                && part.shape.IsValid();
        }

        static bool AreDefined(UnitChannels channels)
        {
            bool isBodyKnown = Enum.IsDefined(typeof(LookSide), channels.side)
                && Enum.IsDefined(typeof(HeadKind), channels.head)
                && Enum.IsDefined(typeof(CountBand), channels.count);
            bool isFrameKnown = Enum.IsDefined(typeof(StemBand), channels.stem)
                && Enum.IsDefined(typeof(MassBand), channels.mass)
                && Enum.IsDefined(typeof(ReachBand), channels.reach);
            bool isAccessoryKnown = Enum.IsDefined(typeof(AccessoryKind), channels.accessory)
                && Enum.IsDefined(typeof(HeadKind), channels.accessoryHead)
                && Enum.IsDefined(typeof(EffectFamily), channels.accent);
            return isBodyKnown && isFrameKnown && isAccessoryKnown;
        }

        static bool HasHead(LookVocabulary vocabulary, HeadKind head)
        {
            return vocabulary.heads != null && vocabulary.heads.ContainsKey(head) && vocabulary.heads[head] != null;
        }

        static bool IsReachPositive(LookVocabulary.RootEntry root)
        {
            return root != null && RenderMath.IsPositive(root.reach);
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
