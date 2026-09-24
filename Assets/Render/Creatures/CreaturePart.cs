using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    [Serializable]
    public struct CreaturePart
    {
        public string id;
        public int parent;
        public Primitive primitive;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public Vector3 dimensions;
        public Color colour;
        public float glow;
        public PartRole role;
        public int variant;
    }
}
