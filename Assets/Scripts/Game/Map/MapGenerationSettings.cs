using UnityEngine;

[CreateAssetMenu(menuName = "Custom/MapGenerationSettings")]
public class MapGenerationSettings : ScriptableObject
{
    // Floors before the boss room
    [Min(1)] public int floorCount = 10;

    [Min(1)] public int columnCount = 7;

    // Paths drawn from the first floor to the last one, they can merge and split
    [Min(1)] public int pathCount = 6;
}
