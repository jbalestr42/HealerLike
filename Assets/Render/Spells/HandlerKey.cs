using System;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // One buff handler on one target, the key of what a handler feeds
    public struct HandlerKey : IEquatable<HandlerKey>
    {
        public GameObject target;
        public ABuffHandlerFactory factory;

        public HandlerKey(GameObject target, ABuffHandlerFactory factory)
        {
            this.target = target;
            this.factory = factory;
        }

        public bool Equals(HandlerKey other)
        {
            return target == other.target && factory == other.factory;
        }

        public override bool Equals(object other)
        {
            return other is HandlerKey && Equals((HandlerKey)other);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(target, null))
            {
                hash = target.GetHashCode();
            }

            if (!ReferenceEquals(factory, null))
            {
                hash = (hash * 397) ^ factory.GetHashCode();
            }

            return hash;
        }
    }
}
