using UnityEngine;

namespace HealerLike.Render.Stones
{
    public readonly struct StoneImpact
    {
        public readonly Vector3 point;
        public readonly Vector3 normal;

        public StoneImpact(Vector3 point, Vector3 normal)
        {
            this.point = point;
            this.normal = normal;
        }
    }
}
