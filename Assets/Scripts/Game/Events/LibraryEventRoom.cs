using System;
using System.Collections.Generic;
using UnityEngine;

// A few different items tagged Library to choose one from, kept for the rest of the run. Normal: the ones
// that aren't cursed, a bonus with no counterpart. Dark: the cursed ones, a stronger bonus and its curse on
// the same item, which the player can also leave
[CreateAssetMenu(menuName = "Custom/EventRooms/Library")]
public class LibraryEventRoom : AEventRoom
{
    public const string TagName = "Library";

    [Min(1)] public int choiceCount = 3;

    // Offers the cursed items of the library instead of the other ones
    public bool isDark = false;

    public override void Play(IEventRoomHost host)
    {
        List<AItemFactory> items = EventRoomPicker.PickDistinct(host.GetItems(GetIncludedTags(isDark), GetExcludedTags(isDark)), choiceCount, host.random);
        host.ShowChoices(eventName, description, CreateChoices(items, isDark, item =>
        {
            host.AddPlayerItem(item.GetItem());
            host.EndEvent();
        }, host.EndEvent));
    }

    public static List<string> GetIncludedTags(bool isDark)
    {
        return isDark ? new List<string> { TagName, CursedTag.Name } : new List<string> { TagName };
    }

    public static List<string> GetExcludedTags(bool isDark)
    {
        return isDark ? new List<string>() : new List<string> { CursedTag.Name };
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
