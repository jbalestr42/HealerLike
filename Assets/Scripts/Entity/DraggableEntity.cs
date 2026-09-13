using UnityEngine;

public class DraggableEntity : MonoBehaviour, IDraggable
{
    GridManager _grid;
    Entity _entity;
    Collider[] _colliders;
    Vector3 _originalPosition = Vector3.zero;
    Vector3 _homePosition = Vector3.zero;
    public Vector3 homePosition { get { return _homePosition; } }
    DraggableEntity _swapTarget;

    void Start()
    {
        _grid = PlayerBehaviour.instance.grid;
        _entity = GetComponent<Entity>();
        _colliders = GetComponentsInChildren<Collider>();
        _originalPosition = transform.position;
        _homePosition = transform.position;
    }

    void SetCollidersEnabled(bool isEnabled)
    {
        foreach (Collider collider in _colliders)
        {
            collider.enabled = isEnabled;
        }
    }

    #region IDraggable

    public bool CanDrag()
    {
        return _entity.isDraggable && _entity.entityType == Entity.EntityType.Player;
    }

    public void StartDrag(RaycastHit hit)
    {
        _originalPosition = transform.position;
        _swapTarget = null;
        // Otherwise this entity's own collider(s) would intercept the raycast once it moves onto
        // a swap target's cell, hiding the terrain underneath and making a re-hover of that same
        // cell look ambiguous.
        SetCollidersEnabled(false);
    }

    public void Drag(RaycastHit hit)
    {
        // With this entity's collider disabled, re-hovering the cell a previewed target vacated
        // now cleanly hits flat terrain (not this entity, not the target - both are elsewhere),
        // so comparing grid cells reliably detects "still hovering the same target" and avoids
        // re-triggering revert/preview every frame (which would otherwise flicker: the target
        // reverting to its real home - exactly where this entity currently sits - would put its
        // own collider back, getting freshly re-detected and re-previewed again next frame).
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
            if (_grid.CanPlaceObject(hitCoord))
            {
                transform.position = _grid.GetCellCenterFromCoord(hitCoord);
                _grid.SetWalkable(_originalPosition, true);
                _grid.SetWalkable(hitCoord.x, hitCoord.y, false);
                _originalPosition = transform.position;
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
            _swapTarget = null;
        }
        else
        {
            _homePosition = transform.position;
        }
        SetCollidersEnabled(true);
    }

    public void CancelDrag()
    {
        if (_swapTarget != null)
        {
            _swapTarget.RevertPreview();
            _swapTarget = null;
        }
        transform.position = _grid.GetCellCenterFromPosition(_homePosition);
        SetCollidersEnabled(true);
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
    }
}
