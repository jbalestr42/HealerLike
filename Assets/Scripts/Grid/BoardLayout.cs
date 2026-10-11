using System.Collections.Generic;
using UnityEngine;

// What a cell of the board can hold: nothing, the units of the player or the enemies
public enum BoardCellType
{
    None,
    Player,
    Enemy,
}

// The board of a fight: a width x height grid where each cell is for the player, for the enemies or for nobody. The
// cells of a side can form several separate zones. In the world, the board is centered on its origin, the column 0
// on the left (-x) and the row 0 at the back (-z)
[CreateAssetMenu(menuName = "Custom/BoardLayout")]
public class BoardLayout : ScriptableObject
{
    public const int DefaultWidth = 9;
    public const int DefaultHeight = 4;
    // Columns of each side in the default board, the column between them empty
    public const int DefaultSideWidth = 4;

    [SerializeField, Min(1)] int _width = DefaultWidth;
    public int width => _width;

    [SerializeField, Min(1)] int _height = DefaultHeight;
    public int height => _height;

    [SerializeField, Min(0.1f)] float _cellSize = 1f;
    public float cellSize { get { return _cellSize; } set { _cellSize = Mathf.Max(0.1f, value); } }

    // Column after column (index x * height + y), like the wave patterns
    [SerializeField] List<BoardCellType> _cells = CreateDefaultCells();
    public IReadOnlyList<BoardCellType> cells => _cells;

    // The player on the left columns, the enemies on the right ones, an empty column between them
    static List<BoardCellType> CreateDefaultCells()
    {
        List<BoardCellType> cells = new List<BoardCellType>(DefaultWidth * DefaultHeight);
        for (int x = 0; x < DefaultWidth; x++)
        {
            BoardCellType type = x < DefaultSideWidth ? BoardCellType.Player : x >= DefaultWidth - DefaultSideWidth ? BoardCellType.Enemy : BoardCellType.None;
            for (int y = 0; y < DefaultHeight; y++)
            {
                cells.Add(type);
            }
        }
        return cells;
    }

    public static int GetIndex(int x, int y, int height)
    {
        return x * height + y;
    }

    // The side of the board the units of this type stand on
    public static BoardCellType GetCellType(Entity.EntityType entityType)
    {
        return entityType == Entity.EntityType.Player ? BoardCellType.Player : BoardCellType.Enemy;
    }

    public bool IsValid(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public bool IsValid(Vector2Int coord)
    {
        return IsValid(coord.x, coord.y);
    }

    // None out of the board
    public BoardCellType GetCell(int x, int y)
    {
        EnsureSize();
        return IsValid(x, y) ? _cells[GetIndex(x, y, _height)] : BoardCellType.None;
    }

    public BoardCellType GetCell(Vector2Int coord)
    {
        return GetCell(coord.x, coord.y);
    }

    public void SetCell(int x, int y, BoardCellType type)
    {
        EnsureSize();
        if (IsValid(x, y))
        {
            _cells[GetIndex(x, y, _height)] = type;
        }
    }

    // Every cell of this type, column after column
    public List<Vector2Int> GetCells(BoardCellType type)
    {
        List<Vector2Int> coords = new List<Vector2Int>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (GetCell(x, y) == type)
                {
                    coords.Add(new Vector2Int(x, y));
                }
            }
        }
        return coords;
    }

    // Each cell keeps its type at the same x and y, the new ones are None
    public void Resize(int newWidth, int newHeight)
    {
        EnsureSize();
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);
        List<BoardCellType> resized = new List<BoardCellType>(new BoardCellType[newWidth * newHeight]);
        for (int x = 0; x < Mathf.Min(_width, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(_height, newHeight); y++)
            {
                resized[GetIndex(x, y, newHeight)] = _cells[GetIndex(x, y, _height)];
            }
        }
        _width = newWidth;
        _height = newHeight;
        _cells = resized;
    }

    // Center of the cell in the world, the board centered on origin
    public Vector3 GetCellCenter(Vector3 origin, Vector2Int coord)
    {
        Vector3 min = GetMin(origin);
        return new Vector3(min.x + (coord.x + 0.5f) * _cellSize, origin.y, min.z + (coord.y + 0.5f) * _cellSize);
    }

    // The cell under the position, false outside the board
    public bool TryGetCoord(Vector3 origin, Vector3 position, out Vector2Int coord)
    {
        Vector3 min = GetMin(origin);
        coord = new Vector2Int(Mathf.FloorToInt((position.x - min.x) / _cellSize), Mathf.FloorToInt((position.z - min.z) / _cellSize));
        return IsValid(coord);
    }

    // The back left corner of the board in the world
    Vector3 GetMin(Vector3 origin)
    {
        return new Vector3(origin.x - _width * _cellSize * 0.5f, origin.y, origin.z - _height * _cellSize * 0.5f);
    }

    // A list of the wrong size (e.g. edited by hand) is fitted to the board, the missing cells None
    void EnsureSize()
    {
        int count = _width * _height;
        if (_cells == null)
        {
            _cells = new List<BoardCellType>();
        }
        while (_cells.Count < count)
        {
            _cells.Add(BoardCellType.None);
        }
        if (_cells.Count > count)
        {
            _cells.RemoveRange(count, _cells.Count - count);
        }
    }

    void OnValidate()
    {
        EnsureSize();
    }
}
