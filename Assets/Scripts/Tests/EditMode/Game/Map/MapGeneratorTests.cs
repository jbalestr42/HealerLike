using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Map
{

public class MapGeneratorTests
{
    const int FloorCount = 10;
    const int ColumnCount = 7;
    const int PathCount = 6;

    // Structural rules must hold for any seed, not only a lucky one
    static IEnumerable<int> Seeds => Enumerable.Range(0, 50);

    static RunMap Generate(int seed)
    {
        return MapGenerator.Generate(FloorCount, ColumnCount, PathCount, new System.Random(seed));
    }

    [Test]
    public void Generate_HasOneListPerFloorAndBossAboveTheLastFloor([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = Generate(seed);

        Assert.AreEqual(FloorCount, map.floorCount);
        Assert.AreEqual(MapNodeType.Boss, map.boss.type);
        Assert.AreEqual(FloorCount, map.boss.floor);
        Assert.IsEmpty(map.boss.next);
        for (int floor = 0; floor < FloorCount; floor++)
        {
            Assert.IsNotEmpty(map.floors[floor], $"Floor {floor} is empty");
            Assert.IsTrue(map.floors[floor].All(node => node.floor == floor));
        }
    }

    [Test]
    public void Generate_NodesAreSortedByColumnAndInsideTheGrid([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = Generate(seed);

        foreach (IReadOnlyList<MapNode> floor in map.floors)
        {
            for (int i = 0; i < floor.Count; i++)
            {
                Assert.That(floor[i].column, Is.InRange(0, ColumnCount - 1));
                if (i > 0)
                {
                    Assert.Less(floor[i - 1].column, floor[i].column);
                }
            }
        }
    }

    [Test]
    public void Generate_StartsWithAtLeastTwoRoomsAndAtMostOnePerPath([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = Generate(seed);

        Assert.That(map.startNodes.Count, Is.InRange(2, PathCount));
    }

    [Test]
    public void Generate_EdgesGoToNextFloorMovingAtMostOneColumn([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = Generate(seed);

        foreach (IReadOnlyList<MapNode> floor in map.floors.Take(FloorCount - 1))
        {
            foreach (MapNode node in floor)
            {
                foreach (MapNode next in node.next)
                {
                    Assert.AreEqual(node.floor + 1, next.floor);
                    Assert.LessOrEqual(Mathf.Abs(next.column - node.column), 1);
                }
            }
        }
    }

    [Test]
    public void Generate_EveryRoomLeadsToTheBoss([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = Generate(seed);

        foreach (MapNode node in map.floors[FloorCount - 1])
        {
            CollectionAssert.AreEqual(new[] { map.boss }, node.next);
        }
        foreach (MapNode node in map.GetAllNodes().Where(node => node != map.boss))
        {
            Assert.IsNotEmpty(node.next, $"{node} is a dead end");
        }
    }

    [Test]
    public void Generate_EveryRoomIsReachableFromTheFirstFloor([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = Generate(seed);

        HashSet<MapNode> reached = new HashSet<MapNode>();
        Queue<MapNode> toVisit = new Queue<MapNode>(map.startNodes);
        while (toVisit.Count > 0)
        {
            MapNode node = toVisit.Dequeue();
            if (reached.Add(node))
            {
                foreach (MapNode next in node.next)
                {
                    toVisit.Enqueue(next);
                }
            }
        }

        CollectionAssert.AreEquivalent(map.GetAllNodes(), reached);
    }

    [Test]
    public void Generate_EdgesNeverCross([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = Generate(seed);

        foreach (IReadOnlyList<MapNode> floor in map.floors.Take(FloorCount - 1))
        {
            foreach (MapNode node in floor)
            {
                foreach (MapNode next in node.next.Where(next => next.column != node.column))
                {
                    MapNode neighbour = map.GetNode(node.floor, next.column);
                    MapNode neighbourTarget = map.GetNode(next.floor, node.column);
                    bool crosses = neighbour != null && neighbourTarget != null && neighbour.IsConnectedTo(neighbourTarget);
                    Assert.IsFalse(crosses, $"{node} -> {next} crosses {neighbour} -> {neighbourTarget}");
                }
            }
        }
    }

    [Test]
    public void Generate_AllRoomsExceptBossAreCombatsForNow()
    {
        RunMap map = Generate(0);

        Assert.IsTrue(map.GetAllNodes().Where(node => node != map.boss).All(node => node.type == MapNodeType.Combat));
    }

    [Test]
    public void Generate_SameSeed_GivesSameMap()
    {
        CollectionAssert.AreEqual(Describe(Generate(42)), Describe(Generate(42)));
    }

    [Test]
    public void Generate_DifferentSeeds_GiveDifferentMaps()
    {
        List<string> reference = Describe(Generate(0));

        Assert.IsTrue(Enumerable.Range(1, 10).Any(seed => !Describe(Generate(seed)).SequenceEqual(reference)));
    }

    [Test]
    public void Generate_SinglePath_IsAStraightLineOfOneRoomPerFloor()
    {
        RunMap map = MapGenerator.Generate(FloorCount, ColumnCount, 1, new System.Random(3));

        Assert.IsTrue(map.floors.All(floor => floor.Count == 1));
        Assert.IsTrue(map.GetAllNodes().Where(node => node != map.boss).All(node => node.next.Count == 1));
    }

    [Test]
    public void Generate_SingleColumn_KeepsEveryPathInThatColumn()
    {
        RunMap map = MapGenerator.Generate(FloorCount, 1, PathCount, new System.Random(3));

        Assert.IsTrue(map.floors.All(floor => floor.Count == 1 && floor[0].column == 0));
        Assert.AreEqual(0, map.boss.column);
    }

    [Test]
    public void Generate_SingleFloor_ConnectsStartRoomsToTheBoss()
    {
        RunMap map = MapGenerator.Generate(1, ColumnCount, PathCount, new System.Random(3));

        Assert.AreEqual(1, map.floorCount);
        Assert.AreEqual(1, map.boss.floor);
        Assert.IsTrue(map.startNodes.All(node => node.next.Single() == map.boss));
    }

    [Test]
    public void Generate_UsesTheSettingsSize()
    {
        MapGenerationSettings settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
        try
        {
            settings.floorCount = 4;
            settings.columnCount = 3;
            settings.pathCount = 2;

            RunMap map = MapGenerator.Generate(settings, 7);

            Assert.AreEqual(4, map.floorCount);
            Assert.AreEqual(3, map.columnCount);
            Assert.IsTrue(map.floors.All(floor => floor.Count <= 2));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }

    [TestCase(0, ColumnCount, PathCount)]
    [TestCase(FloorCount, 0, PathCount)]
    [TestCase(FloorCount, ColumnCount, 0)]
    public void Generate_InvalidSize_Throws(int floorCount, int columnCount, int pathCount)
    {
        Assert.Throws<ArgumentException>(() => MapGenerator.Generate(floorCount, columnCount, pathCount, new System.Random(0)));
    }

    // Flat description of the rooms and edges, to compare two maps
    static List<string> Describe(RunMap map)
    {
        return map.GetAllNodes().Select(node => $"{node} -> {string.Join(",", node.next.Select(next => next.column))}").ToList();
    }
}

}
