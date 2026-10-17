using UnityEngine;

// Puts every clicked entity in the status (one more stack on each click) until the interaction is cancelled
public class ApplyStatusInteraction : AInteraction
{
    StatusData _status;

    public ApplyStatusInteraction(StatusData status)
    {
        _status = status;
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Entity;
    }

    public override bool IsValidTarget(GameObject target)
    {
        return RemoveEntityInteraction.GetEntity(target) != null;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        Apply(hit.transform.gameObject);
    }

    // The hit collider can be anywhere under the entity, which gives itself the status
    public void Apply(GameObject hitObject)
    {
        Entity entity = RemoveEntityInteraction.GetEntity(hitObject);
        if (entity != null)
        {
            entity.ApplyStatus(_status, entity.gameObject);
        }
    }
}
