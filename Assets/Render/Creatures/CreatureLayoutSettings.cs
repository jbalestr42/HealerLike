using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    public static class CreatureLayoutSettings
    {
        public static bool IsValid(LookVocabulary.LayoutEntry layout, LookSide side)
        {
            bool shared =
                RenderMath.IsPositive(layout.shoulderOffset)
                && RenderMath.IsPositive(layout.threeHeadScale)
                && RenderMath.IsPositive(layout.fiveHeadScale)
                && RenderMath.IsPositive(layout.branchThickness)
                && RenderMath.IsPositive(layout.headClearance);
            if (!shared)
            {
                return false;
            }

            if (side == LookSide.Plant)
            {
                return RenderMath.IsPositive(layout.plantBodySink)
                    && RenderMath.IsPositive(layout.plantStemFoot)
                    && RenderMath.IsPositive(layout.minBranch)
                    && layout.maxBranch >= layout.minBranch
                    && RenderMath.IsPositive(layout.maxBranch)
                    && Angle(layout.threeHeadSpread)
                    && Angle(layout.fiveHeadSpread)
                    && layout.fiveHeadSpread < 45f
                    && RenderMath.IsPositive(layout.foreshortening)
                    && layout.foreshortening <= 1f
                    && RenderMath.IsPositive(layout.plantAccessoryClearance);
            }

            return RenderMath.IsPositive(layout.stoneBodyLift)
                && RenderMath.IsPositive(layout.stoneNeck)
                && RenderMath.IsPositive(layout.limbSpread)
                && float.IsFinite(layout.limbDepth)
                && RenderMath.IsPositive(layout.limbWidth)
                && RenderMath.IsPositive(layout.limbThickness)
                && float.IsFinite(layout.limbSplay)
                && Mathf.Abs(layout.limbSplay) <= 90f
                && float.IsFinite(layout.limbAsymmetry)
                && layout.limbAsymmetry >= 0f
                && layout.limbAsymmetry <= 0.25f
                && RenderMath.IsPositive(layout.limbBodyOverlap)
                && RenderMath.IsPositive(layout.stoneBranch)
                && RenderMath.IsPositive(layout.stoneBranchThickness)
                && RenderMath.IsPositive(layout.stoneAccessoryClearance);
        }

        static bool Angle(float value)
        {
            return RenderMath.IsPositive(value) && value < 90f;
        }
    }
}
