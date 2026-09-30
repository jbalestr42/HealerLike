using System.Collections.Generic;
using NUnit.Framework;

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
        };
        CollectionAssert.AreEqual(expected, stats.Replace("\r", "").Split('\n'));
    }
}

}
