using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    [Serializable]
    public struct HLArmDefinition
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
    }
}
