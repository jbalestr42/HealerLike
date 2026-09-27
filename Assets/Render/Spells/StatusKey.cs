using System;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // One element on one target, the key of a status in the sink
    public struct StatusKey : IEquatable<StatusKey>
    {
        public GameObject target;
        public EffectKey element;
        public ElementEntry entry;
        public int layer;
        public HealerLike.Render.Grammar.EffectFamily family;
        public HealerLike.Render.Grammar.EffectTempo tempo;
        public float period;
        public float scale;
        public ABuffHandlerFactory clockOwner;
        public EffectRecipe[] additions;

        public StatusKey(GameObject target, EffectKey element)
        {
            this = default;
            this.target = target;
            this.element = element;
        }

        public StatusKey(GameObject target, EffectRecipe recipe, int layer = 0, ABuffHandlerFactory factory = null)
        {
            this.target = target; element = recipe.element; entry = recipe.entry; this.layer = layer;
            family = recipe.family; tempo = recipe.tempo; period = recipe.cycleSeconds; scale = recipe.scale;
            clockOwner = HasPeriodicClock(recipe) ? factory : null;
            additions = recipe.additions;
        }

        public bool Equals(StatusKey other)
        {
            return target == other.target && element == other.element && ReferenceEquals(entry, other.entry)
                && layer == other.layer && family == other.family && tempo == other.tempo && period.Equals(other.period) && scale.Equals(other.scale) && clockOwner == other.clockOwner
                && SameAdditions(additions, other.additions);
        }

        // Keys are made only from validated recipes, so recursive comparison stays inside the part budget.
        static bool HasPeriodicClock(EffectRecipe recipe)
        {
            if (recipe.tempo == HealerLike.Render.Grammar.EffectTempo.PerPeriod) return true;
            foreach (EffectRecipe child in recipe.additions ?? Array.Empty<EffectRecipe>())
                if (child != null && HasPeriodicClock(child)) return true;
            return false;
        }

        static bool SameAdditions(EffectRecipe[] left, EffectRecipe[] right)
        {
            int count = left != null ? left.Length : 0;
            if (count != (right != null ? right.Length : 0)) return false;
            for (int i = 0; i < count; i++)
            {
                EffectRecipe a = left[i], b = right[i];
                if (ReferenceEquals(a, b)) continue;
                if (a == null || b == null || !ReferenceEquals(a.entry, b.entry) || a.motion != b.motion
                    || a.socket != b.socket || a.family != b.family || a.tempo != b.tempo
                    || a.cycleSeconds != b.cycleSeconds || a.scale != b.scale || a.colour != b.colour
                    || !SameAdditions(a.additions, b.additions)) return false;
            }
            return true;
        }

        public override bool Equals(object other)
        {
            return other is StatusKey && Equals((StatusKey)other);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(target, null))
            {
                hash = target.GetHashCode();
            }

            return (((hash * 397) ^ (int)element) * 397 ^ (entry != null ? entry.GetHashCode() : 0)) * 397 ^ layer;
        }
    }
}
