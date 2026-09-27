namespace HealerLike.Render.Spells
{
    public static class EffectValidator
    {
        public const int MaxParts = EffectCompositionValidator.MaxParts;

        public static bool TryValidate(EffectRecipe recipe, out string error)
        {
            if (recipe == null || recipe.entry == null || recipe.entry.parts == null || recipe.entry.parts.Length == 0)
            {
                error = "Require an effect entry with at least one part.";
                return false;
            }

            if (recipe.entry.parts.Length > MaxParts || recipe.count < 1 || recipe.count > MaxParts)
            {
                error = "Require 1..256 effect parts.";
                return false;
            }

            foreach (HealerLike.Render.Creatures.LookPart part in recipe.entry.parts)
            {
                if (!HealerLike.Render.Creatures.LookPartValidation.IsValid(part,
                    HealerLike.Render.Creatures.LookPartBounds.Spell))
                {
                    error = "Require valid spell part data.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
