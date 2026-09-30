using System;
using System.Collections.Generic;
using UnityEngine;

// How the rooms of a type are placed on the map
[Serializable]
public class RoomTypeSettings
{
    public MapNodeType type;

    // Floors where every room is of this type (e.g. combats on the first floor, rests before the boss)
    public List<int> fixedFloors = new List<int>();

    // First floor a random room can be of this type
    [Min(0)] public int firstFloor = 0;

    // Share of this type among the random rooms, relative to the other types
    [Min(0f)] public float weight = 0f;

    // Two rooms of this type in a row on a path (e.g. combats), never for the special rooms
    public bool canFollowItself = false;
}

// Floors are indexed from 0 (first floor) to floorCount - 1 (floor right before the boss). The defaults are
// the Slay the Spire ones: 15 floors of 7 columns crossed by 6 paths
[CreateAssetMenu(menuName = "Custom/MapGenerationSettings")]
public class MapGenerationSettings : ScriptableObject
{
    [Header("Layout")]
    // Floors before the boss room
    [Min(1)] public int floorCount = 15;

    [Min(1)] public int columnCount = 7;

    // Paths drawn from the first floor to the last one, they can merge and split
    [Min(1)] public int pathCount = 6;

    // Rooms of the first floor to choose from, at most one per path and per column
    [Min(1)] public int startRoomCount = 3;

    // Rooms at most on a floor whatever the column count (e.g. 3 rooms spread over 5 columns), 0 for no limit
    [Min(0)] public int maxRoomsPerFloor = 0;

    [Header("Rooms")]
    // One entry per type a room can be; combats also fill the rooms no other type fits
    public List<RoomTypeSettings> roomTypes = CreateDefaultRoomTypes();

    // Slay the Spire: combats on the first floor, treasures on the 9th, rests before the boss, neither
    // elites nor rests below the 6th floor
    public static List<RoomTypeSettings> CreateDefaultRoomTypes()
    {
        return new List<RoomTypeSettings>
        {
            new RoomTypeSettings { type = MapNodeType.Combat, fixedFloors = new List<int> { 0 }, weight = 0.6f, canFollowItself = true },
            new RoomTypeSettings { type = MapNodeType.Elite, firstFloor = 5, weight = 0.16f },
            new RoomTypeSettings { type = MapNodeType.Rest, fixedFloors = new List<int> { 14 }, firstFloor = 5, weight = 0.12f },
            new RoomTypeSettings { type = MapNodeType.Treasure, fixedFloors = new List<int> { 8 }, weight = 0.05f },
        };
    }

    // Null when the type has no settings: no room of this type, but the combat fallback
    public RoomTypeSettings GetRoomType(MapNodeType type)
    {
        return roomTypes.Find(roomType => roomType.type == type);
    }

    // The type every room of the floor is, null when the floor isn't a fixed one (the first type listing it)
    public MapNodeType? GetFixedType(int floor)
    {
        foreach (RoomTypeSettings roomType in roomTypes)
        {
            if (roomType.fixedFloors.Contains(floor))
            {
                return roomType.type;
            }
        }
        return null;
    }

    // Every setting of the other one, its name aside (e.g. to save the settings tuned in the test scene)
    public void CopyFrom(MapGenerationSettings other)
    {
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(other), this);
    }
}
