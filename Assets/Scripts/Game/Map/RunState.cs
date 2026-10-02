using System.Collections.Generic;

public enum MapNodeState
{
    Locked,
    Available,
    Visited,
    Current,
}

// Progress of the player on the run map: the room they are in and the path taken to reach it
public class RunState
{
    RunMap _map;
    public RunMap map => _map;

    // Null until the player picks a first room
    MapNode _currentNode = null;
    public MapNode currentNode => _currentNode;

    List<MapNode> _visitedNodes = new List<MapNode>();
    public IReadOnlyList<MapNode> visitedNodes => _visitedNodes;

    // -1 before the first room, floorCount on the boss
    public int currentFloor => _currentNode != null ? _currentNode.floor : -1;

    public bool isOnBoss => _currentNode == _map.boss;

    // Allies killed during the run, lost until resurrected (twice the same unit when two copies of it died)
    List<EntityData> _deadAllies = new List<EntityData>();
    public IReadOnlyList<EntityData> deadAllies => _deadAllies;

    public RunState(RunMap map)
    {
        _map = map;
    }

    public void AddDeadAlly(EntityData ally)
    {
        if (ally != null)
        {
            _deadAllies.Add(ally);
        }
    }

    // One copy of the unit is no longer dead (e.g. resurrected in a rest room), false when it wasn't dead
    public bool RemoveDeadAlly(EntityData ally)
    {
        return _deadAllies.Remove(ally);
    }

    // Rooms the player can go to next: the first floor at the start, then the rooms linked to the current one
    public IReadOnlyList<MapNode> GetAvailableNodes()
    {
        if (_currentNode == null)
        {
            return _map.startNodes;
        }
        return _currentNode.next;
    }

    public bool CanTravelTo(MapNode node)
    {
        foreach (MapNode availableNode in GetAvailableNodes())
        {
            if (availableNode == node)
            {
                return true;
            }
        }
        return false;
    }

    public bool TravelTo(MapNode node)
    {
        if (!CanTravelTo(node))
        {
            return false;
        }

        _currentNode = node;
        _visitedNodes.Add(node);
        return true;
    }

    public bool IsVisited(MapNode node)
    {
        return _visitedNodes.Contains(node);
    }

    public MapNodeState GetNodeState(MapNode node)
    {
        if (node == _currentNode)
        {
            return MapNodeState.Current;
        }
        if (IsVisited(node))
        {
            return MapNodeState.Visited;
        }
        if (CanTravelTo(node))
        {
            return MapNodeState.Available;
        }
        return MapNodeState.Locked;
    }
}
