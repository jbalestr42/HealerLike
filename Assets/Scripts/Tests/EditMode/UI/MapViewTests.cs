using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

public class MapViewTests
{
    RunMap _map;

    [SetUp]
    public void SetUp()
    {
        _map = MapGenerator.GenerateLayout(10, 7, 6, new System.Random(0));
    }

    [Test]
    public void GetNodeAnchor_StaysInsideTheMapArea()
    {
        foreach (MapNode node in _map.GetAllNodes())
        {
            Vector2 anchor = MapView.GetNodeAnchor(node, _map);

            Assert.That(anchor.x, Is.InRange(0f, 1f));
            Assert.That(anchor.y, Is.InRange(0f, 1f));
        }
    }

    [Test]
    public void GetNodeAnchor_HigherFloorsAreHigherOnScreen()
    {
        MapNode start = _map.startNodes[0];
        MapNode next = start.next[0];

        Assert.Less(MapView.GetNodeAnchor(start, _map).y, MapView.GetNodeAnchor(next, _map).y);
        Assert.Less(MapView.GetNodeAnchor(_map.floors[_map.floorCount - 1][0], _map).y, MapView.GetNodeAnchor(_map.boss, _map).y);
    }

    [Test]
    public void GetNodeAnchor_ColumnsAreOrderedLeftToRight()
    {
        MapNode left = new MapNode(0, 0, MapNodeType.Combat);
        MapNode right = new MapNode(0, 6, MapNodeType.Combat);

        Assert.Less(MapView.GetNodeAnchor(left, _map).x, MapView.GetNodeAnchor(right, _map).x);
    }

    [Test]
    public void GetNodeAnchor_BossIsCentered()
    {
        Assert.AreEqual(0.5f, MapView.GetNodeAnchor(_map.boss, _map).x);
    }

    [Test]
    public void GetNodeAnchor_RoomsOfTheSameFloorShareTheirHeight()
    {
        foreach (var floor in _map.floors)
        {
            Assert.AreEqual(1, floor.Select(node => MapView.GetNodeAnchor(node, _map).y).Distinct().Count());
        }
    }

    [Test]
    public void GetNodeLabel_EveryTypeHasALabel()
    {
        foreach (MapNodeType type in Enum.GetValues(typeof(MapNodeType)))
        {
            Assert.IsNotEmpty(MapView.GetNodeLabel(type));
        }
    }
}

}
