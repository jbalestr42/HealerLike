using System;
using System.Collections.Generic;
using UnityEngine;

// A chest holding one of the cursed unit items (a strong bonus and its curse on the same item), only
// revealed once opened; the player can also leave it closed
[CreateAssetMenu(menuName = "Custom/EventRooms/CursedTreasure")]
public class CursedTreasureEventRoom : AEventRoom
{
    public static List<string> ItemTags => new List<string> { TagNames.Entity, TagNames.Cursed };

    public override void Play(IEventRoomHost host)
    {
        // Drawn now, so the seed of the run always hides the same item in the chest
        List<AItemFactory> items = EventRoomPicker.PickDistinct(host.GetItems(ItemTags), 1, host.random);
        AItemFactory hidden = items.Count > 0 ? items[0] : null;
        host.ShowChoices(eventName, description, CreateChoices(hidden != null, () =>
        {
            AItem item = hidden.GetItem();
            host.AddUnitItem(item);
            host.ShowChoices(eventName, GetRevealText(item), new List<EventChoice>
            {
                new EventChoice { label = "Continue", onSelected = host.EndEvent },
            });
        }, host.EndEvent));
    }

    // Open only when the chest holds an item
    public static List<EventChoice> CreateChoices(bool hasItem, Action open, Action leave)
    {
        List<EventChoice> choices = new List<EventChoice>();
        if (hasItem)
        {
            choices.Add(new EventChoice { label = "Open", description = "Take the cursed item it holds, for one of your units.", onSelected = open });
        }
        choices.Add(new EventChoice { label = "Leave", description = "Leave the chest closed.", onSelected = leave });
        return choices;
    }

    // "You found Bloodthirst Blade: +60% damage, -40% healing received"
    public static string GetRevealText(AItem item)
    {
        string details = string.IsNullOrEmpty(item.description) ? "" : $"\n{item.description}";
        return $"You found <b>{item.title}</b>.{details}";
    }
}
