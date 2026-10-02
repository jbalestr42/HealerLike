using System;
using UnityEngine;

// An event an event room can be, and its share among the other events
[Serializable]
public class EventRoomChance
{
    public AEventRoom eventRoom;

    // Relative to the other events: 2 is drawn twice as often as 1, 0 never
    [Min(0f)] public float weight = 1f;
}
