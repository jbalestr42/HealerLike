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

        // Legacy keeps the baked primitive; procedural profiles use a centered unit box.
        public ShapeProfile shape;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public Vector3 dimensions;
        public Color colour;
        public float glow;
        public PartRole role;
        public int variant;
        // Explicit outlet metadata. False preserves legacy recipes, which use the measured head fallback.
        public bool isSource;
    }
}
