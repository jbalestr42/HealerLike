using System.Collections.Generic;

namespace Game.Events
{

// Records what an event asks the run, and picks a choice like the player would
public class FakeEventRoomHost : IEventRoomHost
{
    public System.Random random { get; set; } = new System.Random(0);

    // The units the character can recruit
    public List<EntityData> rewardEntities = new List<EntityData>();

    // The items of the game, looked up by the names of their tags (or of the parents of their tags)
    public List<AItemFactory> items = new List<AItemFactory>();

    public string shownTitle;
    public string shownDescription;
    public List<EventChoice> shownChoices = new List<EventChoice>();
    public List<EntityData> addedUnits = new List<EntityData>();
    public List<AItem> addedItems = new List<AItem>();
    public int endCount = 0;

    public void ShowChoices(string title, string description, IReadOnlyList<EventChoice> choices)
    {
        shownTitle = title;
        shownDescription = description;
        shownChoices = new List<EventChoice>(choices);
    }

    public IReadOnlyList<EntityData> GetRewardEntities()
    {
        return rewardEntities;
    }

    public IReadOnlyList<AItemFactory> GetItems(List<string> includedTags, List<string> excludedTags = null)
    {
        return items.FindAll(item => item != null
            && includedTags.TrueForAll(tagName => HasTag(item, tagName))
            && (excludedTags == null || !excludedTags.Exists(tagName => HasTag(item, tagName))));
    }

    static bool HasTag(AItemFactory item, string tagName)
    {
        foreach (GameplayTag tag in item.tags)
        {
            for (GameplayTag current = tag; current != null; current = current.parent)
            {
                if (current.name == tagName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void AddUnit(EntityData unit)
    {
        addedUnits.Add(unit);
    }

    public void AddPlayerItem(AItem item)
    {
        addedItems.Add(item);
    }

    public void EndEvent()
    {
        endCount++;
    }

    public void Pick(string label)
    {
        shownChoices.Find(choice => choice.label == label).onSelected();
    }
}

}
