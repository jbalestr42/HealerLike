using UnityEngine;

namespace HealerLike.Render.Environment
{
    public enum ForegroundKind
    {
        Boulder,
        Rosette
    }

    public struct ForegroundItem
    {
        public ForegroundKind kind;
        public Vector3 position;
        // Boulder radius or rosette height
        public float scale;
        // Ground footprint, for both kinds
        public float radius;
        public float yaw;
        public uint seed;
    }
}
