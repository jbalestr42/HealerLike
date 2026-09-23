using UnityEngine;

namespace HealerLike.Render.Stones
{
    public interface IStoneMotionSource
    {
        bool TrySample(out Vector3 velocityWS, out Quaternion facingWS);
    }
}
