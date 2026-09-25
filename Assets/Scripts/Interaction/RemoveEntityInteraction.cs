using UnityEngine;

// Removes every clicked entity until the interaction is cancelled
public class RemoveEntityInteraction : AInteraction
{
    public override int GetLayerMask()
    {
        return 1 << Layers.Entity;
    }

    public override bool IsValidTarget(GameObject target)
    {
        return GetEntity(target) != null;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        Remove(GetEntity(hit.transform.gameObject));
    }

    // The hit collider can be anywhere under the entity (model, HUD, ...)
    public static Entity GetEntity(GameObject hitObject)
    {
        return hitObject.GetComponentInParent<Entity>();
    }

    public static void Remove(Entity entity)
    {
        EntityManager.instance.DestroyEntity(entity.gameObject, entity.entityType);
    }
}
