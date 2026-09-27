using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Spells
{
    // Spells describe their footprint. Ground owns motion, colour mixing, bounds and recovery.
    public static class SpellGround
    {
        public static void Play(Ground ground, EffectRecipe recipe, Vector3 at, float size = 1, bool critical = false)
        {
            Emit(ground, recipe, at, at, size, critical, false);
        }

        public static void Line(Ground ground, EffectRecipe recipe, Vector3 from, Vector3 to)
        {
            Emit(ground, recipe, from, to, 1f, false, true);
        }

        static void Emit(Ground ground, EffectRecipe recipe, Vector3 from, Vector3 to, float size,
            bool critical, bool isLine)
        {
            // Reject cyclic/over-budget composites atomically before publishing any reaction.
            if (ground == null || !RenderMath.IsPositive(size) || !EffectValidator.TryValidate(recipe, out _)) return;
            var pending = new Stack<EffectRecipe>();
            pending.Push(recipe);
            while (pending.Count > 0)
            {
                EffectRecipe current = pending.Pop();
                ElementEntry entry = current.entry;
                if (entry.ground != null)
                {
                    float strength = Mathf.Clamp01(entry.groundStrength * (critical ? 1.35f : 1f));
                    if (isLine && entry.ground.shape == GroundShape.Line)
                        ground.Play(entry.ground, from, to, strength);
                    else
                        ground.Play(entry.ground, to, entry.groundRadius * size, strength);
                }
                if (current.additions == null) continue;
                for (int i = current.additions.Length - 1; i >= 0; i--)
                    pending.Push(current.additions[i]);
            }
        }
    }
}
