using UnityEngine;
using UnityEngine.Serialization;
using Oisif.Inspector;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Custom/Data/WavePatternData")]
public class WavePatternData : ScriptableObject
{
    // The board of the fight: the grid of the wave is its grid, the units standing on its Enemy cells
    [SerializeField] BoardLayout _layout;
    // Columns of the grid, the width of the board
    [FormerlySerializedAs("width")]
    [SerializeField, HideInInspector] int _width = 5;
    // Rows of the grid, the height of the board
    [FormerlySerializedAs("height")]
    [SerializeField, HideInInspector] int _height = 5;
    // The unit of each cell, column after column (x * height + y), null for an empty cell
    [Grid(nameof(_width), nameof(_height), resizable = false, cellEnabledMethod = nameof(IsEnemyCell), cellColorMethod = nameof(GetCellColor))]
    [SerializeField] List<EntityData> _cells = new List<EntityData>();

    // Written by the balance tools only
    [ReadOnly]
    public WaveScore score = new WaveScore();

    public BoardLayout layout => _layout;

    // Changing the size keeps the unit of each cell still in the grid
    public int width
    {
        get => _width;
        set => Resize(value, _height);
    }

    public int height
    {
        get => _height;
        set => Resize(_width, value);
    }

    // The column of the Enemy cells nearest to the Player cells, where the wave meets the team: the column 0 without
    // board
    public int frontColumn => _layout != null ? _layout.GetFrontColumn(BoardCellType.Enemy) : 0;

    // The wave takes the size of the board, each unit keeping its cell
    public void SetLayout(BoardLayout layout)
    {
        _layout = layout;
        if (layout != null)
        {
            Resize(layout.width, layout.height);
        }
    }

    // The columns between the cell and the front one: 0 for the front column
    public int GetRank(int x)
    {
        return Mathf.Abs(x - frontColumn);
    }

    // True for the cells a unit can stand on: the Enemy cells of the board, every cell without board
    public bool IsEnemyCell(int x, int y)
    {
        return _layout == null || _layout.GetCell(x, y) == BoardCellType.Enemy;
    }

    // The colour of the side of the cell in the inspector
    Color GetCellColor(int x, int y)
    {
        switch (_layout != null ? _layout.GetCell(x, y) : BoardCellType.Enemy)
        {
            case BoardCellType.Player:
                return new Color(0.25f, 0.5f, 0.95f, 0.35f);
            case BoardCellType.Enemy:
                return new Color(0.9f, 0.3f, 0.25f, 0.3f);
            default:
                return new Color(0f, 0f, 0f, 0.3f);
        }
    }

    // The unit of the cell, null when the cell is empty or out of the grid
    public EntityData GetEntity(int x, int y)
    {
        int index = GetIndex(x, y);
        return index >= 0 && index < _cells.Count ? _cells[index] : null;
    }

    // Ignored out of the grid
    public void SetEntity(int x, int y, EntityData entity)
    {
        int index = GetIndex(x, y);
        if (index < 0)
        {
            return;
        }
        while (_cells.Count < _width * _height)
        {
            _cells.Add(null);
        }
        _cells[index] = entity;
    }

    // The units of the wave with their cell, column after column, the empty cells and the ones off the Enemy cells
    // left out
    public IEnumerable<(int x, int y, EntityData entity)> GetUnits()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                EntityData entity = GetEntity(x, y);
                if (entity != null && IsEnemyCell(x, y))
                {
                    yield return (x, y, entity);
                }
            }
        }
    }

    // The units with their rank (0 for the front column) instead of their column
    public IEnumerable<(int rank, int y, EntityData entity)> GetUnitsByRank()
    {
        foreach ((int x, int y, EntityData entity) unit in GetUnits())
        {
            yield return (GetRank(unit.x), unit.y, unit.entity);
        }
    }

    public void Resize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(0, newWidth);
        newHeight = Mathf.Max(0, newHeight);
        List<EntityData> cells = new List<EntityData>(new EntityData[newWidth * newHeight]);
        for (int x = 0; x < Mathf.Min(_width, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(_height, newHeight); y++)
            {
                cells[x * newHeight + y] = GetEntity(x, y);
            }
        }
        _width = newWidth;
        _height = newHeight;
        _cells = cells;
    }

    int GetIndex(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height ? x * _height + y : -1;
    }

    // The grid follows the size of the board
    void OnValidate()
    {
        if (_layout != null && (_layout.width != _width || _layout.height != _height))
        {
            Resize(_layout.width, _layout.height);
        }
    }
}
