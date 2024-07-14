using UnityEngine;

public class DraggableEntity : MonoBehaviour, IDraggable
{
    GridManager _grid;
    Vector3 _originalPosition = Vector3.zero;

    void Start()
    {
        _grid = PlayerBehaviour.instance.grid;
    }

    #region IDraggable

    public void StartDrag(RaycastHit hit)
    {
        _originalPosition = transform.position;
    }

    public void Drag(RaycastHit hit)
    {
        if (hit.transform.gameObject.layer == Layers.Terrain)
        {
            var coord = _grid.GetCoordFromPosition(hit.point);
            if (_grid.CanPlaceObject(coord))
            {
                transform.position = _grid.GetCellCenterFromCoord(coord);
                _grid.SetWalkable(_originalPosition, true);
                _grid.SetWalkable(coord.x, coord.y, false);
                _originalPosition = transform.position;
            }
        }
        else if (hit.transform.gameObject.layer == Layers.Entity && hit.transform.gameObject != gameObject)
        {
            var entity = hit.transform.gameObject.GetComponent<Entity>();
            if (entity.entityType == Entity.EntityType.Player)
            {
                transform.position = _grid.GetCellCenterFromPosition(hit.transform.position);
                hit.transform.position = _grid.GetCellCenterFromPosition(_originalPosition);
                _originalPosition = transform.position;
            }
        }
    }

    public void EndDrag(RaycastHit hit) { }

    public void CancelDrag() { }

    #endregion
}
