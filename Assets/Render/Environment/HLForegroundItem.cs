using UnityEngine;

namespace HealerLike.Render.Environment
{
    public enum HLForegroundKind
    {
        Boulder,
        Rosette
    }

    public struct HLForegroundItem
    {
        public HLForegroundKind kind;
        public Vector3 position;
        // Boulder radius or rosette height
        public float scale;
        // Ground footprint, for both kinds
        public float radius;
        public float yaw;
        public uint seed;
    }
}
