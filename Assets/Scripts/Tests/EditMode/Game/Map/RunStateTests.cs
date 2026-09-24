using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Map
{

public class RunStateTests
{
    // start0 -> middle0 -> boss
    //        \           /
    // start1 -> middle1
    MapNode _start0;
    MapNode _start1;
    MapNode _middle0;
    MapNode _middle1;
    MapNode _boss;
    RunState _run;

    [SetUp]
    public void SetUp()
    {
        _start0 = new MapNode(0, 0, MapNodeType.Combat);
        _start1 = new MapNode(0, 1, MapNodeType.Combat);
        _middle0 = new MapNode(1, 0, MapNodeType.Rest);
        _middle1 = new MapNode(1, 1, MapNodeType.Elite);
        _boss = new MapNode(2, 0, MapNodeType.Boss);
        _start0.Connect(_middle0);
        _start0.Connect(_middle1);
        _start1.Connect(_middle1);
        _middle0.Connect(_boss);
        _middle1.Connect(_boss);

        List<List<MapNode>> floors = new List<List<MapNode>>
        {
            new List<MapNode> { _start0, _start1 },
            new List<MapNode> { _middle0, _middle1 },
        };
        _run = new RunState(new RunMap(floors, _boss, 2));
    }

    [Test]
    public void NewRun_IsBeforeTheFirstFloor()
    {
        Assert.IsNull(_run.currentNode);
        Assert.AreEqual(-1, _run.currentFloor);
        Assert.IsEmpty(_run.visitedNodes);
        Assert.IsFalse(_run.isOnBoss);
    }

    [Test]
    public void NewRun_OnlyStartRoomsAreAvailable()
    {
        CollectionAssert.AreEqual(new[] { _start0, _start1 }, _run.GetAvailableNodes());
        Assert.IsTrue(_run.CanTravelTo(_start1));
        Assert.IsFalse(_run.CanTravelTo(_middle0));
        Assert.IsFalse(_run.CanTravelTo(_boss));
    }

    [Test]
    public void TravelTo_AvailableRoom_MovesThere()
    {
        Assert.IsTrue(_run.TravelTo(_start0));

        Assert.AreSame(_start0, _run.currentNode);
        Assert.AreEqual(0, _run.currentFloor);
        Assert.IsTrue(_run.IsVisited(_start0));
        CollectionAssert.AreEqual(new[] { _middle0, _middle1 }, _run.GetAvailableNodes());
    }

    [Test]
    public void TravelTo_RoomNotLinkedToTheCurrentOne_IsRefused()
    {
        _run.TravelTo(_start1);

        Assert.IsFalse(_run.TravelTo(_middle0));

        Assert.AreSame(_start1, _run.currentNode);
        Assert.IsFalse(_run.IsVisited(_middle0));
    }

    [Test]
    public void TravelTo_RoomOfTheSameFloor_IsRefused()
    {
        _run.TravelTo(_start0);

        Assert.IsFalse(_run.TravelTo(_start1));
        Assert.AreSame(_start0, _run.currentNode);
    }

    [Test]
    public void TravelTo_CurrentRoomAgain_IsRefused()
    {
        _run.TravelTo(_start0);

        Assert.IsFalse(_run.TravelTo(_start0));
        Assert.AreEqual(1, _run.visitedNodes.Count);
    }

    [Test]
    public void TravelTo_UpToTheBoss_RecordsThePath()
    {
        _run.TravelTo(_start1);
        _run.TravelTo(_middle1);
        _run.TravelTo(_boss);

        CollectionAssert.AreEqual(new[] { _start1, _middle1, _boss }, _run.visitedNodes);
        Assert.IsTrue(_run.isOnBoss);
        Assert.AreEqual(2, _run.currentFloor);
        Assert.IsEmpty(_run.GetAvailableNodes());
        Assert.IsFalse(_run.IsVisited(_start0));
    }

    [Test]
    public void TravelTo_NullRoom_IsRefused()
    {
        Assert.IsFalse(_run.TravelTo(null));
        Assert.IsNull(_run.currentNode);
    }
}

}
