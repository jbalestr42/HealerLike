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

    RoomTypeSettings Type(MapNodeType type)
    {
        return _settings.GetRoomType(type);
    }

    // Defaults: 9th floor, and the floor right before the boss
    int treasureFloor => Type(MapNodeType.Treasure).fixedFloors[0];
    int restFloor => Type(MapNodeType.Rest).fixedFloors[0];

    bool IsFixedFloor(int floor)
    {
        return _settings.GetFixedType(floor).HasValue;
    }

    // Only the given types are drawn for the random rooms, and only the combats keep a fixed floor
    void KeepOnlyWeights(params MapNodeType[] types)
    {
        foreach (RoomTypeSettings roomType in _settings.roomTypes)
        {
            if (!types.Contains(roomType.type))
            {
                roomType.weight = 0f;
            }
            if (roomType.type != MapNodeType.Combat)
            {
                roomType.fixedFloors.Clear();
            }
        }
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

    IEnumerable<MapNode> RandomRooms(RunMap map)
    {
        return Rooms(map).Where(node => !IsFixedFloor(node.floor));
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

        Assert.IsTrue(map.floors[treasureFloor].All(node => node.type == MapNodeType.Treasure));
    }

    [Test]
    public void Assign_LastFloorIsOnlyRests([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.AreEqual(map.floorCount - 1, restFloor);
        Assert.IsTrue(map.floors[restFloor].All(node => node.type == MapNodeType.Rest));
    }

    [Test]
    public void Assign_SeveralFixedFloors_AreAllOfTheType([ValueSource(nameof(Seeds))] int seed)
    {
        Type(MapNodeType.Treasure).fixedFloors = new List<int> { 3, 8, 11 };

        RunMap map = GenerateAndAssign(seed);

        foreach (int floor in new[] { 3, 8, 11 })
        {
            Assert.IsTrue(map.floors[floor].All(node => node.type == MapNodeType.Treasure), $"Floor {floor}");
        }
    }

    [Test]
    public void Assign_KeepsTheBoss([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.AreEqual(MapNodeType.Boss, map.boss.type);
        Assert.IsFalse(Rooms(map).Any(node => node.type == MapNodeType.Boss));
    }

    [Test]
    public void Assign_NoRandomRoomBelowTheFirstFloorOfItsType([ValueSource(nameof(Seeds))] int seed)
    {
        Type(MapNodeType.Treasure).firstFloor = 10;
        Type(MapNodeType.Treasure).weight = 0.5f;

        RunMap map = GenerateAndAssign(seed);

        foreach (RoomTypeSettings roomType in _settings.roomTypes)
        {
            Assert.IsFalse(RandomRooms(map).Any(node => node.type == roomType.type && node.floor < roomType.firstFloor), roomType.type.ToString());
        }
        // The fixed treasure floor (8) stays full of treasures below the first treasure floor
        Assert.IsTrue(map.floors[treasureFloor].All(node => node.type == MapNodeType.Treasure));
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
        // Outside the fixed floors, to make sure the random draw produces them too
        List<MapNode> randomRooms = Seeds.SelectMany(seed => RandomRooms(GenerateAndAssign(seed))).ToList();

        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Combat));
        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Elite));
        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Rest));
        Assert.IsTrue(randomRooms.Any(node => node.type == MapNodeType.Treasure));
    }

    [Test]
    public void Assign_OnlyCombatWeight_GivesCombatsOutsideFixedFloors([ValueSource(nameof(Seeds))] int seed)
    {
        KeepOnlyWeights(MapNodeType.Combat);

        RunMap map = GenerateAndAssign(seed);

        Assert.IsTrue(Rooms(map).All(node => node.type == MapNodeType.Combat));
    }

    [Test]
    public void Assign_NoWeightAtAll_FallsBackToCombat()
    {
        KeepOnlyWeights();

        RunMap map = GenerateAndAssign(0);

        Assert.IsTrue(Rooms(map).All(node => node.type == MapNodeType.Combat));
    }

    [Test]
    public void Assign_TypeWithoutSettings_NeverAppears([ValueSource(nameof(Seeds))] int seed)
    {
        _settings.roomTypes.RemoveAll(roomType => roomType.type == MapNodeType.Elite);

        RunMap map = GenerateAndAssign(seed);

        Assert.IsFalse(Rooms(map).Any(node => node.type == MapNodeType.Elite));
    }

    [Test]
    public void Assign_OnlyEliteWeight_AlternatesElitesAndCombatsOnALine()
    {
        KeepOnlyWeights(MapNodeType.Elite);
        Type(MapNodeType.Elite).firstFloor = 1;
        RunMap map = CreateLine(5);

        MapNodeTypeAssigner.Assign(map, _settings, new System.Random(0));

        MapNodeType[] expected = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Combat };
        CollectionAssert.AreEqual(expected, map.floors.Select(floor => floor[0].type));
    }

    [Test]
    public void Assign_TypeThatCanFollowItself_CanBeTwiceInARow()
    {
        KeepOnlyWeights(MapNodeType.Elite);
        Type(MapNodeType.Elite).firstFloor = 1;
        Type(MapNodeType.Elite).canFollowItself = true;
        RunMap map = CreateLine(5);

        MapNodeTypeAssigner.Assign(map, _settings, new System.Random(0));

        MapNodeType[] expected = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Elite, MapNodeType.Elite, MapNodeType.Elite };
        CollectionAssert.AreEqual(expected, map.floors.Select(floor => floor[0].type));
    }

    [Test]
    public void Assign_RandomRoomBelowTheRestFloor_IsNeverARest()
    {
        KeepOnlyWeights(MapNodeType.Rest);
        Type(MapNodeType.Rest).fixedFloors = new List<int> { 3 };
        Type(MapNodeType.Rest).firstFloor = 1;
        RunMap map = CreateLine(4);

        MapNodeTypeAssigner.Assign(map, _settings, new System.Random(0));

        MapNodeType[] expected = { MapNodeType.Combat, MapNodeType.Rest, MapNodeType.Combat, MapNodeType.Rest };
        CollectionAssert.AreEqual(expected, map.floors.Select(floor => floor[0].type));
    }

    [Test]
    public void Assign_NoTreasureFixedFloorNorWeight_NoTreasure()
    {
        Type(MapNodeType.Treasure).fixedFloors.Clear();
        Type(MapNodeType.Treasure).weight = 0f;

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

    [Test]
    public void Assign_DefaultSettings_AreTheSlayTheSpireOnes()
    {
        Assert.AreEqual(15, _settings.floorCount);
        Assert.AreEqual(7, _settings.columnCount);
        Assert.AreEqual(6, _settings.pathCount);
        Assert.AreEqual(3, _settings.startRoomCount);
        // 9th floor, and neither elites nor rests below the 6th floor
        Assert.AreEqual(8, treasureFloor);
        Assert.AreEqual(5, Type(MapNodeType.Elite).firstFloor);
        Assert.AreEqual(5, Type(MapNodeType.Rest).firstFloor);
    }

    [Test]
    public void Assign_RoomsReachableFromTheSameRoom_NeverShareASpecialType([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        foreach (MapNode node in Rooms(map))
        {
            // The fixed floors are the same type everywhere, combat is the fallback when nothing else fits
            List<MapNode> randomSpecials = node.next.Where(next => next != map.boss && !IsFixedFloor(next.floor) && NoRepeatTypes.Contains(next.type)).ToList();
            Assert.AreEqual(randomSpecials.Count, randomSpecials.Select(next => next.type).Distinct().Count(), $"Two rooms after {node} share a type");
        }
    }

    [Test]
    public void Assign_NoRestRightBeforeTheRestFloor([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.IsFalse(map.floors[restFloor - 1].Any(node => node.type == MapNodeType.Rest));
    }

    [Test]
    public void Assign_NeverMoreSpecialRoomsThanTheirShare([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);
        List<MapNode> randomRooms = RandomRooms(map).ToList();
        float totalWeight = _settings.roomTypes.Sum(roomType => roomType.weight);

        foreach (MapNodeType type in NoRepeatTypes)
        {
            int expected = (int)System.Math.Round(randomRooms.Count * Type(type).weight / totalWeight, System.MidpointRounding.AwayFromZero);
            Assert.LessOrEqual(randomRooms.Count(node => node.type == type), expected, type.ToString());
        }
    }

    [Test]
    public void CreateTypeList_HoldsEachTypeInProportion()
    {
        // 100 rooms: 60 / 16 / 12 / 5 out of 93
        List<MapNodeType> types = MapNodeTypeAssigner.CreateTypeList(100, _settings, new System.Random(0));

        Assert.AreEqual(100, types.Count);
        Assert.AreEqual(17, types.Count(type => type == MapNodeType.Elite));
        Assert.AreEqual(13, types.Count(type => type == MapNodeType.Rest));
        Assert.AreEqual(5, types.Count(type => type == MapNodeType.Treasure));
        Assert.AreEqual(65, types.Count(type => type == MapNodeType.Combat));
    }

    [Test]
    public void Assign_Defaults_NoEventRoom([ValueSource(nameof(Seeds))] int seed)
    {
        RunMap map = GenerateAndAssign(seed);

        Assert.IsFalse(Rooms(map).Any(node => node.type == MapNodeType.Event));
    }

    [Test]
    public void Assign_EventWeight_PlacesEventRooms([ValueSource(nameof(Seeds))] int seed)
    {
        KeepOnlyWeights(MapNodeType.Combat, MapNodeType.Event);
        Type(MapNodeType.Event).weight = 0.5f;

        RunMap map = GenerateAndAssign(seed);

        Assert.IsTrue(RandomRooms(map).Any(node => node.type == MapNodeType.Event));
    }

    [Test]
    public void CreateTypeList_EventWeight_GivesItsShareOfEventRooms()
    {
        // 100 rooms: 60 combats / 16 / 12 / 5 / 7 events out of 100
        Type(MapNodeType.Event).weight = 0.07f;

        List<MapNodeType> types = MapNodeTypeAssigner.CreateTypeList(100, _settings, new System.Random(0));

        Assert.AreEqual(7, types.Count(type => type == MapNodeType.Event));
    }

    [Test]
    public void CreateTypeList_NoWeight_IsOnlyCombats()
    {
        KeepOnlyWeights();

        List<MapNodeType> types = MapNodeTypeAssigner.CreateTypeList(10, _settings, new System.Random(0));

        Assert.AreEqual(10, types.Count);
        Assert.IsTrue(types.All(type => type == MapNodeType.Combat));
    }
}

}
