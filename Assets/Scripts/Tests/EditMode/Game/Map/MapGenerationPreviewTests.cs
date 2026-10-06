using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Map
{

// The stats of the map generation test scene: seed, then the count and share of each room type
public class MapGenerationPreviewTests
{
    // Four rooms on a line (combat, elite, combat, rest), then the boss
    static RunMap CreateLine()
    {
        MapNodeType[] types = { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Combat, MapNodeType.Rest };
        List<List<MapNode>> floors = new List<List<MapNode>>();
        for (int floor = 0; floor < types.Length; floor++)
        {
            MapNode node = new MapNode(floor, 0, types[floor]);
            if (floor > 0)
            {
                floors[floor - 1][0].Connect(node);
            }
            floors.Add(new List<MapNode> { node });
        }
        MapNode boss = new MapNode(types.Length, 0, MapNodeType.Boss);
        floors[types.Length - 1][0].Connect(boss);
        return new RunMap(floors, boss, 1);
    }

    [Test]
    public void GetStats_CountsEachRoomTypeWithoutTheBoss()
    {
        string stats = MapGenerationPreview.GetStats(CreateLine(), 42);

        string[] expected =
        {
            "Seed 42",
            "4 rooms",
            "Combat: 2 (50%)",
            "Elite: 1 (25%)",
            "Treasure: 0 (0%)",
            "Rest: 1 (25%)",
            "Event: 0 (0%)",
        };
        CollectionAssert.AreEqual(expected, stats.Replace("\r", "").Split('\n'));
    }

    [Test]
    public void GetRoomRows_WeightShareAndCountOfEachType()
    {
        MapGenerationSettings settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
        try
        {
            settings.roomTypes = new List<RoomTypeSettings>
            {
                new RoomTypeSettings { type = MapNodeType.Combat, weight = 1.5f },
                new RoomTypeSettings { type = MapNodeType.Elite, weight = 0.5f },
                new RoomTypeSettings { type = MapNodeType.Event, weight = 0f },
            };
            Dictionary<MapNodeType, int> counts = MapGenerationPreview.CountRooms(CreateLine(), out int roomCount);

            List<MapGenerationPreview.RoomRow> rows = MapGenerationPreview.GetRoomRows(settings, counts, roomCount);

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual(MapNodeType.Combat, rows[0].type);
            Assert.AreEqual(1.5f, rows[0].weight);
            Assert.AreEqual(0.75f, rows[0].share, 0.0001f);
            Assert.AreEqual(2, rows[0].count);
            Assert.AreEqual(0.5f, rows[0].mapShare, 0.0001f);

            Assert.AreEqual(0.25f, rows[1].share, 0.0001f);
            Assert.AreEqual(1, rows[1].count);
            Assert.AreEqual(0.25f, rows[1].mapShare, 0.0001f);

            // No weight, none in the map
            Assert.AreEqual(0f, rows[2].share);
            Assert.AreEqual(0, rows[2].count);
            Assert.AreEqual(0f, rows[2].mapShare);
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    [Test]
    public void GetRoomRows_NoMapYet_NoCount()
    {
        MapGenerationSettings settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
        try
        {
            List<MapGenerationPreview.RoomRow> rows = MapGenerationPreview.GetRoomRows(settings, null, 0);

            Assert.AreEqual(settings.roomTypes.Count, rows.Count);
            Assert.IsTrue(rows.TrueForAll(row => row.count == 0 && row.mapShare == 0f));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    class StubEventRoom : AEventRoom
    {
        public override void Play(IEventRoomHost host) { }
    }

    [Test]
    public void GetEventRows_NameWeightAndShareOfEachEvent()
    {
        MapGenerationSettings settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
        StubEventRoom library = ScriptableObject.CreateInstance<StubEventRoom>();
        StubEventRoom unnamed = ScriptableObject.CreateInstance<StubEventRoom>();
        try
        {
            library.eventName = "Library";
            unnamed.name = "UnnamedEvent";
            settings.eventRooms = new List<EventRoomChance>
            {
                new EventRoomChance { eventRoom = library, weight = 3f },
                new EventRoomChance { eventRoom = unnamed, weight = 1f },
                new EventRoomChance { eventRoom = null, weight = 0f },
            };

            List<MapGenerationPreview.EventRow> rows = MapGenerationPreview.GetEventRows(settings);

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("Library", rows[0].name);
            Assert.AreEqual(3f, rows[0].weight);
            Assert.AreEqual(0.75f, rows[0].share, 0.0001f);
            // No event name: the asset name
            Assert.AreEqual("UnnamedEvent", rows[1].name);
            Assert.AreEqual(0.25f, rows[1].share, 0.0001f);
            Assert.AreEqual("(none)", rows[2].name);
            Assert.AreEqual(0f, rows[2].share);
        }
        finally
        {
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(library);
            Object.DestroyImmediate(unnamed);
        }
    }
}

}
