using System.Collections.Generic;
using UnityEngine;

// The board of the fights in the world, centered on its transform: a tile on each cell of the player or the enemies,
// coloured by side, and the unit standing on each cell
public class Board : MonoBehaviour
{
    // Used until a fight gives its own (e.g. before the first wave, in the Sandbox)
    [SerializeField] BoardLayout _defaultLayout;
    // Tile laid on each active cell, a quad when empty
    [SerializeField] GameObject _tilePrefab;
    [SerializeField] Material _tileMaterial;
    [SerializeField] Color _playerColor = new Color(0.25f, 0.45f, 0.8f);
    [SerializeField] Color _enemyColor = new Color(0.75f, 0.3f, 0.25f);
    // Share of the cell covered by its tile, the rest showing the cells apart
    [SerializeField, Range(0.1f, 1f)] float _tileScale = 0.92f;
    // Above the ground, for the tiles to be seen over it
    [SerializeField] float _tileHeight = 0.01f;
    // Laid on a blocked cell, a block when empty
    [SerializeField] GameObject _blockerPrefab;
    [SerializeField] Material _blockerMaterial;
    [SerializeField] Color _blockerColor = new Color(0.35f, 0.33f, 0.3f);

    BoardLayout _layout;
    public BoardLayout layout => _layout != null ? _layout : _defaultLayout;
    public BoardLayout defaultLayout { get { return _defaultLayout; } set { _defaultLayout = value; } }

    readonly Dictionary<Vector2Int, GameObject> _tiles = new Dictionary<Vector2Int, GameObject>();
    public IReadOnlyDictionary<Vector2Int, GameObject> tiles => _tiles;

    readonly Dictionary<Vector2Int, GameObject> _occupants = new Dictionary<Vector2Int, GameObject>();
    // The side of each unit on the board, for it to find a cell of its side when the board changes
    readonly Dictionary<GameObject, BoardCellType> _sides = new Dictionary<GameObject, BoardCellType>();

    // The sides whose center cell is blocked, on every layout
    readonly HashSet<BoardCellType> _blockedCenters = new HashSet<BoardCellType>();
    // The cells no unit can stand on and what is laid on them: not a unit, nothing to target or move
    readonly Dictionary<Vector2Int, GameObject> _blockers = new Dictionary<Vector2Int, GameObject>();
    public IReadOnlyDictionary<Vector2Int, GameObject> blockers => _blockers;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    // The size of a cell of the board of the scene, 1 without one (e.g. in a test)
    public static float currentCellSize
    {
        get
        {
            EntityManager manager = EntityManager.existingInstance;
            Board board = manager != null ? manager.board : null;
            return board != null && board.layout != null ? board.layout.cellSize : 1f;
        }
    }

    void Start()
    {
        if (_tiles.Count == 0)
        {
            BuildTiles();
            BuildBlockers();
        }
    }

    // Blocks the center cell of the side (see BoardLayout.TryGetCenterCell) on this layout and the next ones, or frees
    // it. The unit on it goes to the free cell of its side nearest to it; returns the units left without a cell, no
    // longer on the board
    public List<GameObject> SetCenterBlocked(BoardCellType side, bool blocked)
    {
        if (blocked)
        {
            _blockedCenters.Add(side);
        }
        else
        {
            _blockedCenters.Remove(side);
        }
        return SetLayout(_layout);
    }

    public bool IsBlocked(Vector2Int coord)
    {
        return _blockers.ContainsKey(coord);
    }

