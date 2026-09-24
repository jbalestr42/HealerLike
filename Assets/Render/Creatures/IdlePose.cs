using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct IdlePose
    {
        public readonly Quaternion sway;
        public readonly Vector3 bodyScale;

        public IdlePose(Quaternion sway, Vector3 scale)
        {
            this.sway = sway;
            bodyScale = scale;
        }
    }
}
