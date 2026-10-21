using System;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The only UI-to-world placement lease. All other UI-origin presses stay blocked in StageTouchInput.
    public sealed class StageRosterDrag : IToolkitRosterDrag, IDisposable
    {
        public delegate bool Project(Vector2 position, out RaycastHit hit, int mask);
        readonly InteractionManager _interaction;
        readonly Board _board;
        readonly Project _project;
        readonly Func<Vector2, bool> _overUi;
        readonly Action _claim;
        readonly Func<EntityData, Action<Entity>, EntityGridInteraction> _create;
        EntityGridInteraction _owned;
        StagePlacementCell _cell;
        public bool active => _owned != null;
        public bool valid { get; private set; }
        public Vector3 target { get; private set; }
        public StageRosterDrag(InteractionManager interaction, Board board, Project project,
            Func<Vector2, bool> overUi, Action claim,
            Func<EntityData, Action<Entity>, EntityGridInteraction> create = null)
        { _interaction = interaction; _board = board; _project = project; _overUi = overUi; _claim = claim;
            _create = create ?? ((data, spawned) => new EntityGridInteraction(data, Entity.EntityType.Player, false, spawned)); }

        public bool Begin(EntityData data, Vector2 point, Action<Entity> deployed)
        {
            if (active)
            {
                return false;
            }

            Cancel();
            if (data == null || data.model == null || _board == null || _board.layout == null || _interaction == null)
            {
                return false;
            }

            _claim();
            _owned = _create(data, deployed);
            _interaction.SetInteraction(_owned);
            _cell = new StagePlacementCell(_board.layout.cellSize);
            Move(point);
            return true;
        }
        public void Move(Vector2 point)
        {
            valid = TryTarget(point, out RaycastHit hit);
            if (!active)
            {
                return;
            }

            if (valid)
            {
                _board.TryGetPlacementPosition(hit.point, BoardCellType.Player, out Vector3 placement);
                target = placement;
                _owned.OnMouseOver(hit);
            }
            _cell?.Show(valid, target);
        }
        bool TryTarget(Vector2 point, out RaycastHit hit)
        {
            hit = default;
            if (_owned == null || !ReferenceEquals(_interaction.GetInteraction(), _owned))
            {
                return false;
            }
            // Lift the intended cell, not just its artwork, so preview and release validate the same ray.
            Vector2 lifted = point + Vector2.up * (56f * ToolkitScreenLayout.GetScale(Screen.width, Screen.height,
                Application.isMobilePlatform));
            if (!_project(lifted, out hit, _owned.GetLayerMask()) || !_owned.IsValidTarget(hit.collider.gameObject))
            {
                return false;
            }

            if (!_board.TryGetCoord(hit.point, out _))
            {
                return false;
            }
            // The board's placement rule, including the snap to the nearest free cell of the player's side.
            return _board.TryGetPlacementCell(hit.point, BoardCellType.Player, out _);
        }
        public bool End(Vector2 point)
        {
            bool commit = !_overUi(point) && TryTarget(point, out _);
            if (commit && TryTarget(point, out RaycastHit hit))
            {
                EntityGridInteraction action = _owned;
                _owned = null; // Release ownership before invoking gameplay, including reentrant refreshes.
                StageTouchInput.Activate(action, hit);
                if (ReferenceEquals(_interaction.GetInteraction(), action))
                {
                    _interaction.EndInteraction();
                }
            }
            Cancel();
            return commit;
        }
        public void Cancel()
        {
            if (_owned != null && ReferenceEquals(_interaction.GetInteraction(), _owned))
            {
                _interaction.CancelInteraction();
            }

            _owned = null; valid = false; _cell?.Dispose(); _cell = null;
        }
        public void Dispose() { Cancel(); }
    }
}
