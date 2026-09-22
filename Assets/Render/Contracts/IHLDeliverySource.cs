using UnityEngine;

namespace HealerLike.Render
{
    /// Implemented by any source model (creature builder, stone visual, character view) so
    /// HLProjectileVisualObserver can drive a delivery on whatever body fired the projectile.
    /// Frozen contract (wave 5). Tokens are per projectile and never reused while live.
    public interface IHLDeliverySource
    {
        /// Called once from Projectile.Init. Returns false if this source cannot present this delivery.
        bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 intendedEnd);
        /// Called every LateUpdate while the projectile lives, with its current world position.
        void UpdateDelivery(int token, Vector3 projectilePosition);
        /// Called on each accepted hit (bounce and chain may call it several times).
        void ContactDelivery(int token, Vector3 contactPosition, GameObject target);
        /// Called once when the projectile is destroyed or disabled; must be idempotent.
        void EndDelivery(int token);
    }
}
