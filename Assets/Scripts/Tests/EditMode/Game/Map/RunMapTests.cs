using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Map
{

public class RunMapTests
{
    MapNode _start;
    MapNode _middle;
    MapNode _boss;
    RunMap _map;

    [SetUp]
    public void SetUp()
    {
        _start = new MapNode(0, 1, MapNodeType.Combat);
        _middle = new MapNode(1, 2, MapNodeType.Combat);
        _boss = new MapNode(2, 1, MapNodeType.Boss);
        _start.Connect(_middle);
        _middle.Connect(_boss);

        List<List<MapNode>> floors = new List<List<MapNode>>
        {
            new List<MapNode> { _start },
            new List<MapNode> { _middle },
        };
        _map = new RunMap(floors, _boss, 3);
    }

    [Test]
    public void GetNode_FindsRoomsAndBoss()
    {
        Assert.AreSame(_start, _map.GetNode(0, 1));
        Assert.AreSame(_middle, _map.GetNode(1, 2));
        Assert.AreSame(_boss, _map.GetNode(2, 1));
    }

    [Test]
    public void GetNode_EmptyCellOrOutsideTheMap_ReturnsNull()
    {
        Assert.IsNull(_map.GetNode(0, 0));
        Assert.IsNull(_map.GetNode(-1, 1));
        Assert.IsNull(_map.GetNode(5, 1));
    }

    [Test]
    public void GetAllNodes_ListsFloorsInOrderThenBoss()
    {
        CollectionAssert.AreEqual(new[] { _start, _middle, _boss }, _map.GetAllNodes());
    }

    [Test]
    public void StartNodes_AreTheFirstFloor()
    {
        CollectionAssert.AreEqual(new[] { _start }, _map.startNodes);
        Assert.AreEqual(2, _map.floorCount);
        Assert.AreEqual(3, _map.columnCount);
    }
}

}
