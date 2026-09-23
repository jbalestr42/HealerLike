using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLIdlePose
    {
        public readonly Quaternion sway;
        public readonly Vector3 bodyScale;
        public readonly float bodyLift;

        public HLIdlePose(Quaternion sway, Vector3 scale, float lift)
        {
            this.sway = sway;
            bodyScale = scale;
            bodyLift = lift;
        }
    }
}
