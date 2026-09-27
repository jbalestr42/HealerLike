using System.Collections.Generic;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Independent spell layers form one small emblem. Gameplay timing and data remain untouched.
    public class SpellIconRecipe
    {
        public List<EffectRecipe> layers = new List<EffectRecipe>();
        public static IEnumerable<EffectRecipe> Entries(EffectRecipe root)
        {
            // Call only on validated recipes; keep presentation inspection separate from composition.
            Stack<EffectRecipe> pending = new Stack<EffectRecipe>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                EffectRecipe recipe = pending.Pop();
                yield return recipe;
                foreach (EffectRecipe child in recipe.additions ?? System.Array.Empty<EffectRecipe>())
                {
                    pending.Push(child);
                }
            }
        }

        public IEnumerable<EffectRecipe> Entries()
        {
            foreach (EffectRecipe layer in layers)
            {
                foreach (EffectRecipe entry in Entries(layer))
                {
                    yield return entry;
                }
            }
        }

        public EffectReach reach;
        public EffectTrigger trigger;
        public EffectOrigin origin;
    }
}
