using System;
using System.Collections.Generic;
using UnityEngine;

// A few different items to choose one from, kept for the rest of the run. Normal: blessings, a bonus with no
// counterpart. Dark: cursed items, a stronger bonus and its curse on the same item, which the player can
// also leave
[CreateAssetMenu(menuName = "Custom/EventRooms/Library")]
public class LibraryEventRoom : AEventRoom
{
    [Min(1)] public int choiceCount = 3;

    // Offers the cursed items of the game instead of the blessings
    public bool isDark = false;

    // Player items offered by the normal library
    public List<AItemFactory> blessings = new List<AItemFactory>();

    public override void Play(IEventRoomHost host)
    {
        IReadOnlyList<AItemFactory> pool = isDark ? host.GetItems(new List<string> { CursedTag.Name }) : blessings;
        List<AItemFactory> items = EventRoomPicker.PickDistinct(pool, choiceCount, host.random);
        host.ShowChoices(eventName, description, CreateChoices(items, isDark, item =>
        {
            host.AddPlayerItem(item.GetItem());
            host.EndEvent();
        }, host.EndEvent));
    }

    // One choice per item, then a way to leave without any when the items come with a curse (or when there
    // is no item at all)
    public static List<EventChoice> CreateChoices(IReadOnlyList<AItemFactory> items, bool canLeave, Action<AItemFactory> take, Action leave)
    {
        List<EventChoice> choices = new List<EventChoice>();
        foreach (AItemFactory item in items)
        {
            AItem preview = item.GetItem();
            choices.Add(new EventChoice { label = preview.title, description = preview.description, onSelected = () => take(item) });
        }

        if (canLeave || choices.Count == 0)
        {
            choices.Add(new EventChoice { label = "Leave", description = "Take nothing.", onSelected = leave });
        }
        return choices;
    }
}
