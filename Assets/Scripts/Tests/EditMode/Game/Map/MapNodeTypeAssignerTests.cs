using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Map
{

public class MapNodeTypeAssignerTests
{
    static readonly MapNodeType[] NoRepeatTypes = { MapNodeType.Elite, MapNodeType.Rest, MapNodeType.Treasure };

    static IEnumerable<int> Seeds => Enumerable.Range(0, 50);

    MapGenerationSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_settings);
    }

    RunMap GenerateAndAssign(int seed)
    {
        System.Random random = new System.Random(seed);
        RunMap map = MapGenerator.GenerateLayout(_settings.floorCount, _settings.columnCount, _settings.pathCount, random);
        MapNodeTypeAssigner.Assign(map, _settings, random);
        return map;
    }

    IEnumerable<MapNode> Rooms(RunMap map)
    {
        return map.GetAllNodes().Where(node => node != map.boss);
    }

    // Straight path of one room per floor, then the boss
    static RunMap CreateLine(int floorCount)
    {
        List<List<MapNode>> floors = new List<List<MapNode>>();
        for (int floor = 0; floor < floorCount; floor++)
        {
            MapNode node = new MapNode(floor, 0, MapNodeType.Combat);
            if (floor > 0)
            {
                floors[floor - 1][0].Connect(node);
            }
            floors.Add(new List<MapNode> { node });
        }
        MapNode boss = new MapNode(floorCount, 0, MapNodeType.Boss);
        floors[floorCount - 1][0].Connect(boss);
        return new RunMap(floors, boss, 1);
    }

    [Test]
    public void Assign_FirstFloorIsOnlyCombats([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.IsTrue(map.floors[0].All(node => node.type == MapNodeType.Combat));
    }

    [Test]
    public void Assign_TreasureFloorIsOnlyTreasures([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.IsTrue(map.floors[_settings.treasureFloor].All(node => node.type == MapNodeType.Treasure));
    }

    [Test]
    public void Assign_LastFloorIsOnlyRests([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.IsTrue(map.floors[map.floorCount - 1].All(node => node.type == MapNodeType.Rest));
    }

    [Test]
    public void Assign_KeepsTheBoss([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.AreEqual(MapNodeType.Boss, map.boss.type);
        Assert.IsFalse(Rooms(map).Any(node => node.type == MapNodeType.Boss));
    }

    [Test]
    public void Assign_NoEliteOrRestBelowTheirFirstFloor([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.IsFalse(Rooms(map).Any(node => node.type == MapNodeType.Elite && node.floor < _settings.firstEliteFloor));
        Assert.IsFalse(Rooms(map).Any(node => node.type == MapNodeType.Rest && node.floor < _settings.firstRestFloor));
    }

    [Test]
    public void Assign_NoSpecialRoomTwiceInARow([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        foreach (MapNode node in Rooms(map).Where(node => NoRepeatTypes.Contains(node.type)))
        {
            Assert.IsFalse(node.next.Any(next => next.type == node.type), $"{node} is followed by the same type");
        }
    }

    [Test]
    public void Assign_DefaultSettings_UseEveryRandomTypeAcrossSeveralMaps()
    {
        List<MapNode> rooms = Seeds.SelectMany(seed => Rooms(GenerateAndAssign(seed))).ToList();

        // Outside the fixed floors, to make sure the random draw produces them too
        List<MapNode> randomRooms = rooms.Where(node => node.floor != 0 && node.floor != _settings.treasureFloor && node.floor != _settings.floorCount - 1).ToList();
        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Combat));
        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Elite));
        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Rest));
        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Treasure));
    }

    [Test]
    public void Assign_OnlyCombatWeight_GivesCombatsOutsideFixedFloors([ValueSource(nameof(Seeds))] int seed)
    {
        _settings.eliteWeight = 0f;
        _settings.restWeight = 0f;
        _settings.treasureWeight = 0f;
        _settings.treasureFloor = -1;
        _settings.restBeforeBoss = false;

        RunMap map = GenerateAndAssign(seed);

        Assert.IsTrue(Rooms(map).All(node => node.type == MapNodeType.Combat));
    }

    [Test]
    public void Assign_NoWeightAtAll_FallsBackToCombat()
    {
        _settings.combatWeight = 0f;
        _settings.eliteWeight = 0f;
        _settings.restWeight = 0f;
        _settings.treasureWeight = 0f;
        _settings.treasureFloor = -1;
        _settings.restBeforeBoss = false;

        RunMap map = GenerateAndAssign(0);

        Assert.IsTrue(Rooms(map).All(node => node.type == MapNodeType.Combat));
    }

    [Test]
    public void Assign_OnlyEliteWeight_AlternatesElitesAndCombatsOnALine()
    {
        _settings.combatWeight = 0f;
        _settings.restWeight = 0f;
        _settings.treasureWeight = 0f;
        _settings.treasureFloor = -1;
        _settings.restBeforeBoss = false;
        _settings.firstEliteFloor = 1;
        RunMap map = CreateLine(5);

        MapNodeTypeAssigner.Assign(map, _settings, new System.Random(0));

        MapNodeType[] expected = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Combat };
        CollectionAssert.AreEqual(expected, map.floors.Select(floor => floor[0].type));
    }

    [Test]
    public void Assign_RandomRoomBelowTheRestFloor_IsNeverARest()
    {
        _settings.combatWeight = 0f;
        _settings.eliteWeight = 0f;
        _settings.treasureWeight = 0f;
        _settings.treasureFloor = -1;
        _settings.firstRestFloor = 1;
        RunMap map = CreateLine(4);

        MapNodeTypeAssigner.Assign(map, _settings, new System.Random(0));

        MapNodeType[] expected = { MapNodeType.Combat, MapNodeType.Rest, MapNodeType.Combat, MapNodeType.Rest };
        CollectionAssert.AreEqual(expected, map.floors.Select(floor => floor[0].type));
    }

    [Test]
    public void Assign_TreasureFloorDisabled_NoForcedTreasureFloor()
    {
        _settings.treasureFloor = -1;
        _settings.treasureWeight = 0f;

        RunMap map = GenerateAndAssign(0);

        Assert.IsFalse(Rooms(map).Any(node => node.type == MapNodeType.Treasure));
    }

    [Test]
    public void Assign_SameSeed_GivesSameTypes()
    {
        List<MapNodeType> first = Rooms(GenerateAndAssign(42)).Select(node => node.type).ToList();
        List<MapNodeType> second = Rooms(GenerateAndAssign(42)).Select(node => node.type).ToList();

        CollectionAssert.AreEqual(first, second);
    }
}

}
