using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct IdlePose
    {
        public readonly Quaternion sway;
        public readonly Vector3 bodyScale;
        public readonly float bodyLift;

        public IdlePose(Quaternion sway, Vector3 scale, float lift)
        {
            this.sway = sway;
            bodyScale = scale;
            bodyLift = lift;
        }
    }
}
