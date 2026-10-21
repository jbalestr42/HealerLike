using System;
using System.Collections.Generic;

namespace HealerLike.Render.Creatures
{
    // The same authored presentation checks feed runtime acceptance and individual Studio diagnostics.
    public static class CreaturePresentationValidation
    {
        public static IEnumerable<string> Errors(CreatureRecipe recipe)
        {
            if (!recipe)
            {
                yield break;
            }

            if (!RenderMath.IsFinite(recipe.neckLocal))
            {
                yield return "Neck coordinates must be finite.";
            }
            if (!RenderMath.IsFinite(recipe.wiltColour) || !RenderMath.IsFinite(recipe.stoneOchre))
            {
                yield return "Wilt and stone colours must be finite.";
            }
            if (recipe.parts != null)
            {
                foreach (CreaturePart part in recipe.parts)
                {
                    if (!Enum.IsDefined(typeof(PartRole), part.role))
                    {
                        yield return "Every part needs a valid role.";
                        break;
                    }
                }
            }
            if (recipe.arms != null)
            {
                foreach (ArmDefinition arm in recipe.arms)
                {
                    if (!RenderMath.IsFinite(arm.tipColour))
                    {
                        yield return "Arm tip colours must be finite.";
                        break;
                    }
                }
            }
        }
    }
}
