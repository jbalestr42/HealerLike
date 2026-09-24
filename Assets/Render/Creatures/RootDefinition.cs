using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    [Serializable]
    public struct RootDefinition
    {
        public int count;
        // Jointed cylinder segments per root, hip to foot
        public int segments;
        public float footRadius;
        public float hipHeight;
        public float kneeHeight;
        public float thickness;
        public Color colour;
    }
}
