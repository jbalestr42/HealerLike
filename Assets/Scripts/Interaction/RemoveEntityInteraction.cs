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
        return target.GetComponentInParent<Entity>() != null;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        Entity entity = hit.transform.GetComponentInParent<Entity>();
        EntityManager.instance.DestroyEntity(entity.gameObject, entity.entityType);
    }
}
