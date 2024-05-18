using UnityEngine;
using UnityEngine.Events;

public class SingleTargetInteraction : AInteraction
{
    UnityAction<GameObject> _onTargetSelected;
    UnityAction<bool> _onInteractionEnd;

    public SingleTargetInteraction(UnityAction<GameObject> onTargetSelected, UnityAction<bool> onInteractionEnd)
    {
        _onTargetSelected = onTargetSelected;
        _onInteractionEnd = onInteractionEnd;
        Cursor.SetCursor(Resources.Load<Texture2D>("selectTargetCursor"), new Vector2(256f, 256f), CursorMode.Auto);
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Entity;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        _onTargetSelected(hit.transform.gameObject);
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