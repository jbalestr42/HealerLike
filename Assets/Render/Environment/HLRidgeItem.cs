using UnityEngine;

namespace HealerLike.Render.Environment
{
    public enum HLRidgeKind
    {
        Monolith,
        Mushroom
    }

    public struct HLRidgeItem
    {
        public HLRidgeKind kind;
        public Vector3 position;
        // Full standing height
        public float height;
        public float width;
        // Zero for monoliths
        public float capDiameter;
        public float capThickness;
        public float yaw;
        public uint seed;
    }
}
