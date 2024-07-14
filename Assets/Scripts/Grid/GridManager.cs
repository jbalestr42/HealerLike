using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class GridManager : MonoBehaviour 
{
    [SerializeField] GameObject _ground;


    [SerializeField] int _width;
    public int width { get { return _width; } set { _width = value; } }

    [SerializeField] int _height;
    public int height { get { return _height; } set { _height = value; } }

    [SerializeField] float _size;
    public float size { get { return _size; } set { _size = value; } }

    GridCell[] _cells;
    public GridCell[] cells { get { return _cells; } set { _cells = value; } }

    Vector3 _min;
    Vector3 _max;
    GridGraph _gridGraph;

    public void Generate()
    {
        _min = transform.position;
        _min.x -= (_width / 2.0f) * _size;
        _min.z -= (_height / 2.0f) * _size;
        _max = transform.position;
        _max.x += (_width / 2.0f) * _size + _size;
        _max.z += (_height / 2.0f) * _size + _size;

        _cells = new GridCell[_width * _height];
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i] = new GridCell();
            _cells[i].coord = new Vector2Int(i % _width, i / _width);
            _cells[i].center = GetCellCenterFromCoord(_cells[i].coord);
        }

        _ground.transform.localScale = new Vector3(_width * _size, 1f, _height * _size);

        _gridGraph = AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
        _gridGraph.collision.heightMask = LayerMask.GetMask("Terrain");
        _gridGraph.collision.collisionCheck = false;
        _gridGraph.neighbours = NumNeighbours.Eight;
        _gridGraph.cutCorners = false;
        _gridGraph.showNodeConnections = true;
        _gridGraph.center = transform.position;
        _gridGraph.SetDimensions(_width, _height, _size);
        _gridGraph.Scan();
    }

    void OnDestroy()
    {
        if (AstarPath.active != null && _gridGraph != null)
        {
            AstarPath.active.data.RemoveGraph(_gridGraph);
        }
    }

    public GridCell GetCell(Vector2Int coord)
    {
        return GetCell(coord.x, coord.y);
    }

    public GridCell GetCell(int x, int y)
    {
        return _cells[y * _width + x];
    }

    public bool IsWalkable(int x, int y)
    {
        if (IsValidCoord(x, y))
        {
            return _gridGraph.GetNode(x, y).Walkable;
        }
        return false;
    }

    public bool IsValidCoord(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public bool IsValidCoord(Vector2Int coord)
    {
        return IsValidCoord(coord.x, coord.y);
    }

    public void SetWalkable(GridCell cell, bool walkable)
    {
        SetWalkable(cell.coord.x, cell.coord.y, walkable);
    }

    public void SetWalkable(Vector3 position, bool walkable)
    {
        SetWalkable(GetCoordFromPosition(position), walkable);
    }

    public void SetWalkable(Vector2Int coord, bool walkable)
    {
        SetWalkable(coord.x, coord.y, walkable);
    }

    public void SetWalkable(int x, int y, bool walkable)
    {
        if (IsValidCoord(x, y))
        {
            AstarPath.active.AddWorkItem(new AstarWorkItem(() => {
                _gridGraph.GetNode(x, y).Walkable = walkable;
                _gridGraph.CalculateConnectionsForCellAndNeighbours(x, y);
                //_gridGraph.GetNodes(node => _gridGraph.CalculateConnections((GridNodeBase)node));
            }));
            AstarPath.active.FlushWorkItems();
        }
    }

    public Vector2Int GetCoordFromPosition(Vector3 position)
    {
        Vector2Int coord = Vector2Int.one;
        coord.x = position.x < _min.x ? 0 : (position.x > _max.x ? _width : (int)((position.x - _min.x) / _size));
        coord.y = position.z < _min.z ? 0 : (position.z > _max.z ? _width : (int)((position.z - _min.z) / _size));
        return coord;
    }

    public Vector3 GetCellCenterFromPosition(Vector3 position)
    {
        Vector2Int coord = GetCoordFromPosition(position);
        return GetCellCenterFromCoord(coord);
    }

    public Vector3 GetCellCenterFromCoord(Vector2Int coord)
    {
        Vector3 cellCenterPos = Vector3.one;
        cellCenterPos.x = _min.x + coord.x * _size + _size * 0.5f;
        cellCenterPos.y = _size * 0.5f;
        cellCenterPos.z = _min.z + coord.y * _size + _size * 0.5f;
        return cellCenterPos;
    }

    public bool CanPlaceObject(GridCell cell)
    {
        return CanPlaceObject(cell.coord);
    }

    public bool CanPlaceObject(Vector2Int coord)
    {
        return CanPlaceObject(new Bounds(GetCellCenterFromCoord(coord), new Vector3(_size, _size, _size)));
    }

    public bool CanPlaceObject(Bounds bounds)
    {
        GraphUpdateObject guo = new GraphUpdateObject(bounds);
        guo.modifyWalkability = true;
        guo.setWalkability = false;

        List<GraphNode> nodes = new List<GraphNode>();
        return GraphUpdateUtilities.UpdateGraphsNoBlock(guo, nodes, true);
    }

    public Vector3 GetNearestWalkablePosition(Vector3 position)
    {
        NNConstraint constraint = NNConstraint.None;
        constraint.constrainWalkability = true;
        constraint.walkable = true;
        NNInfoInternal info = _gridGraph.GetNearestForce(position, constraint);
        return (Vector3)info.node.position;
    }

    void OnDrawGizmos()
    {
        Vector3 position = transform.position;
        position.x -= (_width / 2.0f) * _size;
        position.y += 0.1f;
        position.z -= (_height / 2.0f) * _size;

        Gizmos.color = Color.red;
        for (int i = 0; i < _width + 1; i++)
        {
            Vector3 start = new Vector3(i * _size + position.x, position.y, position.z);
            Vector3 end = new Vector3(i * _size + position.x, position.y, _height * _size + position.z);

            Gizmos.DrawLine(start, end);
        }
        for (int i = 0; i < _height + 1; i++)
        {
            Vector3 start = new Vector3(position.x, position.y, i * _size + position.z);
            Vector3 end = new Vector3(_width * _size + position.x, position.y, i * _size + position.z);

            Gizmos.DrawLine(start, end);
        }
    }
}