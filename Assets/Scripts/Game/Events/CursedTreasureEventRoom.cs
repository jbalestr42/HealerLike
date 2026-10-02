using System.Collections.Generic;
using UnityEngine;

// A stronger reward than a treasure room, cursed for the rest of the run. Content to come: for now the
// player can only leave
[CreateAssetMenu(menuName = "Custom/EventRooms/CursedTreasure")]
public class CursedTreasureEventRoom : AEventRoom
{
    public override void Play(IEventRoomHost host)
    {
        host.ShowChoices(eventName, description, CreateChoices(host.EndEvent));
    }

    public static List<EventChoice> CreateChoices(System.Action leave)
    {
        return new List<EventChoice>
        {
            new EventChoice { label = "Leave", description = "Leave the chest closed.", onSelected = leave },
        };
    }
}
