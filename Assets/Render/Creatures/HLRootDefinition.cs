using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    [Serializable]
    public struct HLRootDefinition
    {
        public int count;
        public float footRadius;
        public float hipHeight;
        public float kneeHeight;
        public float thickness;
        public float angularOffset;
        public Color colour;
    }
}
