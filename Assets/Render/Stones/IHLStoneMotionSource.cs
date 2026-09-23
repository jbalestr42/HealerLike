using UnityEngine;

namespace HealerLike.Render.Stones
{
    public interface IHLStoneMotionSource
    {
        bool TrySample(out Vector3 velocityWS, out Quaternion facingWS);
    }
}
