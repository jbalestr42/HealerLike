using UnityEngine;

// One press has one owner. Coordinates are panel coordinates (positive Y points down).
public sealed class ToolkitPress
{
    public enum Owner { None, Pending, Tap, Hold, Scroll, Drag, Cancelled }
    public const float HoldSeconds = 0.4f;
    public const float Slop = 9f;
    public Owner owner { get; private set; }
    public int pointer { get; private set; } = -1;
    Vector2 _start;
    float _began;
    bool _canDrag;

    public bool Begin(int id, Vector2 point, float time, bool canDrag)
    {
        if (pointer >= 0) return false;
        pointer = id;
        _start = point;
        _began = time;
        _canDrag = canDrag;
        owner = Owner.Pending;
        return true;
    }

    public Owner Move(int id, Vector2 point, float time)
    {
        if (id != pointer || owner != Owner.Pending) return owner;
        Vector2 delta = point - _start;
        if (delta.magnitude > Slop)
        {
            owner = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? Owner.Scroll
                : _canDrag && delta.y < 0f ? Owner.Drag : Owner.Cancelled;
        }
        else if (time - _began >= HoldSeconds) owner = Owner.Hold;
        return owner;
    }

    public Owner End(int id, Vector2 point, float time)
    {
        if (id != pointer) return Owner.None;
        Move(id, point, time);
        if (owner == Owner.Pending) owner = Owner.Tap;
        pointer = -1;
        return owner;
    }

    public void Cancel()
    {
        pointer = -1;
        owner = Owner.Cancelled;
    }
}
