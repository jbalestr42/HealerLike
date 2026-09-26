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
        public ShapeProfile segmentShape;
        public ShapeProfile jointShape;

        // Zero retains the original root proportions in previously saved recipes.
        public float taper;
        public float jointScale;
    }
}