    // The board of the fight, the default one when null: the tiles are laid again. Each unit keeps its cell when it
    // is still a cell of its side, else goes to the free cell of its side nearest to where it stood. Returns the
    // units left without a cell, no longer on the board
    public List<GameObject> SetLayout(BoardLayout newLayout)
    {
        List<(GameObject unit, BoardCellType side, Vector2Int coord, Vector3 position)> units = new List<(GameObject, BoardCellType, Vector2Int, Vector3)>();
        foreach (KeyValuePair<Vector2Int, GameObject> occupant in _occupants)
        {
            if (occupant.Value != null)
            {
                units.Add((occupant.Value, _sides[occupant.Value], occupant.Key, occupant.Value.transform.position));
            }
        }
        ReleaseAll();

        _layout = newLayout;
        BuildTiles();
        BuildBlockers();

        // The units still on a free cell of their side first, so the others never take their cell
        List<GameObject> displaced = new List<GameObject>();
        foreach ((GameObject unit, BoardCellType side, Vector2Int coord, Vector3 position) unit in units)
        {
            Occupy(unit.coord, unit.unit, unit.side);
        }
        foreach ((GameObject unit, BoardCellType side, Vector2Int coord, Vector3 position) unit in units)
        {
            if (_sides.ContainsKey(unit.unit))
            {
                continue;
            }
            if (TryGetNearestFreeCell(unit.side, unit.position, out Vector2Int coord))
            {
                Occupy(coord, unit.unit, unit.side);
                unit.unit.transform.position = GetUnitPosition(coord);
            }
            else
            {
                displaced.Add(unit.unit);
            }
        }
        return displaced;
    }

    public BoardCellType GetCellType(Vector2Int coord)
    {
        return layout != null ? layout.GetCell(coord) : BoardCellType.None;
    }

    // Center of the cell on the ground
    public Vector3 GetCellCenter(Vector2Int coord)
    {
        return layout.GetCellCenter(transform.position, coord);
    }

    // Where a unit stands on the cell: on the ground, at its center
    public Vector3 GetUnitPosition(Vector2Int coord)
    {
        return GetCellCenter(coord);
    }

    // The cell under the position, false outside the board
    public bool TryGetCoord(Vector3 position, out Vector2Int coord)
    {
        if (layout == null)
        {
            coord = default;
            return false;
        }
        return layout.TryGetCoord(transform.position, position, out coord);
    }

    // A cell of a side (Player or Enemy) with no unit on it, not blocked
    public bool IsFree(Vector2Int coord, BoardCellType side)
    {
        return side != BoardCellType.None && GetCellType(coord) == side && !IsBlocked(coord) && GetOccupant(coord) == null;
    }

    // The unit on the cell, null when there is none (or it was destroyed)
    public GameObject GetOccupant(Vector2Int coord)
    {
        if (_occupants.TryGetValue(coord, out GameObject occupant) && occupant == null)
        {
            _occupants.Remove(coord);
            RemoveDestroyedSides();
        }
        return occupant != null ? occupant : null;
    }

    // Puts the unit on the cell if it is a free cell of its side, leaving the one it was on
    public bool Occupy(Vector2Int coord, GameObject unit, BoardCellType side)
    {
        if (unit == null || !IsFree(coord, side))
        {
            return false;
        }
        Release(unit);
        _occupants[coord] = unit;
        _sides[unit] = side;
        return true;
    }

