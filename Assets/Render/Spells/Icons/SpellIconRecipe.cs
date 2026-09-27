using System.Collections.Generic;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Independent spell layers form one small emblem. Gameplay timing and data remain untouched.
    public class SpellIconRecipe
    {
        public List<EffectRecipe> layers = new List<EffectRecipe>();
        public EffectReach reach;
        public EffectTrigger trigger;
        public EffectOrigin origin;
    }
}
