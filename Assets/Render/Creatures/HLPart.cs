using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    [Serializable]
    public struct HLPart
    {
        public string id;
        public int parent;
        public HLPrimitive primitive;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public Vector3 dimensions;
        public Color colour;
        public float torusTubeRatio;
        public float glow;
    }
}