    // The cell of the unit on the board, false when it is on none
    public bool TryGetCoordOf(GameObject unit, out Vector2Int coord)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> occupant in _occupants)
        {
            if (occupant.Value == unit && unit != null)
            {
                coord = occupant.Key;
                return true;
            }
        }
        coord = default;
        return false;
    }

    public void Release(GameObject unit)
    {
        if (TryGetCoordOf(unit, out Vector2Int coord))
        {
            _occupants.Remove(coord);
        }
        if (unit != null)
        {
            _sides.Remove(unit);
        }
    }

    public void Release(Vector2Int coord)
    {
        if (_occupants.TryGetValue(coord, out GameObject unit))
        {
            Release(unit);
            _occupants.Remove(coord);
        }
    }

    public void ReleaseAll()
    {
        _occupants.Clear();
        _sides.Clear();
    }

    void RemoveDestroyedSides()
    {
        List<GameObject> destroyed = new List<GameObject>();
        foreach (GameObject unit in _sides.Keys)
        {
            if (unit == null)
            {
                destroyed.Add(unit);
            }
        }
        destroyed.ForEach(unit => _sides.Remove(unit));
    }

    // The free cell of the side nearest to the position (on the ground), false when the side has none left. Two
    // cells as near: the first one column after column
    public bool TryGetNearestFreeCell(BoardCellType side, Vector3 position, out Vector2Int nearest)
    {
        nearest = default;
        if (layout == null)
        {
            return false;
        }
        bool found = false;
        float nearestSqrDistance = float.MaxValue;
        foreach (Vector2Int coord in layout.GetCells(side))
        {
            if (!IsFree(coord, side))
            {
                continue;
            }
            Vector3 center = GetCellCenter(coord);
            float dx = center.x - position.x;
            float dz = center.z - position.z;
            float sqrDistance = dx * dx + dz * dz;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = coord;
                nearestSqrDistance = sqrDistance;
                found = true;
            }
        }
        return found;
    }

    // Where a unit of the side placed at position goes: the cell under position when it is a free cell of the side,
    // else the free cell of the side nearest to it, false when the side has none left
    public bool TryGetPlacementCell(Vector3 position, BoardCellType side, out Vector2Int cell)
    {
        if (TryGetCoord(position, out cell) && IsFree(cell, side))
        {
            return true;
        }
        return TryGetNearestFreeCell(side, position, out cell);
    }

    // The same as TryGetPlacementCell, where the unit stands on that cell (position itself when there is none)
    public bool TryGetPlacementPosition(Vector3 position, BoardCellType side, out Vector3 placement)
    {
        bool found = TryGetPlacementCell(position, side, out Vector2Int cell);
        placement = found ? GetUnitPosition(cell) : position;
        return found;
    }

    static void DestroyAll(Dictionary<Vector2Int, GameObject> objects)
    {
        foreach (GameObject laid in objects.Values)
        {
            if (laid != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(laid);
                }
                else
                {
                    DestroyImmediate(laid);
                }
            }
        }
        objects.Clear();
    }

    // A block on the center cell of each blocked side of the layout
    void BuildBlockers()
    {
        DestroyAll(_blockers);
        if (layout == null)
        {
            return;
        }
        foreach (BoardCellType side in _blockedCenters)
        {
            if (layout.TryGetCenterCell(side, out Vector2Int center))
            {
                _blockers[center] = CreateBlocker(center);
            }
        }
    }

    GameObject CreateBlocker(Vector2Int coord)
    {
        GameObject blocker;
        float size = layout.cellSize * _tileScale * 0.8f;
        if (_blockerPrefab != null)
        {
            blocker = Instantiate(_blockerPrefab, transform);
            blocker.transform.position = GetCellCenter(coord);
        }
        else
        {
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.SetParent(transform, false);
            // Nothing to click or hit: not in the way of the clicks on the ground nor of the projectiles
            Collider blockerCollider = blocker.GetComponent<Collider>();
            if (blockerCollider != null)
            {
                DestroyImmediate(blockerCollider);
            }
            float height = layout.cellSize * 0.6f;
            blocker.transform.localScale = new Vector3(size, height, size);
            blocker.transform.position = GetCellCenter(coord) + Vector3.up * (height * 0.5f);
            SetColor(blocker.GetComponent<Renderer>(), _blockerMaterial, _blockerColor);
        }
        blocker.name = $"Blocker ({coord.x}, {coord.y})";
        return blocker;
    }

    // A tile on each cell of the player or the enemies, none on the cells of nobody
    void BuildTiles()
    {
        DestroyAll(_tiles);
        if (layout == null)
        {
            return;
        }

        for (int x = 0; x < layout.width; x++)
        {
            for (int y = 0; y < layout.height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                BoardCellType type = layout.GetCell(coord);
                if (type != BoardCellType.None)
                {
                    _tiles[coord] = CreateTile(coord, type == BoardCellType.Player ? _playerColor : _enemyColor);
                }
            }
        }
    }

    GameObject CreateTile(Vector2Int coord, Color color)
    {
        GameObject tile;
        if (_tilePrefab != null)
        {
            tile = Instantiate(_tilePrefab, transform);
        }
        else
        {
            tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.transform.SetParent(transform, false);
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // Not in the way of the clicks on the ground
            Collider tileCollider = tile.GetComponent<Collider>();
            if (tileCollider != null)
            {
                DestroyImmediate(tileCollider);
            }
        }
        tile.name = $"Cell ({coord.x}, {coord.y})";
        tile.transform.position = GetCellCenter(coord) + Vector3.up * _tileHeight;
        float size = layout.cellSize * _tileScale;
        tile.transform.localScale = new Vector3(size, size, 1f);

        SetColor(tile.GetComponent<Renderer>(), _tileMaterial, color);
        return tile;
    }

    static void SetColor(Renderer renderer, Material material, Color color)
    {
        if (renderer == null)
        {
            return;
        }
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }
}
