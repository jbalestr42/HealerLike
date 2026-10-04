using UnityEngine;
using UnityEngine.Events;

public class SingleTargetInteraction : AInteraction
{
    UnityAction<GameObject> _onTargetSelected;
    UnityAction<bool> _onInteractionEnd;
    Entity.EntityType _targetEntityType;

    public SingleTargetInteraction(UnityAction<GameObject> onTargetSelected, UnityAction<bool> onInteractionEnd, Entity.EntityType targetEntityType)
    {
        _onTargetSelected = onTargetSelected;
        _onInteractionEnd = onInteractionEnd;
        _targetEntityType = targetEntityType;
        Cursor.SetCursor(Resources.Load<Texture2D>("selectTargetCursor"), new Vector2(256f, 256f), CursorMode.Auto);
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Entity;
    }

    public override bool IsValidTarget(GameObject target)
    {
        return _targetEntityType == Entity.EntityType.Any || target.GetComponent<Entity>()?.entityType == _targetEntityType;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        SelectTarget(hit.transform.gameObject);
    }

    // Same as clicking the target, e.g. for a target chosen by code
    public void SelectTarget(GameObject target)
    {
        _onTargetSelected(target);
        InteractionManager.instance.EndInteraction();
    }

    public override void OnMouseEnter(RaycastHit hit)
    {
    }

    public override void OnMouseExit()
    {
    }

    public override void Cancel()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        _onInteractionEnd(false);
    }

    public override void End()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        _onInteractionEnd(true);
    }
}