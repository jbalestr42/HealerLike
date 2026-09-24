using UnityEngine;

// Floors are indexed from 0 (first floor) to floorCount - 1 (floor right before the boss)
[CreateAssetMenu(menuName = "Custom/MapGenerationSettings")]
public class MapGenerationSettings : ScriptableObject
{
    [Header("Layout")]
    // Floors before the boss room
    [Min(1)] public int floorCount = 10;

    [Min(1)] public int columnCount = 7;

    // Paths drawn from the first floor to the last one, they can merge and split
    [Min(1)] public int pathCount = 6;

    [Header("Fixed floors")]
    // Every room of this floor is a treasure, -1 to disable
    public int treasureFloor = 5;

    // Every room of the last floor is a rest, to heal before the boss
    public bool restBeforeBoss = true;

    [Header("Random rooms")]
    public int firstEliteFloor = 3;
    public int firstRestFloor = 3;

    // Relative chances of the rooms not on a fixed floor
    [Min(0f)] public float combatWeight = 0.6f;
    [Min(0f)] public float eliteWeight = 0.16f;
    [Min(0f)] public float restWeight = 0.12f;
    [Min(0f)] public float treasureWeight = 0.05f;
}
