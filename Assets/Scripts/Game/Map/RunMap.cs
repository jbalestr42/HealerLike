using System.Collections.Generic;

// The whole map of a run: floors of rooms from bottom (0) to top, then a single boss room
public class RunMap
{
    int _columnCount;
    public int columnCount => _columnCount;

    // Only the rooms on a path, sorted by column
    List<List<MapNode>> _floors;
    public IReadOnlyList<IReadOnlyList<MapNode>> floors => _floors;
    public int floorCount => _floors.Count;

    MapNode _boss;
    public MapNode boss => _boss;

    public IReadOnlyList<MapNode> startNodes => _floors[0];

    public RunMap(List<List<MapNode>> floors, MapNode boss, int columnCount)
    {
        _floors = floors;
        _boss = boss;
        _columnCount = columnCount;
    }

    public MapNode GetNode(int floor, int column)
    {
        if (floor == _boss.floor && column == _boss.column)
        {
            return _boss;
        }

        if (floor < 0 || floor >= _floors.Count)
        {
            return null;
        }

        return _floors[floor].Find(node => node.column == column);
    }

    // Every room, floor by floor, the boss last
    public IEnumerable<MapNode> GetAllNodes()
    {
        foreach (List<MapNode> floor in _floors)
        {
            foreach (MapNode node in floor)
            {
                yield return node;
            }
        }
        yield return _boss;
    }
}
