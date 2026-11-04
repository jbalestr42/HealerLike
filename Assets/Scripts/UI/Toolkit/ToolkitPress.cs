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
    bool _canScroll;

    // A sideways move scrolls only a row that can scroll; otherwise it drags like an upward one
    public bool Begin(int id, Vector2 point, float time, bool canDrag, bool canScroll = true)
    {
        if (pointer >= 0)
        {
            return false;
        }

        pointer = id;
        _start = point;
        _began = time;
        _canDrag = canDrag;
        _canScroll = canScroll;
        owner = Owner.Pending;
        return true;
    }

    public Owner Move(int id, Vector2 point, float time)
    {
        if (id != pointer || owner != Owner.Pending)
        {
            return owner;
        }

        Vector2 delta = point - _start;
        if (delta.magnitude > Slop)
        {
            bool sideways = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            // Moving down goes back into the row, never toward the board
            bool downward = !sideways && delta.y > 0f;
            owner = sideways && _canScroll ? Owner.Scroll
                : _canDrag && !downward ? Owner.Drag : Owner.Cancelled;
        }
        else if (time - _began >= HoldSeconds)
        {
            owner = Owner.Hold;
        }

        return owner;
    }

    // A scroll that leaves its row becomes a drag, so a card can still be pulled out sideways
    public Owner LeaveScroll(int id)
    {
        if (id == pointer && owner == Owner.Scroll && _canDrag)
        {
            owner = Owner.Drag;
        }

        return owner;
    }

    public Owner End(int id, Vector2 point, float time)
    {
        if (id != pointer)
        {
            return Owner.None;
        }

        Move(id, point, time);
        if (owner == Owner.Pending)
        {
            owner = Owner.Tap;
        }

        pointer = -1;
        return owner;
    }

    public void Cancel()
    {
        pointer = -1;
        owner = Owner.Cancelled;
    }
}
