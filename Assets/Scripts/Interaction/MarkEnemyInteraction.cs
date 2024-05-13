using UnityEngine;

public class MarkEnemyInteraction : AInteraction
{
    public MarkEnemyInteraction()
    {
        Cursor.SetCursor(Resources.Load<Texture2D>("markedCursor"), new Vector2(256f, 256f), CursorMode.Auto);
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Entity;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        MarkManager.instance.MarkEnemy(hit.transform.gameObject);
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
        MarkManager.instance.ResetMark();
    }

    public override void End()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}