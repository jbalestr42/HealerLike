using System.Collections.Generic;

namespace Game.Events
{

// Records what an event asks the run, and picks a choice like the player would
public class FakeEventRoomHost : IEventRoomHost
{
    public CharacterData characterData { get; set; }
    public System.Random random { get; set; } = new System.Random(0);

    public string shownTitle;
    public string shownDescription;
    public List<EventChoice> shownChoices = new List<EventChoice>();
    public List<EntityData> addedUnits = new List<EntityData>();
    public List<AItem> addedItems = new List<AItem>();
    public Dictionary<string, List<AItemFactory>> itemsPerTag = new Dictionary<string, List<AItemFactory>>();
    public int endCount = 0;

    public void ShowChoices(string title, string description, IReadOnlyList<EventChoice> choices)
    {
        shownTitle = title;
        shownDescription = description;
        shownChoices = new List<EventChoice>(choices);
    }

    public void AddUnit(EntityData unit)
    {
        addedUnits.Add(unit);
    }

    // The items registered for the first included tag, the excluded ones ignored
    public IReadOnlyList<AItemFactory> GetItems(List<string> includedTags, List<string> excludedTags = null)
    {
        return includedTags.Count > 0 && itemsPerTag.TryGetValue(includedTags[0], out List<AItemFactory> items) ? items : new List<AItemFactory>();
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
