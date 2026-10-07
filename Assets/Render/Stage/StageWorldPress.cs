using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Delay a creature move until slop resolves it, leaving a stationary press available to inspect.
    public sealed class StageWorldPress
    {
        readonly ToolkitPress _press = new ToolkitPress();
        readonly System.Func<float> _clock;
        public StageWorldPress(System.Func<float> clock = null)
        { _clock = clock ?? (() => Time.realtimeSinceStartup); }
        IDraggable _candidate;
        IDraggable _drag;
        Entity _entity;
        RaycastHit _start;
        public bool consumed { get; private set; }
        public void Begin(int id, Vector2 point, RaycastHit hit, bool canMove)
        {
            Cancel(); consumed = false; _start = hit;
            _entity = hit.collider != null ? hit.collider.GetComponentInParent<Entity>() : null;
            _candidate = canMove && hit.collider != null ? hit.collider.GetComponentInParent<IDraggable>() : null;
            _press.Begin(id, point, _clock(), true);
        }
        public void Move(int id, Vector2 point, RaycastHit hit, ToolkitGameUI ui, Vector2 screen)
        {
            var owner = _press.Move(id, point, _clock());
            if (owner == ToolkitPress.Owner.Hold && !consumed && _entity != null)
            {
                consumed = true;
                ui?.InspectEntity(_entity, screen);
            }
            // Board movement keeps the existing IDraggable move/swap rules in every direction.
            if ((owner == ToolkitPress.Owner.Drag || owner == ToolkitPress.Owner.Scroll
                || owner == ToolkitPress.Owner.Cancelled) && _candidate != null && _drag == null && !consumed)
            {
                consumed = true;
                if (_candidate.CanDrag()) { _drag = _candidate; _drag.StartDrag(_start); }
            }
            if (_drag != null && hit.collider != null)
            {
                _drag.Drag(hit);
            }
        }
        public void End(RaycastHit hit)
        {
            if (_drag != null && hit.collider != null) { _drag.EndDrag(hit); _drag = null; }
            Cancel();
        }
        public void Cancel()
        {
            _drag?.CancelDrag(); _drag = null; _candidate = null; _entity = null; _press.Cancel();
        }
    }
}
