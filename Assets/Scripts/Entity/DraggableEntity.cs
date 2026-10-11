using UnityEngine;

public class DraggableEntity : MonoBehaviour, IDraggable
{
    Board _board;
    Entity _entity;
    Collider[] _colliders;
    int[] _originalLayers;
    Vector3 _homePosition = Vector3.zero;
    public Vector3 homePosition { get { return _homePosition; } }
    // The cell of the board left at the start of the drag, to go back to on cancel
    Vector2Int _homeCell;
    bool _hasHomeCell;

    void Start()
    {
        _board = EntityManager.instance.board;
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

    // Moves this entity onto the free cell closest to point: the cell under it when free, else the nearest one, the
    // units already placed never moved
    public void MoveToNearestFreeCell(Vector3 point)
    {
        // Only on a cell of its side
        if (_board.TryGetPlacementPosition(point, side, out Vector3 placement))
        {
            transform.position = placement;
        }
    }

    // Only the units of the player are dragged (see CanDrag)
    const BoardCellType side = BoardCellType.Player;

    void GoBackToHomeCell()
    {
        if (_hasHomeCell && _board.Occupy(_homeCell, gameObject, side))
        {
            transform.position = _board.GetUnitPosition(_homeCell);
        }
        else
        {
            transform.position = _homePosition;
        }
    }

    #region IDraggable

    public bool CanDrag()
    {
        return _entity.isDraggable && _entity.entityType == Entity.EntityType.Player;
    }

    public void StartDrag(RaycastHit hit)
    {
        // The dragged entity doesn't occupy any cell until it's dropped (see EndDrag/CancelDrag), so its home cell
        // stays a free cell to move back to.
        _hasHomeCell = _board.TryGetCoordOf(gameObject, out _homeCell);
        _board.Release(gameObject);
        // Otherwise this entity's own collider(s) would intercept the drag/drop raycast, hiding the cell under the cursor.
        SetIgnoredByRaycasts(true);
    }

    public void Drag(RaycastHit hit)
    {
        // Over another unit, its cell is taken: the closest free cell to the cursor instead
        int layer = hit.transform.gameObject.layer;
        if (layer == Layers.Terrain || layer == Layers.Entity)
        {
            MoveToNearestFreeCell(hit.point);
        }
    }

    public void EndDrag(RaycastHit hit)
    {
        // On the cell it was dragged to, else back to its cell
        if (_board.TryGetCoord(transform.position, out Vector2Int cell) && _board.Occupy(cell, gameObject, side))
        {
            transform.position = _board.GetUnitPosition(cell);
            _homePosition = transform.position;
        }
        else
        {
            GoBackToHomeCell();
        }
        SetIgnoredByRaycasts(false);
    }

    public void CancelDrag()
    {
        GoBackToHomeCell();
        SetIgnoredByRaycasts(false);
    }

    #endregion
}
