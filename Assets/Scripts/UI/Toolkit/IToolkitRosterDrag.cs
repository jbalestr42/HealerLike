using UnityEngine;

// Drags a party card onto the board: the touch input owns projection and the existing gameplay placement action.
public interface IToolkitRosterDrag
{
    bool Begin(EntityData data, Vector2 screenPoint, System.Action<Entity> deployed);
    void Move(Vector2 screenPoint);
    bool End(Vector2 screenPoint);
    void Cancel();
}
