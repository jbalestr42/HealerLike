using UnityEngine;

public class DraggableEntity : MonoBehaviour, IDraggable
{
    GridManager _grid;
    Entity _entity;
    Collider[] _colliders;
    int[] _originalLayers;
    Vector3 _homePosition = Vector3.zero;
    public Vector3 homePosition { get { return _homePosition; } }
    DraggableEntity _swapTarget;

    void Start()
    {
        _grid = PlayerBehaviour.instance.grid;
        _entity = GetComponent<Entity>();
        _colliders = GetComponentsInChildren<Collider>();
        _originalLayers = new int[_colliders.Length];
        _homePosition = transform.position;
    }

    // Moves this entity's collider(s) off the layer used by drag/drop raycasts (without touching
    // the colliders themselves), so InteractionManager stops self-hitting this entity mid-drag
    // while every other physics interaction (e.g. BoostCell triggers) keeps working normally.
    void SetIgnoredByRaycasts(bool isIgnored)
    {
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (isIgnored)
            {
                _originalLayers[i] = _colliders[i].gameObject.layer;
                _colliders[i].gameObject.layer = Layers.IgnoreRaycast;
            }
            else
            {
                _colliders[i].gameObject.layer = _originalLayers[i];
            }
        }
    }

    // A cell the dragged entity can move onto: a free one, or the home cell of the swap target
    // (still flagged as occupied since the target only moved there as a preview).
    public bool CanMoveTo(Vector2Int coord)
    {
        if (_swapTarget != null && coord == _grid.GetCoordFromPosition(_swapTarget.homePosition))
        {
            return true;
        }
        return _grid.CanPlaceObject(coord);
    }

    #region IDraggable

    public bool CanDrag()
    {
        return _entity.isDraggable && _entity.entityType == Entity.EntityType.Player;
    }

    public void StartDrag(RaycastHit hit)
    {
        _swapTarget = null;
        // The dragged entity doesn't occupy any cell until it's dropped (see EndDrag/CancelDrag).
        _grid.SetWalkable(_homePosition, true);
        // Otherwise this entity's own collider(s) would intercept the drag/drop raycast once it
        // moves onto a swap target's cell, hiding the terrain underneath and making a re-hover of
        // that same cell look ambiguous.
        SetIgnoredByRaycasts(true);
    }

    public void Drag(RaycastHit hit)
    {
        // With this entity ignored by raycasts, re-hovering the cell a previewed target vacated
        // now cleanly hits flat terrain (not this entity, not the target - both are elsewhere),
        // so comparing grid cells reliably detects "still hovering the same target" and avoids
        // re-triggering revert/preview every frame (which would otherwise flicker: the target
        // reverting to its real home - exactly where this entity currently sits - would be
        // freshly re-detected and re-previewed again next frame).
        Vector2Int hitCoord = _grid.GetCoordFromPosition(hit.point);
        bool stillOnSameTarget = _swapTarget != null && hitCoord == _grid.GetCoordFromPosition(_swapTarget.homePosition);

        if (!stillOnSameTarget)
        {
            DraggableEntity newSwapTarget = null;
            if (hit.transform.gameObject.layer == Layers.Entity && hit.transform.gameObject != gameObject)
            {
                var entity = hit.transform.gameObject.GetComponent<Entity>();
                if (entity.entityType == Entity.EntityType.Player)
                {
                    newSwapTarget = hit.transform.gameObject.GetComponent<DraggableEntity>();
                }
            }

            if (_swapTarget != newSwapTarget)
            {
                if (_swapTarget != null)
                {
                    _swapTarget.RevertPreview();
                }
                if (newSwapTarget != null)
                {
                    newSwapTarget.PreviewSwapTo(_homePosition);
                }
                _swapTarget = newSwapTarget;
            }
        }

        if (hit.transform.gameObject.layer == Layers.Terrain)
        {
            if (CanMoveTo(hitCoord))
            {
                transform.position = _grid.GetCellCenterFromCoord(hitCoord);
            }
        }
    }

    public void EndDrag(RaycastHit hit)
    {
        if (_swapTarget != null)
        {
            Vector3 targetHomePosition = _swapTarget.homePosition;
            _swapTarget.CommitSwap(_homePosition);
            _homePosition = targetHomePosition;
            transform.position = _grid.GetCellCenterFromPosition(_homePosition);
            _swapTarget = null;
        }
        else
        {
            _homePosition = transform.position;
        }
        _grid.SetWalkable(_homePosition, false);
        SetIgnoredByRaycasts(false);
    }

    public void CancelDrag()
    {
        if (_swapTarget != null)
        {
            _swapTarget.RevertPreview();
            _swapTarget = null;
        }
        transform.position = _grid.GetCellCenterFromPosition(_homePosition);
        _grid.SetWalkable(_homePosition, false);
        SetIgnoredByRaycasts(false);
    }

    #endregion

    // Moves this entity to preview a pending swap - visual only, doesn't commit a new home yet
    // so the move can still be reverted if the dragged entity moves away or the drag is cancelled.
    public void PreviewSwapTo(Vector3 position)
    {
        transform.position = _grid.GetCellCenterFromPosition(position);
    }

    public void RevertPreview()
    {
        transform.position = _grid.GetCellCenterFromPosition(_homePosition);
    }

    public void CommitSwap(Vector3 newHomePosition)
    {
        _homePosition = newHomePosition;
        transform.position = _grid.GetCellCenterFromPosition(newHomePosition);
        _grid.SetWalkable(newHomePosition, false);
    }
}
