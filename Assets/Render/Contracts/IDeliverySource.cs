using UnityEngine;

namespace HealerLike.Render
{
    // Authored per projectile prefab variant
    // Stored by value in assets: append new members, never reorder or remove
    public enum DeliveryStyle
    {
        Direct = 0,
        Arc = 1,
        Rigid = 2,
        Swarm = 3,
        Bounce = 4,
        ChainSync = 5,
        Thrown = 6,
    }

    public interface IDeliverySource
    {
        bool BeginDelivery(int token, DeliveryStyle style, Transform projectile, Vector3 intendedEnd);

        void ContactDelivery(int token, Vector3 contactPosition, GameObject target);

        // Called when the projectile is destroyed or disabled, can be called twice
        void EndDelivery(int token);
    }
}
