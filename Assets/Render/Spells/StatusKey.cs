using System;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // One element on one target, the key of a status in the sink
    public struct StatusKey : IEquatable<StatusKey>
    {
        public GameObject target;
        public EffectElement element;

        public StatusKey(GameObject target, EffectElement element)
        {
            this.target = target;
            this.element = element;
        }

        public bool Equals(StatusKey other)
        {
            return target == other.target && element == other.element;
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

            return (hash * 397) ^ (int)element;
        }
    }
}
