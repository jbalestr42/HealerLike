using UnityEngine;

namespace HealerLike.Render
{

    public interface IHLDeliverySource
    {
        bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 intendedEnd);
        void UpdateDelivery(int token, Vector3 projectilePosition);
        void ContactDelivery(int token, Vector3 contactPosition, GameObject target);
        void EndDelivery(int token);
    }
}
