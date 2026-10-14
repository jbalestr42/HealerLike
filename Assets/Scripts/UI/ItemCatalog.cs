using System.Collections.Generic;

// Editor tool (see ItemCatalogView): every item of the game in an inventory, to try them in game. An item taken out
// (e.g. dragged onto a unit) is replaced by a new one in its slot: the catalog never runs out
public class ItemCatalog
{
    readonly InventoryHandler _inventoryHandler = new InventoryHandler();
    public InventoryHandler inventoryHandler => _inventoryHandler;

    readonly Dictionary<AItem, AItemFactory> _factoryPerItem = new Dictionary<AItem, AItemFactory>();

    // Locked, its items can't be dragged out (e.g. the items of the player, given by a click instead)
    public ItemCatalog(IEnumerable<AItemFactory> itemFactories, bool isLocked = false)
    {
        _inventoryHandler.isLocked = isLocked;
        int index = 0;
        foreach (AItemFactory itemFactory in itemFactories)
        {
            if (itemFactory != null)
            {
                Add(itemFactory, index);
                index++;
            }
        }
        _inventoryHandler.OnItemRemoved.AddListener(OnItemRemoved);
    }

    void Add(AItemFactory itemFactory, int inventoryIndex)
    {
        AItem item = itemFactory.GetItem();
        _factoryPerItem[item] = itemFactory;
        _inventoryHandler.AddItem(item, inventoryIndex);
    }

    void OnItemRemoved(InventoryItemData itemData)
    {
        if (itemData == null || !_factoryPerItem.TryGetValue(itemData.item, out AItemFactory itemFactory))
        {
            return;
        }
        _factoryPerItem.Remove(itemData.item);
        Add(itemFactory, itemData.inventoryIndex);
    }

    // A new item like this one of the catalog, which keeps its own (e.g. clicked to give it), null for an item not
    // in the catalog
    public AItem Copy(AItem item)
    {
        return item != null && _factoryPerItem.TryGetValue(item, out AItemFactory itemFactory) ? itemFactory.GetItem() : null;
    }
}
