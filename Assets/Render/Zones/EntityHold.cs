using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Whether the player is holding an entity: DraggableEntity moves its colliders off the raycast layers for the
    // length of a drag. A held creature is in the air, so it leaves the grass alone until it lands.
    public static class EntityHold
    {
        public static Collider Find(Component entity)
        {
            return entity != null ? entity.GetComponentInChildren<Collider>() : null;
        }

        public static bool IsHeld(Collider collider)
        {
            return collider != null && collider.gameObject.layer == Layers.IgnoreRaycast;
        }
    }
}
