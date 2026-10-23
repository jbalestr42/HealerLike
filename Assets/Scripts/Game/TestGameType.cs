using System.Collections.Generic;

public class TestGameType : AGameType
{
    public override void StartGame()
    {
        InventoryHandler stash = PlayerBehaviour.instance.stash;
        foreach (AItemFactory itemFactory in DataManager.instance.GetItems(new List<string> { TagNames.Entity }))
        {
            stash.AddItem(itemFactory.GetItem(), -1);
            stash.AddItem(itemFactory.GetItem(), -1);
        }
    }

    public override bool IsOver()
    {
        return false;
    }
}
