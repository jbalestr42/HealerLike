using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Plant root dimensions and count limits are checked before any arm arrays are allocated.
    public static class CreatureRootSettings
    {
        // A plant's reach, its root and arm counts and the extent the renderer can draw
        public static void Check(LookVocabulary vocabulary, UnitChannels channels, List<string> errors)
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
            bool areRootsSized =
                RenderMath.IsPositive(vocabulary.rootHip)
                && RenderMath.IsPositive(vocabulary.rootKnee)
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
            float thicknessScale = entry == null ? 1f : entry.effectiveThicknessScale;
            if (
                entry != null
                && (
                    !RenderMath.IsPositive(thicknessScale)
                    || !entry.segmentShape.IsValid()
                    || !entry.jointShape.IsValid()
                    || !float.IsFinite(entry.taper)
                    || entry.taper < 0f
                    || entry.taper > 1f
                    || !float.IsFinite(entry.jointScale)
                    || entry.jointScale < 0f
                    || entry.jointScale > 8f
                )
            )
            {
                errors.Add("The selected root profiles or taper/joint proportions are invalid.");
                return;
            }

            float unit = vocabulary.Unit(channels.side);
            float extent = (vocabulary.Reach(channels.reach) + vocabulary.rootThickness * thicknessScale * 0.5f) * unit;
            if (extent > CreatureValidator.MaxRootReach)
            {
                errors.Add("The selected root reach and thickness exceed the renderer's maximum root extent.");
            }
        }

        static bool IsReachPositive(LookVocabulary.RootEntry root)
        {
            return root != null && RenderMath.IsPositive(root.reach);
        }
    }
}
