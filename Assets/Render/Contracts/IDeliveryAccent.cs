using UnityEngine;

namespace HealerLike.Render
{
    // A delivery source that colours the tip a delivery took, found by the token the delivery began with
    public interface IDeliveryAccent
    {
        void SetDeliveryAccent(int token, Color colour);
    }
}
