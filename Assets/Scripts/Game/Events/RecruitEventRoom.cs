using System;
using System.Collections.Generic;
using UnityEngine;

// A few different units the character can recruit to choose from, one joins the player's units; the
// player can also leave without any
[CreateAssetMenu(menuName = "Custom/EventRooms/Recruit")]
public class RecruitEventRoom : AEventRoom
{
    [Min(1)] public int unitCount = 3;

    public override void Play(IEventRoomHost host)
    {
        List<EntityData> units = EventRoomPicker.PickDistinct(host.GetRewardEntities(), unitCount, host.random);
        host.ShowChoices(eventName, description, CreateChoices(units, unit =>
        {
            host.AddUnit(unit);
            host.EndEvent();
        }, host.EndEvent));
    }

    // One choice per unit with its details, then a way to leave without recruiting
    public static List<EventChoice> CreateChoices(IReadOnlyList<EntityData> units, Action<EntityData> recruit, Action leave)
    {
        List<EventChoice> choices = new List<EventChoice>();
        foreach (EntityData unit in units)
        {
            choices.Add(new EventChoice { label = unit.title, description = CharacterCardText.GetUnitDetails(unit), onSelected = () => recruit(unit) });
        }
        choices.Add(new EventChoice { label = "Leave", description = "Recruit no one.", onSelected = leave });
        return choices;
    }
}
