using UnityEngine;

namespace HealerLike.Render.Stones
{
    public readonly struct HLStoneImpact
    {
        public readonly Vector3 pointWS;
        public readonly Vector3 normalWS;
        public readonly Vector3 incomingVelocityWS;
        public readonly bool estimated;

        public HLStoneImpact(Vector3 pointWS, Vector3 normalWS, Vector3 incomingVelocityWS, bool estimated)
        {
            this.pointWS = pointWS;
            this.normalWS = normalWS;
            this.incomingVelocityWS = incomingVelocityWS;
            this.estimated = estimated;
        }
    }
}
