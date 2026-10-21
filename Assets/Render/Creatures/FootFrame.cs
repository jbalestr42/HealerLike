using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct FootFrame
    {
        public readonly Vector3 origin;
        public readonly Vector3 normal;
        public readonly float cellSize;

        public FootFrame(Vector3 origin, Vector3 normal, float cellSize)
        {
            this.origin = origin;
            this.normal = normal.normalized;
            this.cellSize = cellSize;
        }
    }
}
