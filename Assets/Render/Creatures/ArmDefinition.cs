using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    [Serializable]
    public struct ArmDefinition
    {
        public int bodyPart;
        public Vector3 rootLocal;
        public int sourceSocketIndex;
        public int segmentCount;
        public float segmentLength;
        public float radius;
        public Vector3[] restJoints;
        public Vector3 bendPole;
        public Color colour;
        // The tip's own colour, alpha 0 keeps the arm colour
        public Color tipColour;
    }
}
