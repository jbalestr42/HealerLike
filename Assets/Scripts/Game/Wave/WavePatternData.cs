using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Custom/Data/WavePatternData")]
public class WavePatternData : SerializedScriptableObject
{
    // Columns of the grid, the front one first
    [FormerlySerializedAs("width")]
    [SerializeField] int _width = 5;
    // Rows of the grid
    [FormerlySerializedAs("height")]
    [SerializeField] int _height = 5;
    // The unit of each cell, column after column (x * height + y), null for an empty cell
    [SerializeField] List<EntityData> _cells = new List<EntityData>();

    // Written by the balance tools only
    [ReadOnly]
    public WaveScore score = new WaveScore();

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

    // The units of the wave with their cell, column after column, the empty cells left out
    public IEnumerable<(int x, int y, EntityData entity)> GetUnits()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                EntityData entity = GetEntity(x, y);
                if (entity != null)
                {
                    yield return (x, y, entity);
                }
            }
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

    // World position of a slot when the pattern is centered on center
    public Vector3 GetSlotPosition(Vector3 center, int x, int y)
    {
        return center - new Vector3(_width / 2f, 0f, _height / 2f) + new Vector3(x, 0f, y);
    }

    int GetIndex(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height ? x * _height + y : -1;
    }
}
