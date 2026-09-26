using System.Collections.Generic;

public enum MapNodeType
{
    Combat,
    Elite,
    Treasure,
    Rest,
    Boss,
}

// A room of the run map, linked to the rooms of the next floor the player can travel to
public class MapNode
{
    int _floor;
    public int floor => _floor;

    int _column;
    public int column => _column;

    MapNodeType _type;
    public MapNodeType type { get { return _type; } set { _type = value; } }

    List<MapNode> _next = new List<MapNode>();
    public IReadOnlyList<MapNode> next => _next;

    List<MapNode> _previous = new List<MapNode>();
    public IReadOnlyList<MapNode> previous => _previous;

    public MapNode(int floor, int column, MapNodeType type)
    {
        _floor = floor;
        _column = column;
        _type = type;
    }

    public void Connect(MapNode nextNode)
    {
        if (_next.Contains(nextNode))
        {
            return;
        }

        _next.Add(nextNode);
        nextNode._previous.Add(this);
    }

    public bool IsConnectedTo(MapNode nextNode)
    {
        return _next.Contains(nextNode);
    }

    public override string ToString()
    {
        return $"{_type} ({_floor}, {_column})";
    }
}
