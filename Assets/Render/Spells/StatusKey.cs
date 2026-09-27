using System;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // One element on one target, the key of a status in the sink
    public struct StatusKey : IEquatable<StatusKey>
    {
        public GameObject target;
        public EffectElement element;
        public ElementEntry entry;
        public int layer;
        public HealerLike.Render.Grammar.EffectFamily family;
        public HealerLike.Render.Grammar.EffectTempo tempo;
        public float period;
        public float scale;
        public ABuffHandlerFactory clockOwner;

        public StatusKey(GameObject target, EffectElement element)
        {
            this = default;
            this.target = target;
            this.element = element;
        }

        public StatusKey(GameObject target, EffectRecipe recipe, int layer = 0, ABuffHandlerFactory factory = null)
        {
            this.target = target; element = recipe.element; entry = recipe.entry; this.layer = layer;
            family = recipe.family; tempo = recipe.tempo; period = recipe.cycleSeconds; scale = recipe.scale;
            clockOwner = recipe.tempo == HealerLike.Render.Grammar.EffectTempo.PerPeriod ? factory : null;
        }

        public bool Equals(StatusKey other)
        {
            return target == other.target && element == other.element && ReferenceEquals(entry, other.entry)
                && layer == other.layer && family == other.family && tempo == other.tempo && period.Equals(other.period) && scale.Equals(other.scale) && clockOwner == other.clockOwner;
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
